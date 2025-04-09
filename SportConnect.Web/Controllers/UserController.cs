using Humanizer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SportConnect.DataAccess.Repository.IRepository;
using SportConnect.Models;
using SportConnect.Services;
using SportConnect.Utility;
using SportConnect.Web.Models;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;

namespace SportConnect.Web.Controllers
{
    public class UserController : Controller
    {
        public UserManager<SportConnectUser> _userManager;
        public IRepository<Tournament> _tournamentRepository { get; set; }
        public IRepository<Sport> _sportRepository { get; set; }
        public IRepository<SportConnectUser> _userRepository { get; set; }
        public IRepository<Participation> _participationRepository { get; set; }
        public SignInManager<SportConnectUser> _signInManager;
        public CountryService _countryService { get; set; }

        public UserController(UserManager<SportConnectUser> userManager, IRepository<Tournament> tournamentRepository, IRepository<Sport> sportRepository, IRepository<SportConnectUser> userRepository, IRepository<Participation> participationRepository, SignInManager<SportConnectUser> signInManager, CountryService countryService)
        {
            _userManager = userManager;
            _tournamentRepository = tournamentRepository;
            _sportRepository = sportRepository;
            _userRepository = userRepository;
            _participationRepository = participationRepository;
            _signInManager = signInManager;
            _countryService = countryService;
        }

        public async Task<bool> IsValidUsername(string username)
        {
            var allowedCharacters = "abcdefghijklmnopqrstuvwxyz0123456789";
            foreach (var c in username)
            {
                if (!allowedCharacters.Contains(c))
                {
                    return false;
                }
            }
            return true;
        }

        [Authorize(Roles = $"{SD.AdminRole},{SD.UserRole}")]
        public async Task<IActionResult> EditUser(string id, string returnUrl = null)
        {
            var user = await _userRepository.GetById(id);
            if (user == null)
            {
                return View("~/Views/Shared/NotFound.cshtml");
            }
            var currentUser = await _userManager.GetUserAsync(this.User);
            ViewBag.UserId = currentUser.Id;

            var editedUser = await _userRepository.GetById(id);
            var names = editedUser.FullName.Split(' ').ToList();

            var model = new UserViewModel();
            if (id == currentUser.Id)
            {
                model = new UserViewModel()
                {
                    Id = id,
                    UserName = editedUser.UserName,
                    FirstName = names[0],
                    Email = editedUser.Email,
                    LastName = names[1],
                    Country = editedUser.Country,
                    CountryList = _countryService.GetAllCountries(),
                    DateOfBirth = DateTime.Parse(editedUser.DateOfBirth),
                    ProfileImage = editedUser.ImageUrl
                };
            }
            else
            {
                model = new UserViewModel()
                {
                    Id = id,
                    UserName = editedUser.UserName,
                    FirstName = names[0],
                    LastName = names[1],
                    ProfileImage = editedUser.ImageUrl
                };
            }

            ViewBag.ReturnUrl = returnUrl ?? Url.Action("AllSports", "Sport"); return View(model);
        }

