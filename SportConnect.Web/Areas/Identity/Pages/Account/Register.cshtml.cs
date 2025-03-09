// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using CloudinaryDotNet.Actions;
using CloudinaryDotNet;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Extensions.Logging;
using SportConnect.DataAccess;
using SportConnect.Models;
using SportConnect.Utility;
using SportConnect.Services;
using Newtonsoft.Json;
using System.Diagnostics.Metrics;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Model;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SportConnect.Web.Areas.Identity.Pages.Account
{
    public class RegisterModel : PageModel
    {

        private SportConnectDbContext _context;
        private readonly SignInManager<SportConnectUser> _signInManager;
        private readonly UserManager<SportConnectUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IUserStore<SportConnectUser> _userStore;
        private readonly IUserEmailStore<SportConnectUser> _emailStore;
        private readonly ILogger<RegisterModel> _logger;
        private readonly IEmailSender _emailSender;
        private readonly Cloudinary _cloudinary;
        private readonly CloudinaryService _cloudinaryService;
        private static readonly string RestCountriesApiUrl = "https://restcountries.com/v3.1/all";

        public RegisterModel(Cloudinary cloudinary, SportConnectDbContext context,
            UserManager<SportConnectUser> userManager,
            IUserStore<SportConnectUser> userStore,
            SignInManager<SportConnectUser> signInManager,
            ILogger<RegisterModel> logger,
            RoleManager<IdentityRole> roleManager,
            IEmailSender emailSender, CloudinaryService cloudinaryService)
        {
            _cloudinary = cloudinary;
            _userManager = userManager;
            _context = context;
            _userStore = userStore;
            _emailStore = (IUserEmailStore<SportConnectUser>)GetEmailStore();
            _signInManager = signInManager;
            _emailSender = emailSender;
            _logger = logger;
            _roleManager = roleManager;
            _emailSender = emailSender;
            _cloudinaryService = cloudinaryService;
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
        public string ReturnUrl { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public IList<AuthenticationScheme> ExternalLogins { get; set; }

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
            [Required(ErrorMessage = "Моля, въведете имейл.")]
            [EmailAddress(ErrorMessage = "Невалиден имейл")]
            [Display(Name = "Имейл")]
            public string Email { get; set; }

            /// <summary>
            ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
            ///     directly from your code. This API may change or be removed in future releases.
            /// </summary>
            [DataType(DataType.Password)]
            [Display(Name = "Парола")]
            public string? Password { get; set; }

            /// <summary>
            ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
            ///     directly from your code. This API may change or be removed in future releases.
            /// </summary>
            [DataType(DataType.Password)]
            [Display(Name = "Потвърди паролата")]
            public string? ConfirmPassword { get; set; }

            public string? Role { get; set; }
            [ValidateNever]
            public IEnumerable<SelectListItem> RoleList { get; set; } = new List<SelectListItem>();

            [Required(ErrorMessage = "Моля, въведете потребителско име.")]
            [StringLength(100, ErrorMessage = "Потребителското име трябва да е от {2} до {1} символа", MinimumLength = 5)]
            [Display(Name = "Потребителско име")]
            public string Username { get; set; }

            [Required(ErrorMessage = "Моля, въведете първото си име.")]
            [StringLength(100, ErrorMessage = "Името трябва да е от {2} до {1} символа", MinimumLength = 2)]
            [Display(Name = "Първо име")]
            public string FirstName { get; set; }

            [Required(ErrorMessage = "Моля, въведете фамилното си име.")]
            [StringLength(100, ErrorMessage = "Фамилията трябва да е от {2} до {1} символа", MinimumLength = 2)]
            [Display(Name = "Фамилия")]
            public string LastName { get; set; }

            [Required(ErrorMessage = "Моля, въведете своята дата на раждане.")]
            [DataType(DataType.Date)]
            [Display(Name = "Дата на раждане")]
            public DateTime? DateOfBirth { get; set; }

            [Required(ErrorMessage = "Моля, въведете държава.")]
            [Display(Name = "Държава")]
            public string Country { get; set; }
            [ValidateNever]
            public IEnumerable<SelectListItem> CountryList { get; set; } = new List<SelectListItem>();

            [Required(ErrorMessage = "Моля, въведете телефонен номер.")]
            [Phone(ErrorMessage = "Невалиден телефонен номер.")]
            [Display(Name = "Телефонен номер")]
            public string PhoneNumber { get; set; }

            [Required(ErrorMessage = "Моля, качете профилна снимка.")]
            [Display(Name = "Профилна снимка")]
            public string ProfileImage { get; set; }
        }

        private async Task<List<string>> GetCountriesAsync()
        {
            using (HttpClient client = new HttpClient())
            {
                var response = await client.GetStringAsync(RestCountriesApiUrl);
                var countries = JsonConvert.DeserializeObject<List<Country>>(response);

                return countries.OrderBy(c => c.Name.Common)
                                .Select(c => c.Name.Common)
                                .ToList();
            }
        }

        public async Task OnGetAsync(string returnUrl = null)
        {
            if (!await _roleManager.RoleExistsAsync(SD.AdminRole))
            {
                await _roleManager.CreateAsync(new IdentityRole(SD.AdminRole));
            }
            if (!await _roleManager.RoleExistsAsync(SD.UserRole))
            {
                await _roleManager.CreateAsync(new IdentityRole(SD.UserRole));
            }
            var countries = await GetCountriesAsync();
            Input = new()
            {
                CountryList = countries.Select(y => new SelectListItem()
                {
                    Value = y,
                    Text = y
                }).ToList()
            };
            ReturnUrl = returnUrl;
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            string password = Input.Password;
            bool passwordHasErrors = false;  // Flag to check if any password-related errors were found

            // Check password validity
            if (string.IsNullOrEmpty(password) || password.Length < 8)
            {
                ModelState.AddModelError("", "");
                ViewData["ShortPassword"] = "Паролата трябва да бъде поне 8 символа.";  // Send error to view via ViewData
                passwordHasErrors = true;
            }
            else
            {
                if(!password.Any(char.IsUpper))
                {
                    ModelState.AddModelError("", "");
                    ViewData["UpperPassword"] = "Паролата трябва да съдържа поне една главна буква.";  // Send error to view via ViewData
                    passwordHasErrors = true;
                }

                if (!password.Any(char.IsDigit))
                {
                    ModelState.AddModelError("", "");
                    ViewData["DigitPassword"] = "Паролата трябва да съдържа поне една цифра.";  // Send error to view via ViewData
                    passwordHasErrors = true;
                }

                if (!password.Any(char.IsLower))
                {
                    ModelState.AddModelError("", "");
                    ViewData["LowerPassword"] = "Паролата трябва да съдържа поне една малка буква.";  // Send error to view via ViewData
                    passwordHasErrors = true;
                }

                if (!password.Any(c => !char.IsLetterOrDigit(c)))
                {
                    ModelState.AddModelError("", "");
                    ViewData["SpecialPassword"] = "Паролата трябва да съдържа поне един специален символ.";  // Send error to view via ViewData
                    passwordHasErrors = true;
                }
            }

            // Only check for mismatch if both password and confirm password are filled and no previous errors for password
            if (passwordHasErrors == false && !string.IsNullOrEmpty(Input.Password) && !string.IsNullOrEmpty(Input.ConfirmPassword) && Input.Password != Input.ConfirmPassword)
            {
                ModelState.AddModelError("", "");
                ViewData["MissMatch"] = "Въведените данни в двете полета не съответстват.";
            }
            else if (passwordHasErrors == false && !string.IsNullOrEmpty(Input.Password) && string.IsNullOrEmpty(Input.ConfirmPassword))
            {
                ModelState.AddModelError("", "");
                ViewData["PleaseConfirm"] = "Моля потвърдете паролата.";
            }
            if (ModelState.IsValid)
            {
                var user = new SportConnectUser
                {
                    FullName = $"{Input.FirstName} {Input.LastName}",
                    DateOfBirth = (DateTime)Input.DateOfBirth,
                    PhoneNumber = Input.PhoneNumber,
                    Country = Input.Country,
                    ImageUrl = Input.ProfileImage
                };

                await _userStore.SetUserNameAsync(user, Input.Username, CancellationToken.None);
                await _emailStore.SetEmailAsync(user, Input.Email, CancellationToken.None);
                var result = await _userManager.CreateAsync(user, Input.Password);

                if (result.Succeeded)
                {
                    _logger.LogInformation("User created a new account with password.");

                    if (!string.IsNullOrEmpty(Input.Role))
                    {
                        await _userManager.AddToRoleAsync(user, Input.Role);
                    }
                    else
                    {
                        await _userManager.AddToRoleAsync(user, SD.UserRole);
                    }

                    var userId = await _userManager.GetUserIdAsync(user);
                    var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                    code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
                    var callbackUrl = Url.Page(
                        "/Account/ConfirmEmail",
                        pageHandler: null,
                        values: new { area = "Identity", userId = userId, code = code, returnUrl = returnUrl },
                        protocol: Request.Scheme);

                    await _emailSender.SendEmailAsync(Input.Email, "Confirm your email",
                        $"Please confirm your account by <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>clicking here</a>.");

                    if (_userManager.Options.SignIn.RequireConfirmedAccount)
                    {
                        return RedirectToPage("RegisterConfirmation", new { email = Input.Email, returnUrl = returnUrl });
                    }
                    else
                    {
                        await _signInManager.SignInAsync(user, isPersistent: false);
                        return LocalRedirect(returnUrl);
                    }
                }
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            var countries = await GetCountriesAsync();
            Input.CountryList = countries.Select(y => new SelectListItem()
            {
                Value = y,
                Text = y
            }).ToList();
            ModelState.AddModelError("Input.ProfileImage", "Моля, качете профилна снимка.");
            return Page();
        }

        private IdentityUser CreateUser()
        {
            try
            {
                return Activator.CreateInstance<IdentityUser>();
            }
            catch
            {
                throw new InvalidOperationException($"Can't create an instance of '{nameof(IdentityUser)}'. " +
                    $"Ensure that '{nameof(IdentityUser)}' is not an abstract class and has a parameterless constructor, or alternatively " +
                    $"override the register page in /Areas/Identity/Pages/Account/Register.cshtml");
            }
        }

        private IUserEmailStore<SportConnectUser> GetEmailStore()
        {
            if (!_userManager.SupportsUserEmail)
            {
                throw new NotSupportedException("The default UI requires a user store with email support.");
            }
            return (IUserEmailStore<SportConnectUser>)_userStore;
        }
    }
}

public class Country
{
    public CountryName Name { get; set; }
}

public class CountryName
{
    public string Common { get; set; }
}