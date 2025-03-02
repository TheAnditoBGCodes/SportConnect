using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages.Manage;
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
        public IRepository<SportConnectUser> _repository { get; set; }

        public UserController(ILogger<UserController> logger, UserManager<SportConnectUser> userManager, IRepository<Participation> participationRepository, SignInManager<SportConnectUser> signInManager, IRepository<SportConnectUser> repository)
        {
            _logger = logger;
            _userManager = userManager;
            _participationRepository = participationRepository;
            _signInManager = signInManager;
            _repository = repository;
        }

        [Authorize(Roles = $"{SD.AdminRole},{SD.UserRole}")]
        public IActionResult EditUserMy()
        {
            var user = _userManager.GetUserAsync(this.User).Result;
            var names = user.FullName.Split(' ').ToList();
            var model = new SportConnectUserEditViewModel()
            {
                UserName = user.UserName,
                FirstName = names[0],
                LastName = names[1],
                Age = user.Age,
                Location = user.Location,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
            };
            return View(model);
        }

        [Authorize(Roles = $"{SD.AdminRole},{SD.UserRole}")]
        [HttpPost]
        public async Task<IActionResult> EditUserMy(SportConnectUserEditViewModel user)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return NotFound(); // Handle case where user is not found
            }

            // Skip the uniqueness check if the username has not changed
            if (currentUser.UserName != user.UserName && _repository.GetAll().Any(s => s.UserName == user.UserName))
            {
                ModelState.AddModelError("UserName", "Потребителското име е заето.");
            }// Skip the uniqueness check if the email has not changed
            if (currentUser.Email != user.Email && _repository.GetAll().Any(s => s.Email == user.Email))
            {
                ModelState.AddModelError("Email", "Този имейл вече е регистриран.");
            }

            // Skip the uniqueness check if the phone number has not changed
            if (currentUser.PhoneNumber != user.PhoneNumber && _repository.GetAll().Any(s => s.PhoneNumber == user.PhoneNumber))
            {
                ModelState.AddModelError("PhoneNumber", "Този телефонен номер вече е свързан с друг акаунт.");
            }

            if (ModelState.IsValid)
            {
                currentUser.UserName = user.UserName;
                currentUser.FullName = $"{user.FirstName} {user.LastName}";
                currentUser.Age = user.Age ?? 0;
                currentUser.Location = user.Location;
                currentUser.Email = user.Email;
                currentUser.PhoneNumber = user.PhoneNumber;

                var updateResult = await _userManager.UpdateAsync(currentUser);
                if (!updateResult.Succeeded)
                {
                    foreach (var error in updateResult.Errors)
                    {
                        ModelState.AddModelError("", error.Description);
                    }
                    return View(user);
                }

                await _signInManager.RefreshSignInAsync(currentUser); // Ensure navbar updates

                return RedirectToAction("PersonalData", "User");
            }
            return View(user);
        }

        [Authorize(Roles = $"{SD.AdminRole},{SD.UserRole}")]
        public IActionResult PersonalData()
        {
            var user = _userManager.GetUserAsync(this.User).Result;
            var names = user.FullName.Split(' ').ToList();
            var model = new SportConnectUserViewModel()
            {
                Id = user.Id,
                UserName = user.UserName,
                BothNames = $"{names[0]} {names[1]}",
                Age = user.Age,
                Location = user.Location,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
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
                Age = user.Age,
                Location = user.Location,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
            };

            model.Participations = participations;

            return View(model);
        }

        [Authorize(Roles = $"{SD.AdminRole}")]
        public IActionResult EditUserAdmin(string id)
        {
            var user = _repository.GetUserById(id);
            var names = user.FullName.Split(' ').ToList();
            var model = new SportConnectUserEditAdminViewModel()
            {
                Id = id,
                UserName = user.UserName,
                FirstName = names[0],
                LastName = names[1],
            };
            return View(model);
        }

        [Authorize(Roles = $"{SD.AdminRole}")]
        [HttpPost]
        public async Task<IActionResult> EditUserAdmin(string id, SportConnectUserEditAdminViewModel user)
        {
            if (!_repository.IsPropertyUnique(s => s.UserName == user.UserName && s.Id != user.Id))
            {
                ModelState.AddModelError("UserName", "Потребителското име е заето.");
            }
            if (ModelState.IsValid)
            {
                var existingUser = _repository.GetUserById(id);
                if (existingUser == null)
                {
                    return NotFound();
                }

                existingUser.UserName = user.UserName;
                existingUser.FullName = $"{user.FirstName} {user.LastName}";

                var updateResult = await _userManager.UpdateAsync(existingUser);
                if (!updateResult.Succeeded)
                {
                    foreach (var error in updateResult.Errors)
                    {
                        ModelState.AddModelError("", error.Description);
                    }
                    return View(user);
                }

                await _signInManager.RefreshSignInAsync(existingUser);

                return RedirectToAction("AllUsers", "User");
            }
            return View(user);
        }

        [Authorize(Roles = $"{SD.AdminRole}")]
        public IActionResult AllUsers()
        {
            var model = _repository.GetAll();
            return View(model.ToList());
        }

        [Authorize(Roles = $"{SD.AdminRole}")]
        public IActionResult DeleteUserAdmin(string id)
        {
            var user = _repository.GetUserById(id);
            var participations = _participationRepository.AllWithIncludes(p => p.Tournament, p => p.Tournament.Sport).Where(p => p.ParticipantId == id).ToList();

            var names = user.FullName.Split(' ').ToList();

            var model = new SportConnectUserViewModel()
            {
                Id = id,
                UserName = user.UserName,
                FirstName = names[0],
                LastName = names[1],
                Age = user.Age,
                Location = user.Location,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
            };

            model.Participations = participations;

            return View(model);
        }

        [Authorize(Roles = $"{SD.AdminRole}")]
        [HttpPost]
        public async Task<IActionResult> DeleteUserAdmin(string id, string ConfirmText, SportConnectUserDeletionViewModel model)
        {
            if (ConfirmText == "ПОТВЪРДИ")
            {
                var user = _repository.GetUserById(id);

                var currentUser = await _userManager.GetUserAsync(User);

                if (user.Id == currentUser.Id)
                {
                    var result = await _userManager.DeleteAsync(user);

                    if (result.Succeeded)
                    {
                        await _signInManager.SignOutAsync();
                        return RedirectToAction("Index", "Home");
                    }
                }

                var result1 = await _userManager.DeleteAsync(user);
                if (result1.Succeeded)
                {
                    return RedirectToAction("AllUsers");
                }

                return RedirectToAction("AllUsers");
            }
            return View(model);
        }

        [Authorize(Roles = $"{SD.AdminRole},{SD.UserRole}")]
        public IActionResult DeleteUserMy(string id)
        {
            var user = _repository.GetUserById(id);
            var participations = _participationRepository.AllWithIncludes(p => p.Tournament, p => p.Tournament.Sport).Where(p => p.ParticipantId == id).ToList();

            var names = user.FullName.Split(' ').ToList();

            var model = new SportConnectUserViewModel()
            {
                Id = id,
                UserName = user.UserName,
                FirstName = names[0],
                LastName = names[1],
                Age = user.Age,
                Location = user.Location,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
            };

            model.Participations = participations;

            return View(model);
        }

        [Authorize(Roles = $"{SD.AdminRole},{SD.UserRole}")]
        [HttpPost]
        public async Task<IActionResult> DeleteUserMy(string id, string ConfirmText,SportConnectUserDeletionViewModel model)
        {
            if (ConfirmText == "ПОТВЪРДИ")
            {
                var user = _repository.GetUserById(id);
                var currentUser = await _userManager.GetUserAsync(User);

                if (user.Id == currentUser.Id)
                {
                    var result = await _userManager.DeleteAsync(user);

                    if (result.Succeeded)
                    {
                        await _signInManager.SignOutAsync();
                        return RedirectToAction("Index", "Home");
                    }
                    else
                    {
                        // Optionally, add an error message
                        ModelState.AddModelError(string.Empty, "Неуспешно изтриване на акаунта.");
                    }
                }

                return RedirectToAction("PersonalData");
            }
            return View(model);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
