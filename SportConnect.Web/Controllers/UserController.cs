using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages.Manage;
using Newtonsoft.Json;
using SportConnect.DataAccess;
using SportConnect.DataAccess.Repository.IRepository;
using SportConnect.Models;
using SportConnect.Services;
using SportConnect.Utility;
using SportConnect.Web.Models;
using System.ComponentModel.DataAnnotations;
using System.Composition;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Net.Http;
using System.Text.Json;
using static Azure.Core.HttpHeader;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace SportConnect.Web.Controllers
{
    public class UserController : Controller
    {
        private readonly ILogger<TournamentController> _logger;
        private readonly UserManager<SportConnectUser> _userManager;
        public IRepository<Tournament> _tournamentsRepository { get; set; }
        public IRepository<Sport> _sportRepository { get; set; }
        public IRepository<SportConnectUser> _repository { get; set; }
        public IRepository<Participation> _participationsRepository { get; set; }
        private readonly HttpClient _httpClient;
        private readonly Cloudinary _cloudinary;
        private readonly SignInManager<SportConnectUser> _signInManager;
        private readonly CloudinaryService _cloudinaryService;
        private readonly SportConnectDbContext _context;

        public UserController(ILogger<TournamentController> logger, UserManager<SportConnectUser> userManager, IRepository<Tournament> tournamentsRepository, IRepository<Sport> sportRepository, IRepository<SportConnectUser> repository, IRepository<Participation> participationsRepository, HttpClient httpClient, Cloudinary cloudinary, SignInManager<SportConnectUser> signInManager, CloudinaryService cloudinaryService, SportConnectDbContext context)
        {
            _logger = logger;
            _userManager = userManager;
            _tournamentsRepository = tournamentsRepository;
            _sportRepository = sportRepository;
            _repository = repository;
            _participationsRepository = participationsRepository;
            _httpClient = httpClient;
            _cloudinary = cloudinary;
            _signInManager = signInManager;
            _cloudinaryService = cloudinaryService;
            _context = context;
        }

        private async Task<List<SelectListItem>> GetAllCountries()
        {
            var response = await _httpClient.GetStringAsync("https://restcountries.com/v3.1/all");
            var countries = System.Text.Json.JsonSerializer.Deserialize<List<CountryResponse>>(response, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            return countries?
                .OrderBy(c => c.Name.Common)
                .Select(c => new SelectListItem
                {
                    Value = c.Name.Common,
                    Text = c.Name.Common
                }).ToList() ?? new List<SelectListItem>();
        }

        [Authorize(Roles = $"{SD.AdminRole},{SD.UserRole}")]
        public async Task<IActionResult> EditUser(string id, string returnUrl = null)
        {
            var currentUser = await _userManager.GetUserAsync(this.User);
            ViewBag.UserId = currentUser.Id;

            var editedUser = _repository.GetUserById(id);
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
                    CountryList = await GetAllCountries(),
                    DateOfBirth = editedUser.DateOfBirth,
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

            // Save returnUrl in ViewBag
            ViewBag.ReturnUrl = returnUrl ?? Url.Action("AllSports", "Sport"); // Default fallback
            return View(model);
        }

        private bool IsValidUsername(string username)
        {
            // Define the allowed characters (same as in Identity configuration)
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

                if (string.IsNullOrWhiteSpace(user.UserName))
                {
                    ModelState.AddModelError("UserName", "Моля, въведете потребителско име.");
                }
                else if (user.UserName.Length < 5 || user.UserName.Length > 100)
                {
                    ModelState.AddModelError("UserName", "Tрябва да е от 5 до 100 символа.");
                }
                else if (_repository.GetAll().Any(s => s.UserName == user.UserName && s.Id != user.Id))
                {
                    ModelState.AddModelError("UserName", "Потребителското име е заето.");
                }
                else if (!IsValidUsername(user.UserName))
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
                    return View(user);
                }

                // Retrieve the user entity to update
                var editedUser = _repository.GetUserById(user.Id);

                if (editedUser == null)
                {
                    ModelState.AddModelError(string.Empty, "User not found.");
                    return View(user);
                }

                // Update the user properties from the model
                editedUser.UserName = user.UserName;
                editedUser.ImageUrl = user.ProfileImage;
                editedUser.FullName = $"{user.FirstName} {user.LastName}";
                editedUser.Email = user.Email;
                editedUser.Country = user.Country;
                editedUser.DateOfBirth = user.DateOfBirth.Value.Date;

                if (ModelState.IsValid)
                {
                    var result = await _userManager.UpdateAsync(editedUser);

                    if (result.Succeeded)
                    {
                        await _context.SaveChangesAsync(); // Save changes if necessary

                        // Redirect based on returnUrl
                        if (!string.IsNullOrEmpty(returnUrl))
                        {
                            return Redirect(returnUrl);
                        }
                        return RedirectToAction("AllUsers");
                    }
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
                else if (_repository.GetAll().Any(s => s.UserName == user.UserName && s.Id != user.Id))
                {
                    ModelState.AddModelError("UserName", "Потребителското име е заето.");
                }
                else if (!IsValidUsername(user.UserName))
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
                    return View(user);
                }

                // Retrieve the user entity to update
                var editedUser = _repository.GetUserById(user.Id);

                if (editedUser == null)
                {
                    ModelState.AddModelError(string.Empty, "User not found.");
                    return View(user);
                }

                // Update the user properties from the model
                editedUser.UserName = user.UserName;
                editedUser.ImageUrl = user.ProfileImage;
                editedUser.FullName = $"{user.FirstName} {user.LastName}";

                // Check if all the fields are valid
                if (ModelState.IsValid)
                {
                    // Update the user in the identity database
                    var result = await _userManager.UpdateAsync(editedUser);

                    if (result.Succeeded)
                    {
                        // Save changes if Identity update is successful
                        await _context.SaveChangesAsync(); // Ensure this is for the changes in your repository if needed
                        return RedirectToAction("AllUsers");
                    }
                }

                // If model state is invalid, return the view with the errors
                return View(user);
            }
        }

        [Authorize(Roles = $"{SD.AdminRole},{SD.UserRole}")]
        public IActionResult PersonalData()
        {
            var user = _userManager.GetUserAsync(this.User).Result;
            var names = user.FullName.Split(' ').ToList();
            var model = new UserViewModel()
            {
                Id = user.Id,
                UserName = user.UserName,
                BothNames = $"{names[0]} {names[1]}",
                DateOfBirth = user.DateOfBirth,
                Country = user.Country,
                Email = user.Email,
                ProfileImage = user.ImageUrl
            };
            return View(model);
        }

        [Authorize(Roles = $"{SD.AdminRole},{SD.UserRole}")]
        public async Task<IActionResult> UserDetails(string id, string returnUrl = null)
        {
            var user = _repository.GetUserById(id);
            var names = user.FullName.Split(' ').ToList();
            var model = new UserViewModel()
            {
                Id = user.Id,
                UserName = user.UserName,
                BothNames = $"{names[0]} {names[1]}",
                DateOfBirth = user.DateOfBirth,
                Country = user.Country,
                Email = user.Email,
                ProfileImage = user.ImageUrl
            };
            var myself = await _userManager.GetUserAsync(this.User);
            ViewBag.UserId = myself.Id;

            ViewBag.ReturnUrl = returnUrl ?? Url.Action("AllUsers", "User");
            return View(model);
        }

        [Authorize(Roles = $"{SD.AdminRole}")]
        public async Task<IActionResult> AllUsers(UserViewModel? filter)
        {
            if (filter == null)
            {
                return View(new UserViewModel());
            }

            var query = _repository.GetAll().AsQueryable();

            // Filter by country if selected
            if (!string.IsNullOrEmpty(filter.Country))
            {
                query = query.Where(p => p.Country == filter.Country);
            }

            // Filter by UserName or FullName if either contains the filter value, case-insensitive and trimming spaces
            if (!string.IsNullOrEmpty(filter.UserName))
            {
                string trimmedFilter = filter.UserName.Trim().ToLower();

                query = query.Where(p => p.UserName.Trim().ToLower().Contains(trimmedFilter) || p.FullName.Trim().ToLower().Contains(trimmedFilter));
            }

            // Filter by UserName or FullName if either contains the filter value, case-insensitive and trimming spaces
            if (!string.IsNullOrEmpty(filter.Email))
            {
                string trimmedFilter = filter.Email.Trim().ToLower();

                query = query.Where(p => p.Email.Trim().ToLower().Contains(trimmedFilter));
            }

            // Filter users by the specified birth year
            if (filter.BirthYear.HasValue)
            {
                // Filter users born in the selected year
                query = query.Where(p => p.DateOfBirth.Year == filter.BirthYear);
            }

            // Get the list of countries for the dropdown
            ViewBag.Countries = await GetAllCountries();

            // Prepare the model with filtered data
            var model = new UserViewModel
            {
                UserName = filter.UserName,
                BirthYear = filter.BirthYear,
                Country = filter.Country,
                Users = _repository.GetAll().ToList(),
                Email = filter.Email,
                FilteredUsers = query.ToList(),
            };

            return View(model);
        }

        [Authorize(Roles = $"{SD.AdminRole},{SD.UserRole}")]
        public async Task<IActionResult> DeleteUser(string id, string returnUrl = null)
        {
            var currentUser = await _userManager.GetUserAsync(this.User);
            ViewBag.UserId = currentUser.Id;

            var editedUser = _repository.GetUserById(id);
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
                    CountryList = await GetAllCountries(),
                    DateOfBirth = editedUser.DateOfBirth,
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

            // Save returnUrl in ViewBag
            ViewBag.ReturnUrl = returnUrl ?? Url.Action("AllSports", "Sport"); // Default fallback
            return View(model);
        }

        [Authorize(Roles = $"{SD.AdminRole},{SD.UserRole}")]
        [HttpPost]
        public async Task<IActionResult> DeleteUser(string ConfirmText, UserViewModel user, string returnUrl = null)
        {
            var deletedUser = _repository.GetUserById(user.Id);
            List<string> names = new List<string>();

            if (deletedUser == null)
            {
                ModelState.AddModelError(string.Empty, "User not found.");
                names = deletedUser.FullName.Split(' ').ToList();
                user.UserName = deletedUser.UserName;
                user.FirstName = names[0];
                user.LastName = names.Count > 1 ? names[1] : "";
                user.ProfileImage = deletedUser.ImageUrl;

                ViewBag.ReturnUrl = returnUrl;

                return View(user);
            }

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
                        return RedirectToAction("Index", "Home");
                    }
                    else if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    {
                        return Redirect(returnUrl);
                    }
                }
                else
                {
		    await _userManager.UpdateSecurityStampAsync(deletedUser);

                    var result = await _userManager.DeleteAsync(deletedUser);

                    if (result.Succeeded)
                    {  
                        return RedirectToAction("AllUsers");
                    }
                    else if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    {
                        return Redirect(returnUrl);
                    }
                }
            }
            ModelState.AddModelError(string.Empty, "User not found.");
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
