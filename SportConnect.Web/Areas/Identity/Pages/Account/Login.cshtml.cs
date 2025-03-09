// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using SportConnect.Models;
using SportConnect.DataAccess;

namespace SportConnect.Web.Areas.Identity.Pages.Account
{
    public class LoginModel : PageModel
    {

        private SportConnectDbContext _context;
        private readonly SignInManager<SportConnectUser> _signInManager;
        private readonly UserManager<SportConnectUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IUserStore<SportConnectUser> _userStore;
        private readonly IUserEmailStore<SportConnectUser> _emailStore;
        private readonly ILogger<RegisterModel> _logger;
        private readonly IEmailSender _emailSender;

        public LoginModel(SportConnectDbContext context,
            UserManager<SportConnectUser> userManager,
            IUserStore<SportConnectUser> userStore,
            SignInManager<SportConnectUser> signInManager,
            ILogger<RegisterModel> logger,
            RoleManager<IdentityRole> roleManager,
            IEmailSender emailSender)
        {
            _userManager = userManager;
            _context = context;
            _userStore = userStore;
            _emailStore = (IUserEmailStore<SportConnectUser>)GetEmailStore();
            _signInManager = signInManager;
            _emailSender = emailSender;
            _logger = logger;
            _roleManager = roleManager;
        }

        private IUserEmailStore<SportConnectUser> GetEmailStore()
        {
            if (!_userManager.SupportsUserEmail)
            {
                throw new NotSupportedException("The default UI requires a user store with email support.");
            }
            return (IUserEmailStore<SportConnectUser>)_userStore;
        }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        [BindProperty]
        public InputModel Input { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public IList<AuthenticationScheme> ExternalLogins { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public string ReturnUrl { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        [TempData]
        public string ErrorMessage { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public class InputModel
        {
            /// <summary>
            ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
            ///     directly from your code. This API may change or be removed in future releases.
            /// </summary>

            [Required(ErrorMessage = "Моля въведете потребителско име или имейл.")]
            public string UserNameOrEmail { get; set; }

            [DataType(DataType.Password)]
            [Display(Name = "Парола")]
            public string Password { get; set; }
            /// <summary>
            ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
            ///     directly from your code. This API may change or be removed in future releases.
            /// </summary>
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

            // Clear the existing external cookie to ensure a clean login process
            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            ReturnUrl = returnUrl;
        }
        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            // Explicitly check both fields before adding errors.
            if (string.IsNullOrEmpty(Input.UserNameOrEmail))
            {
                ModelState.AddModelError("Input.UserNameOrEmail", "Моля въведете потребителско име или имейл.");
                ViewData["PasswordError"] = "Моля въведете парола.";
            }
            else if (string.IsNullOrEmpty(Input.Password))
            {
                ModelState.AddModelError("Input.Password", "Моля въведете парола.");
            }

            // If the model state is not valid, return the page with validation errors
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var user = await _userManager.FindByEmailAsync(Input.UserNameOrEmail);
            if (user == null)
            {
                user = await _userManager.FindByNameAsync(Input.UserNameOrEmail);
            }

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
