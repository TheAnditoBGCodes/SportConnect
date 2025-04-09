#nullable disable

using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SportConnect.Models;

namespace SportConnect.Web.Areas.Identity.Pages.Account
{
    public class LoginModel : PageModel
    {
        private readonly SignInManager<SportConnectUser> _signInManager;
        private readonly UserManager<SportConnectUser> _userManager;
        private readonly IUserStore<SportConnectUser> _userStore;
        private readonly IUserEmailStore<SportConnectUser> _emailStore;
        private readonly ILogger<RegisterModel> _logger;

        public LoginModel(SignInManager<SportConnectUser> signInManager, UserManager<SportConnectUser> userManager, IUserStore<SportConnectUser> userStore, ILogger<RegisterModel> logger)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _userStore = userStore;
            _emailStore = (IUserEmailStore<SportConnectUser>)GetEmailStore();
            _logger = logger;
        }

        private IUserEmailStore<SportConnectUser> GetEmailStore()
        {
            if (!_userManager.SupportsUserEmail)
            {
                throw new NotSupportedException("The default UI requires a user store with email support.");
            }
            return (IUserEmailStore<SportConnectUser>)_userStore;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public IList<AuthenticationScheme> ExternalLogins { get; set; }
        public string ReturnUrl { get; set; }

        [TempData]
        public string ErrorMessage { get; set; }

        public class InputModel
        {
            [Required(ErrorMessage = "Моля въведете потребителско име или имейл.")]
            public string UserNameOrEmail { get; set; }

            [DataType(DataType.Password)]
            [Display(Name = "Парола")]
            public string Password { get; set; }

            [Display(Name = "Запомни ме?")]
            public bool RememberMe { get; set; }
        }

        public async Task OnGetAsync(string returnUrl = null)
        {
            if (!string.IsNullOrEmpty(ErrorMessage))
            {
                ModelState.AddModelError(string.Empty, ErrorMessage);
            }

            returnUrl ??= Url.Content("~/");

            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            ReturnUrl = returnUrl;
        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            if (string.IsNullOrEmpty(Input.UserNameOrEmail))
            {
                ModelState.AddModelError("Input.UserNameOrEmail", "Моля въведете потребителско име или имейл.");
                ViewData["PasswordError"] = "Моля въведете парола.";
            }
            else if (string.IsNullOrEmpty(Input.Password))
            {
                ModelState.AddModelError("Input.Password", "Моля въведете парола.");
            }

            if (!ModelState.IsValid)
            {
                return Page();
            }

            var user = await _userManager.FindByEmailAsync(Input.UserNameOrEmail) ?? await _userManager.FindByNameAsync(Input.UserNameOrEmail);

            if (user != null)
            {
                var result = await _signInManager.PasswordSignInAsync(user, Input.Password, Input.RememberMe, lockoutOnFailure: false);

                if (result.Succeeded)
                {
                    _logger.LogInformation("User logged in.");
                    return LocalRedirect(returnUrl ?? "~/");
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Грешно потребителско име/имейл или парола.");
                }
            }
            else
            {
                ModelState.AddModelError(string.Empty, "Потребителят не е намерен. Моля въведете имейла/потребителското име и/или паролата отново.");
            }
            return Page();
        }
    }
}
