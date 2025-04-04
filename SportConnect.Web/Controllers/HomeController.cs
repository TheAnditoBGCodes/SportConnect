using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SportConnect.Models;
using SportConnect.Web.Models;
using System.Diagnostics;

namespace SportConnect.Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly UserManager<SportConnectUser> _userManager;

        public HomeController(UserManager<SportConnectUser> userManager)
        {
            _userManager = userManager;
        }

        [AllowAnonymous]
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(this.User);
            if (user != null)
            {
                ViewBag.UserName = user.UserName;
            }
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}