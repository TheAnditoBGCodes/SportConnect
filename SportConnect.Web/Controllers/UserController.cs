using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages.Manage;
using SportConnect.DataAccess.Repository.IRepository;
using SportConnect.Models;
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
        private readonly SignInManager<SportConnectUser> _signInManager;
        public IRepository<SportConnectUser> _repository { get; set; }

        public UserController(ILogger<UserController> logger, UserManager<SportConnectUser> userManager, SignInManager<SportConnectUser> signInManager, IRepository<SportConnectUser> repository)
        {
            _logger = logger;
            _userManager = userManager;
            _signInManager = signInManager;
            _repository = repository;
        }

        public IActionResult EditUser(string id)
        {
            var user = _repository.GetUserById(id);
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
                PasswordHash = user.PasswordHash
            };
            return View(model);
        }

        [HttpPost]
        public IActionResult EditUser(string id, SportConnectUserViewModel user)
        {
            if (ModelState.IsValid)
            {
                var useless = _repository.GetUserById(id);
                useless.Id = id;
                useless.UserName = user.UserName;
                useless.FullName = $"{user.FirstName} {user.LastName}";
                useless.Age = user.Age;
                useless.Location = user.Location;
                useless.Email = user.Email;
                useless.PhoneNumber = user.PhoneNumber;
                useless.PasswordHash = user.PasswordHash;
                _repository.Update(useless);
                return RedirectToAction("PersonalData", "User");
            }
            return View(user);
        }

        public IActionResult PersonalData()
        {
            var user = _userManager.GetUserAsync(this.User).Result;
            var names = user.FullName.Split(' ').ToList();
            var model = new SportConnectUserViewModel()
            {
                Id = user.Id,
                UserName = user.UserName,
                FirstName = names[0],
                LastName = names[1],
                Age = user.Age,
                Location = user.Location,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                PasswordHash = user.PasswordHash
            };
            return View(model);
        }

        public IActionResult EditUserAdmin(string id)
        {
            var user = _repository.GetUserById(id);
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
                PasswordHash = user.PasswordHash
            };
            return View(model);
        }

        [HttpPost]
        public IActionResult EditUserAdmin(string id, SportConnectUserViewModel user)
        {
            if (ModelState.IsValid)
            {
                var useless = _repository.GetUserById(id);
                useless.Id = id;
                useless.UserName = user.UserName;
                useless.FullName = $"{user.FirstName} {user.LastName}";
                useless.Age = user.Age;
                useless.Location = user.Location;
                useless.Email = user.Email;
                useless.PhoneNumber = user.PhoneNumber;
                useless.PasswordHash = user.PasswordHash;
                _repository.Update(useless);
                return RedirectToAction("AllUsers", "User");
            }
            return View(user);
        }

        public IActionResult AllUsers()
        {
            var model = _repository.GetAll();
            return View(model.ToList());
        }

        public IActionResult DeleteUser(string id)
        {
            var user = _userManager.GetUserAsync(this.User).Result;
            var result = _userManager.DeleteAsync(user).Result;
            if (result.Succeeded)
            {
                _signInManager.SignOutAsync();
                return RedirectToAction("Index","Home");
            }
            return RedirectToAction("AllUsers");
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