        [Authorize(Roles = $"{SD.AdminRole},{SD.UserRole}")]
        [HttpPost]
        public async Task<IActionResult> EditUser(UserViewModel user, IFormFile? file, string returnUrl = null)
        {
            var currentUser = await _userManager.GetUserAsync(this.User);
            ViewBag.UserId = currentUser.Id;

            if (user.Id == currentUser.Id)
            {
                if (string.IsNullOrWhiteSpace(user.Email))
                {
                    ModelState.AddModelError("Email", "Моля, въведете имейл.");
                }
                else if (!new EmailAddressAttribute().IsValid(user.Email))
                {
                    ModelState.AddModelError("Email", "Невалиден имейл.");
                }
                else if ((await _userRepository.GetAll()).Any(s => s.Email == user.Email && s.Id != user.Id))
                {
                    ModelState.AddModelError("Email", "Заето.");
                }

                if (string.IsNullOrWhiteSpace(user.UserName))
                {
                    ModelState.AddModelError("UserName", "Моля, въведете потребителско име.");
                }
                else if (user.UserName.Length < 5 || user.UserName.Length > 100)
                {
                    ModelState.AddModelError("UserName", "Tрябва да е от 5 до 100 символа.");
                }
                else if ((await _userRepository.GetAll()).Any(s => s.UserName == user.UserName && s.Id != user.Id))
                {
                    ModelState.AddModelError("UserName", "Потребителското име е заето.");
                }
                else if (!await IsValidUsername(user.UserName))
                {
                    ModelState.AddModelError("UserName", "Потребителското име може да съдържа само малки букви и цифри.");
                }

                if (string.IsNullOrWhiteSpace(user.FirstName))
                {
                    ModelState.AddModelError("FirstName", "Моля, въведете първото си име.");
                }
                else if (user.FirstName.Length < 2 || user.FirstName.Length > 100)
                {
                    ModelState.AddModelError("FirstName", "Tрябва да е от 2 до 100 символа.");
                }

                if (string.IsNullOrWhiteSpace(user.LastName))
                {
                    ModelState.AddModelError("LastName", "Моля, въведете фамилното си име.");
                }
                else if (user.LastName.Length < 2 || user.LastName.Length > 100)
                {
                    ModelState.AddModelError("LastName", "Tрябва да е от 2 до 100 символа.");
                }

                if (!user.DateOfBirth.HasValue)
                {
                    ModelState.AddModelError("DateOfBirth", "Моля, въведете своята дата на раждане.");
                }

                if (string.IsNullOrWhiteSpace(user.Country))
                {
                    ModelState.AddModelError("Country", "Моля, въведете държава.");
                }

                if (!ModelState.IsValid)
                {
                    ViewBag.ReturnUrl = returnUrl;
                    user.CountryList = _countryService.GetAllCountries();
                    return View(user);
                }

                var editedUser = (await _userRepository.GetById(user.Id));

                editedUser.UserName = user.UserName;
                editedUser.ImageUrl = user.ProfileImage;
                editedUser.FullName = $"{user.FirstName} {user.LastName}";
                editedUser.Email = user.Email;
                editedUser.Country = user.Country;
                editedUser.DateOfBirth = user.DateOfBirth.Value.Date.ToString("yyyy-MM-dd");

                var result = await _userManager.UpdateAsync(editedUser);

                if (result.Succeeded)
                {
                    await _userRepository.Save();
                    return Redirect(returnUrl);
                }
                return View(user);
            }
            else
            {
                if (string.IsNullOrWhiteSpace(user.UserName))
                {
                    ModelState.AddModelError("UserName", "Моля, въведете потребителско име.");
                }
                else if (user.UserName.Length < 5 || user.UserName.Length > 100)
                {
                    ModelState.AddModelError("UserName", "Tрябва да е от 5 до 100 символа.");
                }
                else if ((await _userRepository.GetAll()).Any(s => s.UserName == user.UserName && s.Id != user.Id))
                {
                    ModelState.AddModelError("UserName", "Потребителското име е заето.");
                }
                else if (!await IsValidUsername(user.UserName))
                {
                    ModelState.AddModelError("UserName", "Потребителското име може да съдържа само малки букви и цифри.");
                }

                if (string.IsNullOrWhiteSpace(user.FirstName))
                {
                    ModelState.AddModelError("FirstName", "Моля, въведете първото си име.");
                }
                else if (user.FirstName.Length < 2 || user.FirstName.Length > 100)
                {
                    ModelState.AddModelError("FirstName", "Tрябва да е от 2 до 100 символа.");
                }

                if (string.IsNullOrWhiteSpace(user.LastName))
                {
                    ModelState.AddModelError("LastName", "Моля, въведете фамилното си име.");
                }
                else if (user.LastName.Length < 2 || user.LastName.Length > 100)
                {
                    ModelState.AddModelError("LastName", "Tрябва да е от 2 до 100 символа.");
                }

                if (!ModelState.IsValid)
                {
                    ViewBag.ReturnUrl = returnUrl;
                    return View(user);
                }

                var editedUser = (await _userRepository.GetById(user.Id));

                editedUser.UserName = user.UserName;
                editedUser.ImageUrl = user.ProfileImage;
                editedUser.FullName = $"{user.FirstName} {user.LastName}";

                var result = await _userManager.UpdateAsync(editedUser);

                if (result.Succeeded)
                {
                    await _userRepository.Save();
                    return Redirect(returnUrl);
                }
                return View(user);
            }
        }

        [Authorize(Roles = $"{SD.AdminRole},{SD.UserRole}")]
        public async Task<IActionResult> PersonalData()
        {
            var user = await _userManager.GetUserAsync(this.User);
            var names = user.FullName.Split(' ').ToList();

            DateTime date = DateTime.Parse(user.DateOfBirth);

            int age = DateTime.Now.Year - date.Year;
            if (DateTime.Now.Month < date.Month ||
            (DateTime.Now.Month == date.Month && DateTime.Now.Day < date.Day))
            {
                age--;
            }

            var model = new UserViewModel()
            {
                Age = age,
                Id = user.Id,
                UserName = user.UserName,
                FirstName = names[0],
                LastName = names[1],
                DateOfBirth = date.Date,
                Country = user.Country,
                Email = user.Email,
                ProfileImage = user.ImageUrl
            };
            return View(model);
        }

