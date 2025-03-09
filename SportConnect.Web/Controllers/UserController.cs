using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages.Manage;
using Newtonsoft.Json;
using SportConnect.DataAccess.Repository.IRepository;
using SportConnect.Models;
using SportConnect.Utility;
using SportConnect.Web.Models;
using System.Composition;
using System.Diagnostics;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace SportConnect.Web.Controllers
{
    public class UserController : Controller
    {
        private readonly ILogger<UserController> _logger;
        private readonly UserManager<SportConnectUser> _userManager;
        public IRepository<Participation> _participationRepository;
        private readonly SignInManager<SportConnectUser> _signInManager;
        private static readonly string RestCountriesApiUrl = "https://restcountries.com/v3.1/all";
        public IRepository<SportConnectUser> _repository { get; set; }

        public UserController(ILogger<UserController> logger, UserManager<SportConnectUser> userManager, IRepository<Participation> participationRepository, SignInManager<SportConnectUser> signInManager, IRepository<SportConnectUser> repository)
        {
            _logger = logger;
            _userManager = userManager;
            _participationRepository = participationRepository;
            _signInManager = signInManager;
            _repository = repository;
        }

        private async Task<SelectList> GetCountriesAsync()
        {
            using (HttpClient client = new HttpClient())
            {
                var response = await client.GetStringAsync(RestCountriesApiUrl);
                var countries = JsonConvert.DeserializeObject<List<Country>>(response);

                var orderedCountries = countries.OrderBy(c => c.Name.Common)
                                                 .Select(c => new SelectListItem
                                                 {
                                                     Value = c.Name.Common,
                                                     Text = c.Name.Common
                                                 });

                // Return a SelectList, not IEnumerable<SelectListItem>
                return new SelectList(orderedCountries, "Value", "Text");
            }
        }
        [Authorize(Roles = $"{SD.AdminRole},{SD.UserRole}")]
        public async Task<IActionResult> EditUser(string id, string returnUrl = null)
        {
            var currentUser = await _userManager.GetUserAsync(this.User);
            ViewBag.UserId = currentUser.Id;

            var editedUser = _repository.GetUserById(id);
            var names = editedUser.FullName.Split(' ').ToList();

            var model = new SportConnectUserEditViewModel();
            if (id == currentUser.Id)
            {
                model = new SportConnectUserEditViewModel()
                {
                    Id = id,
                    UserName = editedUser.UserName,
                    FirstName = names[0],
                    LastName = names[1],
                    Country = editedUser.Country,
                    CountryList = await GetCountriesAsync(),
                    PhoneNumber = editedUser.PhoneNumber,
                    DateOfBirth = editedUser.DateOfBirth,
                    ProfileImage = editedUser.ImageUrl
                };
            }
            else
            {
                model = new SportConnectUserEditViewModel()
                {
                    Id = id,
                    UserName = editedUser.UserName,
                    FirstName = names[0],
                    LastName = names[1],
                    ProfileImage = editedUser.ImageUrl
                };
            }

            ViewBag.ReturnUrl = returnUrl ?? Url.Action("AllSports", "Sport"); // Default fallback
            return View(model);
        }

        [Authorize(Roles = $"{SD.AdminRole},{SD.UserRole}")]
        [HttpPost]
        public async Task<IActionResult> EditUser(SportConnectUserEditViewModel user, string returnUrl = null)
        {
            var currentUser = await _userManager.GetUserAsync(this.User);
            ViewBag.UserId = currentUser.Id;

            var editedUser = _repository.GetUserById(user.Id);

            if (user.Id == currentUser.Id)
            {
                if (_repository.GetAll().Any(s => s.UserName == user.UserName && s.Id != user.Id))
                {
                    ModelState.AddModelError("UserName", "Потребителското име е заето.");
                }

                if (_repository.GetAll().Any(s => s.PhoneNumber == user.PhoneNumber && s.Id != user.Id))
                {
                    ModelState.AddModelError("PhoneNumber", "Този телефонен номер вече е свързан с друг акаунт.");
                }

                if (ModelState.IsValid)
                {
                    editedUser.Id = user.Id;
                    editedUser.UserName = user.UserName;
                    editedUser.FullName = $"{user.FirstName} {user.LastName}";
                    editedUser.Country = user.Country;
                    editedUser.PhoneNumber = user.PhoneNumber;
                    editedUser.DateOfBirth = (DateTime)user.DateOfBirth.Value.Date;
                    editedUser.ImageUrl = user.ProfileImage;

                    var updateResult = await _userManager.UpdateAsync(editedUser);

                    if (!updateResult.Succeeded)
                    {
                        foreach (var error in updateResult.Errors)
                        {
                            ModelState.AddModelError("", error.Description);
                        }
                        user.CountryList = await GetCountriesAsync();
                        return View(user);
                    }

                    await _signInManager.RefreshSignInAsync(editedUser);

                    if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    {
                        return Redirect(returnUrl);
                    }

                    var referrer = Request.Headers["Referer"].ToString();

                    if (User.IsInRole(SD.AdminRole))
                    {
                        if (referrer.Contains("PersonalData"))
                        {
                            return RedirectToAction("PersonalDataAdmin");
                        }
                        else
                        {
                            return RedirectToAction("AllUsers");
                        }
                    }
                    return RedirectToAction("PersonalData");
                }
                else
                {
                    user.CountryList = await GetCountriesAsync();
                    return View(user);
                }
            }
            else
            {
                if (_repository.GetAll().Any(s => s.UserName == user.UserName && s.Id != user.Id))
                {
                    ModelState.AddModelError("UserName", "Потребителското име е заето.");
                }

                if (ModelState.IsValid)
                {
                    editedUser.Id = user.Id;
                    editedUser.UserName = user.UserName;
                    editedUser.FullName = $"{user.FirstName} {user.LastName}";
                    editedUser.ImageUrl = user.ProfileImage;

                    var updateResult = await _userManager.UpdateAsync(editedUser);

                    if (!updateResult.Succeeded)
                    {
                        foreach (var error in updateResult.Errors)
                        {
                            ModelState.AddModelError("", error.Description);
                        }
                        user.CountryList = await GetCountriesAsync();
                        return View(user);
                    }

                    await _signInManager.RefreshSignInAsync(editedUser);

                    if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    {
                        return Redirect(returnUrl);
                    }

                    var referrer = Request.Headers["Referer"].ToString();

                    if (referrer.Contains("PersonalData"))
                    {
                        return RedirectToAction("PersonalData");
                    }
                    else
                    {
                        return RedirectToAction("AllUsers");
                    }
                }
                else
                {
                    user.CountryList = await GetCountriesAsync();
                    return View(user);
                }
            }
        }

        [Authorize(Roles = $"{SD.AdminRole},{SD.UserRole}")]
        public IActionResult PersonalData()
        {
            var user = _userManager.GetUserAsync(this.User).Result;
            var names = user.FullName.Split(' ').ToList();
            var model = new SportConnectUserEditViewModel()
            {
                Id = user.Id,
                UserName = user.UserName,
                BothNames = $"{names[0]} {names[1]}",
                DateOfBirth = user.DateOfBirth,
                Country = user.Country,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                ProfileImage = user.ImageUrl
            };
            return View(model);
        }

        [Authorize(Roles = $"{SD.AdminRole}")]
        public IActionResult UserDetails(string id)
        {
            var user = _repository.GetUserById(id);
            var participations = _participationRepository.AllWithIncludes(p => p.Tournament, p => p.Tournament.Sport).Where(p => p.ParticipantId == id).ToList();

            var names = user.FullName.Split(' ').ToList();

            var model = new SportConnectUserViewModel()
            {
                Id = id,
                UserName = user.UserName,
                BothNames = $"{names[0]} {names[1]}",
                DateOfBirth = user.DateOfBirth,
                Country = user.Country,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
            };

            model.Participations = participations;

            return View(model);
        }

        [Authorize(Roles = $"{SD.AdminRole}")]
        public IActionResult AllUsers()
        {
            var model = _repository.GetAll();
            return View(model.ToList());
        }

        [Authorize(Roles = $"{SD.AdminRole},{SD.UserRole}")]
        public async Task<IActionResult> DeleteUser(string id, string returnUrl = null)
        {
            var currentUser = await _userManager.GetUserAsync(this.User);
            ViewBag.UserId = currentUser.Id;

            var deletedUser = _repository.GetUserById(id);
            var names = deletedUser.FullName.Split(' ').ToList();

            var model = new SportConnectUserEditViewModel();
            if (id == currentUser.Id)
            {
                model = new SportConnectUserEditViewModel()
                {
                    Id = id,
                    UserName = deletedUser.UserName,
                    FirstName = names[0],
                    LastName = names[1],
                    Country = deletedUser.Country,
                    Email = deletedUser.Email,
                    CountryList = await GetCountriesAsync(),
                    PhoneNumber = deletedUser.PhoneNumber,
                    DateOfBirth = deletedUser.DateOfBirth,
                    ProfileImage = deletedUser.ImageUrl
                };
            }
            else
            {
                model = new SportConnectUserEditViewModel()
                {
                    Id = id,
                    UserName = deletedUser.UserName,
                    FirstName = names[0],
                    LastName = names[1],
                    ProfileImage = deletedUser.ImageUrl
                };
            }

            ViewBag.ReturnUrl = returnUrl ?? Url.Action("AllSports", "Sport"); // Default fallback
            return View(model);
        }

        [Authorize(Roles = $"{SD.AdminRole},{SD.UserRole}")]
        [HttpPost]
        public async Task<IActionResult> DeleteUser(string ConfirmText, SportConnectUserEditViewModel user, string returnUrl = null)
        {
            if (ConfirmText == "ПОТВЪРДИ")
            {
                var currentUser = await _userManager.GetUserAsync(this.User);
                ViewBag.UserId = currentUser.Id;

                var deletedUser = _repository.GetUserById(user.Id);

                if (user.Id == currentUser.Id)
                {
                    var result = await _userManager.DeleteAsync(deletedUser);

                    if (result.Succeeded)
                    {
                        await _signInManager.SignOutAsync();
                        return RedirectToAction("Index", "Home");
                    }
                    else if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    {
                        return Redirect(returnUrl);
                    }
                }
                else
                {
                    var result = await _userManager.DeleteAsync(deletedUser);

                    if (result.Succeeded)
                    {
                        await _signInManager.SignOutAsync();
                        return RedirectToAction("AllUsers");
                    }
                    else if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    {
                        return Redirect(returnUrl);
                    }
                }
            }
            return View(user);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
