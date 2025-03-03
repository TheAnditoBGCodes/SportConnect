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

        public RegisterModel(Cloudinary cloudinary, SportConnectDbContext context,
            UserManager<SportConnectUser> userManager,
            IUserStore<SportConnectUser> userStore,
            SignInManager<SportConnectUser> signInManager,
            ILogger<RegisterModel> logger,
            RoleManager<IdentityRole> roleManager,
            IEmailSender emailSender)
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
            [Required(ErrorMessage = "Моля, въведете парола.")]
            [StringLength(int.MaxValue, ErrorMessage = "Паролата трябва да е поне {2} символа", MinimumLength = 8)]
            [DataType(DataType.Password)]
            [Display(Name = "Парола")]
            public string Password { get; set; }

            /// <summary>
            ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
            ///     directly from your code. This API may change or be removed in future releases.
            /// </summary>
            [DataType(DataType.Password)]
            [Display(Name = "Потвърди паролата")]
            [Compare("Password", ErrorMessage = "Това поле не съответства с въведената парола")]
            public string ConfirmPassword { get; set; }

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
            public DateTime DateOfBirth { get; set; }

            [Required(ErrorMessage = "Моля, въведете местоположение.")]
            [StringLength(100, ErrorMessage = "Местоположението трябва да е от {2} до {1} символа", MinimumLength = 4)]
            [Display(Name = "Местоположение")]
            public string Location { get; set; }

            [Required(ErrorMessage = "Моля, въведете телефонен номер.")]
            [Phone(ErrorMessage = "Невалиден телефонен номер.")]
            [Display(Name = "Телефонен номер")]
            public string PhoneNumber { get; set; }

            [Required(ErrorMessage = "Моля, качете профилна снимка.")]
            [Display(Name = "Профилна снимка")]
            public IFormFile ProfileImage { get; set; }

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
            var roles = _roleManager.Roles.Select(x => x.Name).ToList();
            Input = new()
            {
                RoleList = roles.Select(y => new SelectListItem()
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

            if (ModelState.IsValid)
            {
                // Upload image to Cloudinary
                var uploadParams = new ImageUploadParams()
                {
                    File = new FileDescription(Input.ProfileImage.FileName, Input.ProfileImage.OpenReadStream()),
                    Transformation = new Transformation().Crop("fill").Gravity("face").Width(200).Height(200)
                };
                var uploadResult = await _cloudinary.UploadAsync(uploadParams);

                if (uploadResult.Error != null)
                {
                    ModelState.AddModelError(string.Empty, "Грешка при качване на изображението.");
                    return Page();
                }

                var user = new SportConnectUser
                {
                    FullName = $"{Input.FirstName} {Input.LastName}",
                    DateOfBirth = Input.DateOfBirth,
                    PhoneNumber = Input.PhoneNumber,
                    Location = Input.Location,
                    ImageUrl = uploadResult.SecureUrl.ToString() // Store URL from Cloudinary
                };

                await _userStore.SetUserNameAsync(user, Input.Username, CancellationToken.None);
                await _emailStore.SetEmailAsync(user, Input.Email, CancellationToken.None);
                var result = await _userManager.CreateAsync(user, Input.Password);

                if (result.Succeeded)
                {
                    _logger.LogInformation("User created a new account with password.");

                    // Automatically sign in the user after registration
                    await _signInManager.SignInAsync(user, isPersistent: false);

                    if (!string.IsNullOrEmpty(Input.Role))
                    {
                        await _userManager.AddToRoleAsync(user, Input.Role);
                    }
                    else
                    {
                        await _userManager.AddToRoleAsync(user, SD.UserRole);
                    }

                    return LocalRedirect(returnUrl); // Redirect to the return URL
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

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