        [Authorize(Roles = $"{SD.AdminRole}")]
        public async Task<IActionResult> AllUsers(UserViewModel? filter, string returnUrl)
        {
            HttpContext.Session.Remove("ReturnUrl");
            var allSports = await _userRepository.GetAll(); if (filter == null)
            {
                return View(new UserViewModel
                {
                    Users = allSports.ToList(),
                    FilteredUsers = allSports.ToList()
                });
            }

            var query = (await _userRepository.GetAll()).AsQueryable();

            if (!string.IsNullOrEmpty(filter.Country))
            {
                query = query.Where(p => p.Country == filter.Country);
            }

            if (!string.IsNullOrEmpty(filter.UserName))
            {
                string trimmedFilter = filter.UserName.Trim().ToLower();

                query = query.Where(p => p.UserName.Trim().ToLower().Contains(trimmedFilter) || p.FullName.Trim().ToLower().Contains(trimmedFilter));
            }

            if (!string.IsNullOrEmpty(filter.Email))
            {
                string trimmedFilter = filter.Email.Trim().ToLower();

                query = query.Where(p => p.Email.Trim().ToLower().Contains(trimmedFilter));
            }

            if (filter.BirthYear.HasValue)
            {
                query = query.Where(p => DateTime.Parse(p.DateOfBirth).Year == filter.BirthYear);
            }

            var user = await _userManager.GetUserAsync(this.User);

            ViewBag.Countries = _countryService.GetAllCountries();
            ViewBag.UserId = user.Id;

            var model = new UserViewModel
            {
                UserName = filter.UserName,
                BirthYear = filter.BirthYear,
                Country = filter.Country,
                Users = (await _userRepository.GetAll()).ToList(),
                Email = filter.Email,
                FilteredUsers = query.ToList(),
            };

            ViewBag.ReturnUrl = returnUrl ?? Url.Action("AllUsers", "User");
            return View(model);
        }

        [Authorize(Roles = $"{SD.AdminRole},{SD.UserRole}")]
        public async Task<IActionResult> DeleteUser(string id, string returnUrl = null)
        {
            var editedUser = (await _userRepository.GetById(id));
            if (editedUser == null)
            {
                return View("~/Views/Shared/NotFound.cshtml");
            }
            var names = editedUser.FullName.Split(' ').ToList();

            var model = new UserViewModel()
            {
                Id = id,
                UserName = editedUser.UserName,
                FirstName = names[0],
                LastName = names[1],
                ProfileImage = editedUser.ImageUrl
            };

            ViewBag.ReturnUrl = returnUrl ?? Url.Action("AllSports", "Sport"); return View(model);
        }

        [Authorize(Roles = $"{SD.AdminRole},{SD.UserRole}")]
        [HttpPost]
        public async Task<IActionResult> DeleteUser(string ConfirmText, UserViewModel user, string returnUrl)
        {
            var deletedUser = (await _userRepository.GetById(user.Id));

            if (ConfirmText == "ПОТВЪРДИ")
            {
                var currentUser = await _userManager.GetUserAsync(this.User);
                ViewBag.UserId = currentUser.Id;
                if (user.Id == currentUser.Id)
                {
                    var result = await _userManager.DeleteAsync(deletedUser);

                    if (result.Succeeded)
                    {
                        await _signInManager.SignOutAsync();
                        await _userRepository.Save();
                        return RedirectToAction("Index", "Home");
                    }
                }
                else
                {
                    await _userManager.UpdateSecurityStampAsync(deletedUser);
                    var result = await _userManager.DeleteAsync(deletedUser);

                    if (result.Succeeded)
                    {
                        await _userRepository.Save();
                        return RedirectToAction("AllUsers");
                    }
                }
            }

            List<string> names = new List<string>();
            user.Id = deletedUser.Id;
            names = deletedUser.FullName.Split(' ').ToList();
            user.UserName = deletedUser.UserName;
            user.FirstName = names[0];
            user.LastName = names.Count > 1 ? names[1] : "";
            user.ProfileImage = deletedUser.ImageUrl;

            ViewBag.ReturnUrl = returnUrl;
            return View(user);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}