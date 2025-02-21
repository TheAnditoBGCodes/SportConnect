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
                BothNames = $"{names[0]} {names[1]}",
                Age = user.Age,
                Location = user.Location,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
            };
            return View(model);
        }

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

        public IActionResult DeleteThisUser()
        {
            var user = _userManager.GetUserAsync(this.User).Result;
            var result = _userManager.DeleteAsync(user).Result;
            if (result.Succeeded)
            {
                _signInManager.SignOutAsync();
                return RedirectToAction("Index", "Home");
            }
            return RedirectToAction("PersonalData", "User");
        }

        public IActionResult DeleteIdUser(string id)
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

        [HttpPost]
        public async Task<IActionResult> DeleteIdUser(string id, SportConnectUserDeletionViewModel model)
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

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
