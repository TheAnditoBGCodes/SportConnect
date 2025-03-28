#nullable disable

using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using SportConnect.DataAccess.Repository.IRepository;
using SportConnect.Models;
using SportConnect.Services;
using SportConnect.Utility;

namespace SportConnect.Web.Areas.Identity.Pages.Account
{
    public class RegisterModel : PageModel
    {
        private readonly IRepository<SportConnectUser> _repository;
        private readonly SignInManager<SportConnectUser> _signInManager;
        private readonly UserManager<SportConnectUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly CountryService _countryService;
        private readonly IUserEmailStore<SportConnectUser> _emailStore;
        private readonly ILogger<RegisterModel> _logger;
        private readonly IUserStore<SportConnectUser> _userStore;

        public RegisterModel(IRepository<SportConnectUser> repository, SignInManager<SportConnectUser> signInManager, UserManager<SportConnectUser> userManager, RoleManager<IdentityRole> roleManager, CountryService countryService, ILogger<RegisterModel> logger, IUserStore<SportConnectUser> userStore)
        {
            _repository = repository;
            _signInManager = signInManager;
            _userManager = userManager;
            _roleManager = roleManager;
            _countryService = countryService;
            _logger = logger;
            _userStore = userStore;
            _emailStore = (IUserEmailStore<SportConnectUser>)_userStore;
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

            [Required(ErrorMessage = "Моля, качете профилна снимка.")]
            [Display(Name = "Профилна снимка")]
            public string ProfileImage { get; set; }
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
                CountryList = await _countryService.GetAllCountriesAsync()
            };
            ReturnUrl = returnUrl;
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
        }

        private async Task<bool> IsValidUsername(string username)
        {
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
                if ((await _repository.GetAll()).Any(s => s.UserName == Input.Username && s.Id != Input.Username))
                {
                    ModelState.AddModelError("Input.Username", "Заето.");
                }

                if (!await IsValidUsername(Input.Username))
                {
                    ModelState.AddModelError("Input.Username", "Може да съдържа само малки букви и цифри.");
                }
            }

            if ((await _repository.GetAll()).Any(s => s.Email == Input.Email))
            {
                ModelState.AddModelError("Input.Email", "Заето.");
            }

            if (string.IsNullOrEmpty(password) || password.Length < 8)
            {
                ViewData["ShortPassword"] = "8 символа;";
                passwordHasErrors = true;
            }
            else
            {
                if (!password.Any(char.IsUpper))
                {
                    ViewData["UpperPassword"] = "една главна буква;";
                    passwordHasErrors = true;
                }

                if (!Input.Username.Any(char.IsDigit))
                {
                    ViewData["DigitPassword"] = "една цифра;";
                    passwordHasErrors = true;
                }

                if (!password.Any(char.IsLower))
                {
                    ViewData["LowerPassword"] = "една малка буква;";
                    passwordHasErrors = true;
                }

                if (!password.Any(c => !char.IsLetterOrDigit(c)))
                {
                    ViewData["SpecialPassword"] = "един специален символ;";
                    passwordHasErrors = true;
                }
            }

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

                    await _signInManager.SignInAsync(user, isPersistent: false);
                    return LocalRedirect(returnUrl);
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
                CountryList = await _countryService.GetAllCountriesAsync()
            };
            ModelState.AddModelError("Input.ProfileImage", "Моля, качете профилна снимка.");
            return Page();
        }
        private async Task<IdentityUser> CreateUser()
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

        private async Task<IUserEmailStore<SportConnectUser>> GetEmailStore()
        {
            if (!_userManager.SupportsUserEmail)
            {
                throw new NotSupportedException("The default UI requires a user store with email support.");
            }
            return (IUserEmailStore<SportConnectUser>)_userStore;
        }
    }
}