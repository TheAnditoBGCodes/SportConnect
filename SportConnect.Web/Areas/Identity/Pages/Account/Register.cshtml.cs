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
using Microsoft.Extensions.Logging;
using SportConnect.DataAccess.Repository.IRepository;
using SportConnect.DataAccess;
using SportConnect.Models;
using SportConnect.Services;
using SportConnect.Utility;
using System.Text.Json;

namespace SportConnect.Web.Areas.Identity.Pages.Account
{
    public class RegisterModel : PageModel
    {
        private readonly IRepository<SportConnectUser> _repository;
        private readonly HttpClient _httpClient;
        private SportConnectDbContext _context;
        private readonly SignInManager<SportConnectUser> _signInManager;
        private readonly UserManager<SportConnectUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IUserEmailStore<SportConnectUser> _emailStore;
        private readonly ILogger<RegisterModel> _logger;
        private readonly IEmailSender _emailSender;
        private readonly Cloudinary _cloudinary;
        private readonly CloudinaryService _cloudinaryService;
        private readonly IUserStore<SportConnectUser> _userStore;

        public RegisterModel(
            IRepository<SportConnectUser> repository,
            HttpClient httpClient,
            SportConnectDbContext context,
            SignInManager<SportConnectUser> signInManager,
            UserManager<SportConnectUser> userManager,
            RoleManager<IdentityRole> roleManager,
            ILogger<RegisterModel> logger,
            IEmailSender emailSender,
            Cloudinary cloudinary,
            CloudinaryService cloudinaryService,
            IUserStore<SportConnectUser> userStore) // Add IUserStore<SportConnectUser> here
        {
            _repository = repository;
            _httpClient = httpClient;
            _context = context;
            _signInManager = signInManager;
            _userManager = userManager;
            _roleManager = roleManager;
            _logger = logger;
            _emailSender = emailSender;
            _cloudinary = cloudinary;
            _cloudinaryService = cloudinaryService;
            _userStore = userStore; // Initialize directly
            _emailStore = (IUserEmailStore<SportConnectUser>)_userStore; // Cast to IUserEmailStore
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
            [StringLength(100, ErrorMessage = "Tрябва да е от {2} до {1} символа", MinimumLength = 5)]
            [Display(Name = "Потребителско име")]
            public string Username { get; set; }

            [Required(ErrorMessage = "Моля, въведете първото си име.")]
            [StringLength(100, ErrorMessage = "Tрябва да е от {2} до {1} символа", MinimumLength = 2)]
            [Display(Name = "Първо име")]
            public string FirstName { get; set; }

            [Required(ErrorMessage = "Моля, въведете фамилното си име.")]
            [StringLength(100, ErrorMessage = "Tрябва да е от {2} до {1} символа", MinimumLength = 2)]
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
                RoleList = roles.Select(y => new SelectListItem
                {
                    Value = y,
                    Text = y,
                }).ToList(),
                CountryList = await GetAllCountries()
            };
            ReturnUrl = returnUrl;
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
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
        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            string password = Input.Password;
            bool passwordHasErrors = false;

            if (Input.Username != null)
            {
                if (_repository.GetAll().Any(s => s.UserName == Input.Username && s.Id != Input.Username))
                {
                    ModelState.AddModelError("Input.Username", "Заето.");
                }

                if (!IsValidUsername(Input.Username))
                {
                    ModelState.AddModelError("Input.Username", "Може да съдържа само малки букви и цифри.");
                }
            }

            if (_repository.GetAll().Any(s => s.Email == Input.Email))
            {
                ModelState.AddModelError("Input.Email", "Заето.");
            }

            if (_repository.GetAll().Any(s => s.PhoneNumber == Input.PhoneNumber))
            {
                ModelState.AddModelError("Input.PhoneNumber", "Заето.");
            }

            if (string.IsNullOrEmpty(password) || password.Length < 8)
            {
                ViewData["ShortPassword"] = "8 символа;";  // Send error to view via ViewData
                passwordHasErrors = true;
            }
            else
            {
                if (!password.Any(char.IsUpper))
                {
                    ViewData["UpperPassword"] = "една главна буква;";  // Send error to view via ViewData
                    passwordHasErrors = true;
                }

                if (!Input.Username.Any(char.IsDigit))
                {
                    ViewData["DigitPassword"] = "една цифра;";  // Send error to view via ViewData
                    passwordHasErrors = true;
                }

                if (!password.Any(char.IsLower))
                {
                    ViewData["LowerPassword"] = "една малка буква;";  // Send error to view via ViewData
                    passwordHasErrors = true;
                }

                if (!password.Any(c => !char.IsLetterOrDigit(c)))
                {
                    ViewData["SpecialPassword"] = "един специален символ;";  // Send error to view via ViewData
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
                        await _userManager.AddToRoleAsync(user, SD.AdminRole);
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


            var roles = _roleManager.Roles.Select(x => x.Name).ToList();
            Input = new()
            {
                RoleList = roles.Select(y => new SelectListItem
                {
                    Value = y,
                    Text = y,
                }).ToList(),
                CountryList = await GetAllCountries()
            };
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

class CountryResponse
{
    public CountryName Name { get; set; }
}

class CountryName
{
    public string Common { get; set; }
}