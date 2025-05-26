using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;
using SportConnect.DataAccess.Repository.IRepository;
using SportConnect.Models;
using SportConnect.Services;
using SportConnect.Services.Participation;
using SportConnect.Services.Sport;
using SportConnect.Services.Tournament;
using SportConnect.Services.User;
using SportConnect.Utility;
using SportConnect.Web.Controllers;
using SportConnect.Web.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Linq.Expressions;
using System.Security.Claims;
using System.Threading.Tasks;

namespace SportConnect.Tests.Controllers
{
    [TestFixture]
    public class UserControllerTests
    {
        private Mock<UserManager<SportConnectUser>> _mockUserManager;
        public Mock<ITournamentService> _tournamentService;
        public Mock<IUserService> _userService;
        public Mock<ISportService> _sportService;
        public Mock<IParticipationService> _participationService;
        private Mock<SignInManager<SportConnectUser>> _mockSignInManager;
        private Mock<CountryService> _mockCountryService;
        private UserController _controller;

        private const string USER_ID = "user1";
        private const string ADMIN_ID = "admin1";

        [TearDown]
        public void TearDown()
        {
            _controller.Dispose();
        }

        [SetUp]
        public void Setup()
        {
            var userStore = new Mock<IUserStore<SportConnectUser>>();
            _mockUserManager = new Mock<UserManager<SportConnectUser>>(
                userStore.Object,
                null, null, null, null, null, null, null, null
            );

            var contextAccessor = new Mock<IHttpContextAccessor>();
            var userPrincipalFactory = new Mock<IUserClaimsPrincipalFactory<SportConnectUser>>();
            _mockSignInManager = new Mock<SignInManager<SportConnectUser>>(
                _mockUserManager.Object,
                contextAccessor.Object,
                userPrincipalFactory.Object,
                null, null, null, null
            );

            _tournamentService = new Mock<ITournamentService>();
            _userService = new Mock<IUserService>();
            _sportService = new Mock<ISportService>();
            _participationService = new Mock<IParticipationService>();
            _mockCountryService = new Mock<CountryService>();

            _controller = new UserController(
    _mockUserManager.Object, _tournamentService.Object,
                _userService.Object,
                _sportService.Object,
                _participationService.Object,
    _mockSignInManager.Object,
    _mockCountryService.Object
);


            var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
{
                new Claim(ClaimTypes.NameIdentifier, USER_ID),
                new Claim(ClaimTypes.Role, SD.UserRole)
}, "mock"));

            var httpContext = new DefaultHttpContext() { User = user };
            httpContext.Session = new MockHttpSession();
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };

            _controller.TempData = new TempDataDictionary(
    new DefaultHttpContext(),
    Mock.Of<ITempDataProvider>()
);
        }

        [Test]
        public void Constructor_ShouldInjectDependencies()
        {
            Assert.IsNotNull(_controller);
            Assert.IsInstanceOf<UserController>(_controller);
            Assert.AreEqual(_tournamentService.Object, _controller._tournamentService);
            Assert.AreEqual(_sportService.Object, _controller._sportService);
            Assert.AreEqual(_userService.Object, _controller._userService);
            Assert.AreEqual(_participationService.Object, _controller._participationService);
            Assert.AreEqual(_mockUserManager.Object, _controller._userManager);
            Assert.AreEqual(_mockSignInManager.Object, _controller._signInManager);
            Assert.AreEqual(_mockCountryService.Object, _controller._countryService);
        }
        [Test]
        public async Task EditUser_Get_CurrentUser_ReturnsViewWithFullModel()
        {
            var currentUser = new SportConnectUser
            {
                Id = USER_ID,
                UserName = "testuser",
                FullName = "Test User",
                Email = "test@example.com",
                Country = "Bulgaria",
                DateOfBirth = "1990-01-01",
                ImageUrl = "profile.jpg"
            };

            _mockUserManager.Setup(um => um.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
                .ReturnsAsync(currentUser);

            _userService.Setup(r => r.GetById(USER_ID))
                .ReturnsAsync(currentUser);

            _mockCountryService.Setup(cs => cs.GetAllCountries())
                .Returns(new List<SelectListItem>
                {
            new SelectListItem { Text = "Bulgaria", Value = "Bulgaria" },
            new SelectListItem { Text = "USA", Value = "USA" },
            new SelectListItem { Text = "Germany", Value = "Germany" }
                });

            var mockUrlHelper = new Mock<IUrlHelper>();
            mockUrlHelper.Setup(u => u.Action(It.IsAny<UrlActionContext>())).Returns("mockedReturnUrl");
            _controller.Url = mockUrlHelper.Object;

            var result = await _controller.EditUser(USER_ID) as ViewResult;

            Assert.IsNotNull(result);
            var model = result.Model as UserViewModel;
            Assert.IsNotNull(model);
            Assert.AreEqual(USER_ID, model.Id);
            Assert.AreEqual("testuser", model.UserName);
            Assert.AreEqual("Test", model.FirstName);
            Assert.AreEqual("User", model.LastName);
            Assert.AreEqual("test@example.com", model.Email);
            Assert.AreEqual("Bulgaria", model.Country);
            Assert.AreEqual(new DateTime(1990, 1, 1), model.DateOfBirth);
            Assert.AreEqual("profile.jpg", model.ProfileImage);
            Assert.IsNotNull(model.CountryList);
            Assert.AreEqual(3, model.CountryList.Count());
        }

        [Test]
        public async Task EditUser_Get_OtherUser_ReturnsViewWithLimitedModel()
        {
            var currentUser = new SportConnectUser { Id = USER_ID };
            var otherUser = new SportConnectUser
            {
                Id = "user2",
                UserName = "otheruser",
                FullName = "Other User",
                ImageUrl = "other.jpg"
            };

            _mockUserManager.Setup(um => um.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
                .ReturnsAsync(currentUser);

            _userService.Setup(r => r.GetById("user2"))
                .ReturnsAsync(otherUser);

            var mockUrlHelper = new Mock<IUrlHelper>();
            mockUrlHelper.Setup(u => u.Action(It.IsAny<UrlActionContext>())).Returns("mockedReturnUrl");
            _controller.Url = mockUrlHelper.Object;

            var result = await _controller.EditUser("user2") as ViewResult;

            Assert.IsNotNull(result);
            var model = result.Model as UserViewModel;
            Assert.IsNotNull(model);
            Assert.AreEqual("user2", model.Id);
            Assert.AreEqual("otheruser", model.UserName);
            Assert.AreEqual("Other", model.FirstName);
            Assert.AreEqual("User", model.LastName);
            Assert.AreEqual("other.jpg", model.ProfileImage);
            Assert.IsNull(model.Email);
            Assert.IsNull(model.Country);
            Assert.IsNull(model.DateOfBirth);
        }

        [Test]
        public async Task EditUser_Post_CurrentUser_ValidModel_UpdatesAndRedirects()
        {
            var currentUser = new SportConnectUser
            {
                Id = USER_ID,
                UserName = "oldusername",
                FullName = "Old Name",
                Email = "old@example.com",
                Country = "OldCountry",
                DateOfBirth = "1980-01-01",
                ImageUrl = "old.jpg"
            };

            _mockUserManager.Setup(um => um.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
                .ReturnsAsync(currentUser);

            _userService.Setup(r => r.GetById(USER_ID))
                .ReturnsAsync(currentUser);

            _userService.Setup(r => r.GetAll())
                .ReturnsAsync(new List<SportConnectUser> { currentUser });

            _mockUserManager.Setup(um => um.UpdateAsync(It.IsAny<SportConnectUser>()))
                .ReturnsAsync(IdentityResult.Success);

            var userViewModel = new UserViewModel
            {
                Id = USER_ID,
                UserName = "newusername",
                FirstName = "New",
                LastName = "Name",
                Email = "new@example.com",
                Country = "NewCountry",
                DateOfBirth = new DateTime(1990, 5, 15),
                ProfileImage = "new.jpg"
            };

            string returnUrl = "/Sport/AllSports";

            var result = await _controller.EditUser(userViewModel, null, returnUrl) as RedirectResult;

            Assert.IsNotNull(result);
            Assert.AreEqual(returnUrl, result.Url);

            _mockUserManager.Verify(um => um.UpdateAsync(It.Is<SportConnectUser>(u =>
                u.Id == USER_ID &&
                u.UserName == "newusername" &&
                u.FullName == "New Name" &&
                u.Email == "new@example.com" &&
                u.Country == "NewCountry" &&
                u.DateOfBirth == "1990-05-15" &&
                u.ImageUrl == "new.jpg"
            )), Times.Once);

            _userService.Verify(r => r.Save(), Times.Once);
        }

        [Test]
        public async Task EditUser_Post_CurrentUser_InvalidModel_ReturnsViewWithErrors()
        {
            var currentUser = new SportConnectUser
            {
                Id = USER_ID,
                UserName = "testuser",
                Email = "test@example.com"
            };

            _mockUserManager.Setup(um => um.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
                .ReturnsAsync(currentUser);

            _userService.Setup(r => r.GetAll())
                .ReturnsAsync(new List<SportConnectUser> {
                    currentUser,
                    new SportConnectUser { Id = "user2", UserName = "takenusername", Email = "taken@example.com" }
                });
            _mockCountryService.Setup(cs => cs.GetAllCountries())
    .Returns(new List<SelectListItem>
    {
        new SelectListItem { Text = "Bulgaria", Value = "Bulgaria" },
        new SelectListItem { Text = "USA", Value = "USA" },
    });

            var userViewModel = new UserViewModel
            {
                Id = USER_ID,
                UserName = "ta",
                FirstName = "",
                LastName = "S",
                Email = "invalidemail",
                Country = "",
                ProfileImage = "image.jpg"
            };

            string returnUrl = "/Sport/AllSports";

            var result = await _controller.EditUser(userViewModel, null, returnUrl) as ViewResult;

            Assert.IsNotNull(result);
            Assert.IsFalse(_controller.ModelState.IsValid);
            Assert.IsTrue(_controller.ModelState["UserName"].Errors.Any(e => e.ErrorMessage == "Tрябва да е от 5 до 100 символа."));
            Assert.IsTrue(_controller.ModelState["FirstName"].Errors.Any(e => e.ErrorMessage == "Моля, въведете първото си име."));
            Assert.IsTrue(_controller.ModelState["LastName"].Errors.Any(e => e.ErrorMessage == "Tрябва да е от 2 до 100 символа."));
            Assert.IsTrue(_controller.ModelState["Email"].Errors.Any(e => e.ErrorMessage == "Невалиден имейл."));
            Assert.IsTrue(_controller.ModelState["Country"].Errors.Any(e => e.ErrorMessage == "Моля, въведете държава."));
            Assert.IsTrue(_controller.ModelState["DateOfBirth"].Errors.Any(e => e.ErrorMessage == "Моля, въведете своята дата на раждане."));

            var model = result.Model as UserViewModel;
            Assert.IsNotNull(model);
            Assert.IsNotNull(model.CountryList);
            Assert.AreEqual(2, model.CountryList.Count());
        }

        [Test]
        public async Task EditUser_Post_CurrentUser_DuplicateUsername_ReturnsViewWithError()
        {
            var currentUser = new SportConnectUser
            {
                Id = USER_ID,
                UserName = "testuser"
            };

            _mockUserManager.Setup(um => um.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
                .ReturnsAsync(currentUser);

            _userService.Setup(r => r.GetAll())
                .ReturnsAsync(new List<SportConnectUser> {
                    currentUser,
                    new SportConnectUser { Id = "user2", UserName = "takenusername" }
                });
            _mockCountryService.Setup(cs => cs.GetAllCountries())
    .Returns(new List<SelectListItem>
    {
        new SelectListItem { Text = "Bulgaria", Value = "Bulgaria" },
        new SelectListItem { Text = "USA", Value = "USA" },
    });

            var userViewModel = new UserViewModel
            {
                Id = USER_ID,
                UserName = "takenusername",
                FirstName = "Test",
                LastName = "User",
                Email = "test@example.com",
                Country = "Bulgaria",
                DateOfBirth = DateTime.Now.AddYears(-20),
                ProfileImage = "image.jpg"
            };

            var result = await _controller.EditUser(userViewModel, null) as ViewResult;

            Assert.IsNotNull(result);
            Assert.IsFalse(_controller.ModelState.IsValid);
            Assert.IsTrue(_controller.ModelState["UserName"].Errors.Any(e => e.ErrorMessage == "Потребителското име е заето."));
        }

        [Test]
        public async Task EditUser_Post_CurrentUser_DuplicateEmail_ReturnsViewWithError()
        {
            var currentUser = new SportConnectUser
            {
                Id = USER_ID,
                Email = "test@example.com"
            };

            _mockUserManager.Setup(um => um.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
                .ReturnsAsync(currentUser);

            _userService.Setup(r => r.GetAll())
                .ReturnsAsync(new List<SportConnectUser> {
                    currentUser,
                    new SportConnectUser { Id = "user2", Email = "taken@example.com" }
                });

            _mockCountryService.Setup(cs => cs.GetAllCountries())
    .Returns(new List<SelectListItem>
    {
        new SelectListItem { Text = "Bulgaria", Value = "Bulgaria" },
        new SelectListItem { Text = "USA", Value = "USA" },
    });
            var userViewModel = new UserViewModel
            {
                Id = USER_ID,
                UserName = "validusername",
                FirstName = "Test",
                LastName = "User",
                Email = "taken@example.com",
                Country = "Bulgaria",
                DateOfBirth = DateTime.Now.AddYears(-20),
                ProfileImage = "image.jpg"
            };

            var result = await _controller.EditUser(userViewModel, null) as ViewResult;

            Assert.IsNotNull(result);
            Assert.IsFalse(_controller.ModelState.IsValid);
            Assert.IsTrue(_controller.ModelState["Email"].Errors.Any(e => e.ErrorMessage == "Заето."));
        }

        [Test]
        public async Task EditUser_Post_CurrentUser_InvalidUsername_ReturnsViewWithError()
        {
            var currentUser = new SportConnectUser
            {
                Id = USER_ID,
                UserName = "testuser"
            };

            _mockUserManager.Setup(um => um.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
                .ReturnsAsync(currentUser);

            _userService.Setup(r => r.GetAll())
                .ReturnsAsync(new List<SportConnectUser> { currentUser });

            _mockCountryService.Setup(cs => cs.GetAllCountries())
    .Returns(new List<SelectListItem>
    {
        new SelectListItem { Text = "Bulgaria", Value = "Bulgaria" },
        new SelectListItem { Text = "USA", Value = "USA" },
    });
            var userViewModel = new UserViewModel
            {
                Id = USER_ID,
                UserName = "Invalid@Username",
                FirstName = "Test",
                LastName = "User",
                Email = "test@example.com",
                Country = "Bulgaria",
                DateOfBirth = DateTime.Now.AddYears(-20),
                ProfileImage = "image.jpg"
            };

            var result = await _controller.EditUser(userViewModel, null) as ViewResult;

            Assert.IsNotNull(result);
            Assert.IsFalse(_controller.ModelState.IsValid);
            Assert.IsTrue(_controller.ModelState["UserName"].Errors.Any(e => e.ErrorMessage == "Потребителското име може да съдържа само малки букви и цифри."));
        }

        [Test]
        public async Task EditUser_Post_OtherUser_ValidModel_UpdatesAndRedirects()
        {
            var currentUser = new SportConnectUser { Id = USER_ID };
            var otherUser = new SportConnectUser
            {
                Id = "other1",
                UserName = "oldusername",
                FullName = "Old Name",
                ImageUrl = "old.jpg"
            };

            _mockUserManager.Setup(um => um.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
                .ReturnsAsync(currentUser);

            _userService.Setup(r => r.GetById("other1"))
                .ReturnsAsync(otherUser);

            _userService.Setup(r => r.GetAll())
                .ReturnsAsync(new List<SportConnectUser> { currentUser, otherUser });

            _mockUserManager.Setup(um => um.UpdateAsync(It.IsAny<SportConnectUser>()))
                .ReturnsAsync(IdentityResult.Success);

            var userViewModel = new UserViewModel
            {
                Id = "other1",
                UserName = "newusername",
                FirstName = "New",
                LastName = "Name",
                ProfileImage = "new.jpg"
            };

            string returnUrl = "/User/AllUsers";

            var result = await _controller.EditUser(userViewModel, null, returnUrl) as RedirectResult;

            Assert.IsNotNull(result);
            Assert.AreEqual(returnUrl, result.Url);

            _mockUserManager.Verify(um => um.UpdateAsync(It.Is<SportConnectUser>(u =>
                u.Id == "other1" &&
                u.UserName == "newusername" &&
                u.FullName == "New Name" &&
                u.ImageUrl == "new.jpg"
            )), Times.Once);

            _userService.Verify(r => r.Save(), Times.Once);
        }

        [Test]
        public async Task EditUser_Post_OtherUser_InvalidModel_ReturnsViewWithErrors()
        {
            var currentUser = new SportConnectUser { Id = USER_ID };

            _mockUserManager.Setup(um => um.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
                .ReturnsAsync(currentUser);

            var userViewModel = new UserViewModel
            {
                Id = "other1",
                UserName = "",
                FirstName = "",
                LastName = "",
                ProfileImage = "image.jpg"
            };

            var result = await _controller.EditUser(userViewModel, null) as ViewResult;

            Assert.IsNotNull(result);
            Assert.IsFalse(_controller.ModelState.IsValid);
            Assert.IsTrue(_controller.ModelState["UserName"].Errors.Any(e => e.ErrorMessage == "Моля, въведете потребителско име."));
            Assert.IsTrue(_controller.ModelState["FirstName"].Errors.Any(e => e.ErrorMessage == "Моля, въведете първото си име."));
            Assert.IsTrue(_controller.ModelState["LastName"].Errors.Any(e => e.ErrorMessage == "Моля, въведете фамилното си име."));
        }

        [Test]
        public async Task PersonalData_ReturnsViewWithUserData()
        {
            var currentUser = new SportConnectUser
            {
                Id = USER_ID,
                UserName = "testuser",
                FullName = "Test User",
                Email = "test@example.com",
                Country = "Bulgaria",
                DateOfBirth = "1990-01-01",
                ImageUrl = "profile.jpg"
            };

            _mockUserManager.Setup(um => um.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
                .ReturnsAsync(currentUser);

            var result = await _controller.PersonalData() as ViewResult;

            Assert.IsNotNull(result);
            var model = result.Model as UserViewModel;
            Assert.IsNotNull(model);
            Assert.AreEqual(USER_ID, model.Id);
            Assert.AreEqual("testuser", model.UserName);
            Assert.AreEqual("Test", model.FirstName);
            Assert.AreEqual("User", model.LastName);
            Assert.AreEqual("test@example.com", model.Email);
            Assert.AreEqual("Bulgaria", model.Country);
            Assert.AreEqual(new DateTime(1990, 1, 1), model.DateOfBirth);
            Assert.AreEqual("profile.jpg", model.ProfileImage);

            int expectedAge = DateTime.Now.Year - 1990;
            if (DateTime.Now.Month < 1 || (DateTime.Now.Month == 1 && DateTime.Now.Day < 1))
            {
                expectedAge--;
            }
            Assert.AreEqual(expectedAge, model.Age);
        }

        [Test]
        public async Task IsValidUsername_ValidUsername_ReturnsTrue()
        {
            string validUsername = "testuser123";

            var result = await _controller.IsValidUsername(validUsername);

            Assert.IsTrue(result);
        }

        [Test]
        public async Task IsValidUsername_InvalidUsername_WithUppercaseLetters_ReturnsFalse()
        {
            string invalidUsername = "TestUser123";

            var result = await _controller.IsValidUsername(invalidUsername);

            Assert.IsFalse(result);
        }

        [Test]
        public async Task IsValidUsername_InvalidUsername_WithSpecialCharacters_ReturnsFalse()
        {
            string invalidUsername = "test_user@123";

            var result = await _controller.IsValidUsername(invalidUsername);

            Assert.IsFalse(result);
        }

        [Test]
        public async Task IsValidUsername_InvalidUsername_WithSpaces_ReturnsFalse()
        {
            string invalidUsername = "test user 123";

            var result = await _controller.IsValidUsername(invalidUsername);

            Assert.IsFalse(result);
        }

        [Test]
        public async Task DeleteUser_Get_CurrentUser_ReturnsViewWithModel()
        {
            var currentUser = new SportConnectUser
            {
                Id = USER_ID,
                UserName = "testuser",
                FullName = "Test User",
                ImageUrl = "profile.jpg"
            };

            _userService.Setup(r => r.GetById(USER_ID))
                .ReturnsAsync(currentUser);

            var mockUrlHelper = new Mock<IUrlHelper>();
            mockUrlHelper.Setup(u => u.Action(It.IsAny<UrlActionContext>())).Returns("mockedReturnUrl");
            _controller.Url = mockUrlHelper.Object;

            var result = await _controller.DeleteUser(USER_ID) as ViewResult;

            Assert.IsNotNull(result);
            var model = result.Model as UserViewModel;
            Assert.IsNotNull(model);
            Assert.AreEqual(USER_ID, model.Id);
            Assert.AreEqual("testuser", model.UserName);
            Assert.AreEqual("Test", model.FirstName);
            Assert.AreEqual("User", model.LastName);
            Assert.AreEqual("profile.jpg", model.ProfileImage);
        }

        [Test]
        public async Task DeleteUser_Get_OtherUser_ReturnsViewWithModel()
        {
            var otherUser = new SportConnectUser
            {
                Id = "other1",
                UserName = "otheruser",
                FullName = "Other User",
                ImageUrl = "other.jpg"
            };

            _userService.Setup(r => r.GetById("other1"))
                .ReturnsAsync(otherUser);

            var mockUrlHelper = new Mock<IUrlHelper>();
            mockUrlHelper.Setup(u => u.Action(It.IsAny<UrlActionContext>())).Returns("mockedReturnUrl");
            _controller.Url = mockUrlHelper.Object;

            var result = await _controller.DeleteUser("other1") as ViewResult;

            Assert.IsNotNull(result);
            var model = result.Model as UserViewModel;
            Assert.IsNotNull(model);
            Assert.AreEqual("other1", model.Id);
            Assert.AreEqual("otheruser", model.UserName);
            Assert.AreEqual("Other", model.FirstName);
            Assert.AreEqual("User", model.LastName);
            Assert.AreEqual("other.jpg", model.ProfileImage);
        }

        [Test]
        public async Task DeleteUser_Post_CurrentUser_ValidConfirmation_LogsOutAndRedirectsToHome()
        {
            var currentUser = new SportConnectUser
            {
                Id = USER_ID,
                UserName = "testuser",
                FullName = "Test User"
            };

            _mockUserManager.Setup(um => um.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
                .ReturnsAsync(currentUser);

            _userService.Setup(r => r.GetById(USER_ID))
                .ReturnsAsync(currentUser);

            _mockUserManager.Setup(um => um.DeleteAsync(It.IsAny<SportConnectUser>()))
                .ReturnsAsync(IdentityResult.Success);

            var userViewModel = new UserViewModel
            {
                Id = USER_ID
            };

            var result = await _controller.DeleteUser("ПОТВЪРДИ", userViewModel, null) as RedirectToActionResult;

            Assert.IsNotNull(result);
            Assert.AreEqual("Index", result.ActionName);
            Assert.AreEqual("Home", result.ControllerName);

            _mockUserManager.Verify(um => um.DeleteAsync(It.IsAny<SportConnectUser>()), Times.Once);
            _mockSignInManager.Verify(sm => sm.SignOutAsync(), Times.Once);
            _userService.Verify(r => r.Save(), Times.Once);
        }

        [Test]
        public async Task DeleteUser_Post_OtherUser_ValidConfirmation_RedirectsToAllUsers()
        {
            var currentUser = new SportConnectUser { Id = USER_ID };
            var otherUser = new SportConnectUser
            {
                Id = "other1",
                UserName = "otheruser",
                FullName = "Other User"
            };

            _mockUserManager.Setup(um => um.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
                .ReturnsAsync(currentUser);

            _userService.Setup(r => r.GetById("other1"))
                .ReturnsAsync(otherUser);

            _mockUserManager.Setup(um => um.DeleteAsync(It.IsAny<SportConnectUser>()))
                .ReturnsAsync(IdentityResult.Success);

            var userViewModel = new UserViewModel
            {
                Id = "other1"
            };

            var result = await _controller.DeleteUser("ПОТВЪРДИ", userViewModel, null) as RedirectToActionResult;

            Assert.IsNotNull(result);
            Assert.AreEqual("AllUsers", result.ActionName);
            Assert.IsNull(result.ControllerName);

            _mockUserManager.Verify(um => um.UpdateSecurityStampAsync(It.IsAny<SportConnectUser>()), Times.Once);
            _mockUserManager.Verify(um => um.DeleteAsync(It.IsAny<SportConnectUser>()), Times.Once);
            _userService.Verify(r => r.Save(), Times.Once);
            _mockSignInManager.Verify(sm => sm.SignOutAsync(), Times.Never);
        }

        [Test]
        public async Task DeleteUser_Post_InvalidConfirmation_ReturnsViewWithModel()
        {
            var currentUser = new SportConnectUser { Id = USER_ID };
            var otherUser = new SportConnectUser
            {
                Id = "other1",
                UserName = "otheruser",
                FullName = "Other User",
                ImageUrl = "other.jpg"
            };

            _mockUserManager.Setup(um => um.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
                .ReturnsAsync(currentUser);

            _userService.Setup(r => r.GetById("other1"))
                .ReturnsAsync(otherUser);

            var userViewModel = new UserViewModel
            {
                Id = "other1"
            };

            string returnUrl = "/User/AllUsers";

            var result = await _controller.DeleteUser("invalid", userViewModel, returnUrl) as ViewResult;

            Assert.IsNotNull(result);
            var model = result.Model as UserViewModel;
            Assert.IsNotNull(model);
            Assert.AreEqual("other1", model.Id);
            Assert.AreEqual("otheruser", model.UserName);
            Assert.AreEqual("Other", model.FirstName);
            Assert.AreEqual("User", model.LastName);
            Assert.AreEqual("other.jpg", model.ProfileImage);
            Assert.AreEqual(returnUrl, result.ViewData["ReturnUrl"]);

            _mockUserManager.Verify(um => um.DeleteAsync(It.IsAny<SportConnectUser>()), Times.Never);
            _userService.Verify(r => r.Save(), Times.Never);
        }

        [Test]
        public async Task DeleteUser_Post_SingleNameUser_HandlesCorrectly()
        {
            var currentUser = new SportConnectUser { Id = USER_ID };
            var singleNameUser = new SportConnectUser
            {
                Id = "single1",
                UserName = "singlename",
                FullName = "Single",
                ImageUrl = "single.jpg"
            };

            _mockUserManager.Setup(um => um.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
                .ReturnsAsync(currentUser);

            _userService.Setup(r => r.GetById("single1"))
                .ReturnsAsync(singleNameUser);

            var userViewModel = new UserViewModel
            {
                Id = "single1"
            };

            var result = await _controller.DeleteUser("invalid", userViewModel, null) as ViewResult;

            Assert.IsNotNull(result);
            var model = result.Model as UserViewModel;
            Assert.IsNotNull(model);
            Assert.AreEqual("single1", model.Id);
            Assert.AreEqual("singlename", model.UserName);
            Assert.AreEqual("Single", model.FirstName);
            Assert.AreEqual("", model.LastName);
        }

        [Test]
        public async Task AllUsers_NoFilter_ReturnsDefaultViewModel()
        {
            var filter = (UserViewModel?)null;
            var userId = "user123";

            var user = new SportConnectUser
            {
                Id = userId,
                UserName = "testUser"
            };

            _userService.Setup(r => r.GetAll())
                .ReturnsAsync(new List<SportConnectUser>
                {
            new SportConnectUser { Id = "user1", UserName = "user1" },
            new SportConnectUser { Id = "user2", UserName = "user2" }
                });

            _mockUserManager.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
                .ReturnsAsync(user);

            _mockCountryService.Setup(cs => cs.GetAllCountries())
                .Returns(new List<SelectListItem>
                {
            new SelectListItem { Text = "USA", Value = "USA" },
            new SelectListItem { Text = "Germany", Value = "Germany" }
                });

            var result = await _controller.AllUsers(filter, "/return") as ViewResult;

            Assert.IsNotNull(result);
            Assert.That(result.Model, Is.InstanceOf(typeof(UserViewModel)));
            var model = result.Model as UserViewModel;
            Assert.IsNotNull(model);
            Assert.AreEqual(2, model.Users.Count); Assert.AreEqual(2, model.FilteredUsers.Count); Assert.AreEqual(null, model.UserName); Assert.AreEqual(null, model.Country);
            Assert.AreEqual(null, model.Email);
            Assert.AreEqual(null, model.BirthYear);
        }

        [Test]
        public async Task AllUsers_WithFilter_ReturnsFilteredResults()
        {
            var filter = new UserViewModel
            {
                UserName = "user1",
                Country = "USA"
            };

            var userId = "user123";
            var user = new SportConnectUser
            {
                Id = userId,
                UserName = "testUser"
            };

            _userService.Setup(r => r.GetAll())
                .ReturnsAsync(new List<SportConnectUser>
                {
            new SportConnectUser { Id = "user1", UserName = "user1", Country = "USA" },
            new SportConnectUser { Id = "user2", UserName = "user2", Country = "Germany" }
                });

            _mockUserManager.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
                .ReturnsAsync(user);

            _mockCountryService.Setup(cs => cs.GetAllCountries())
                .Returns(new List<SelectListItem>
                {
            new SelectListItem { Text = "USA", Value = "USA" },
            new SelectListItem { Text = "Germany", Value = "Germany" }
                });

            var result = await _controller.AllUsers(filter, "/return") as ViewResult;

            Assert.IsNotNull(result);
            var model = result.Model as UserViewModel;
            Assert.IsNotNull(model);
            Assert.AreEqual(1, model.FilteredUsers.Count); Assert.AreEqual("user1", model.FilteredUsers[0].UserName);
            Assert.AreEqual("USA", model.FilteredUsers[0].Country);
        }
        [Test]
        public async Task AllUsers_WithMultipleFilters_ReturnsCorrectFilteredResults()
        {
            var filter = new UserViewModel
            {
                UserName = "user1",
                Country = "USA",
                Email = "user1@example.com",
                BirthYear = 1990
            };

            var userId = "user123";
            var user = new SportConnectUser
            {
                Id = userId,
                UserName = "testUser"
            };

            _userService.Setup(r => r.GetAll())
                .ReturnsAsync(new List<SportConnectUser>
                {
            new SportConnectUser { Id = "user1", UserName = "user1", Email = "user1@example.com", Country = "USA", DateOfBirth = "1990-01-01" },
            new SportConnectUser { Id = "user2", UserName = "user2", Email = "user2@example.com", Country = "Germany", DateOfBirth = "1992-02-02" }
                });

            _mockUserManager.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
                .ReturnsAsync(user);

            _mockCountryService.Setup(cs => cs.GetAllCountries())
                .Returns(new List<SelectListItem>
                {
            new SelectListItem { Text = "Bulgaria", Value = "Bulgaria" },
            new SelectListItem { Text = "USA", Value = "USA" },
            new SelectListItem { Text = "Germany", Value = "Germany" }
                });

            var result = await _controller.AllUsers(filter, "/return") as ViewResult;

            Assert.IsNotNull(result);
            var model = result.Model as UserViewModel;
            Assert.IsNotNull(model);
            Assert.AreEqual(1, model.FilteredUsers.Count); Assert.AreEqual("user1", model.FilteredUsers[0].UserName);
            Assert.AreEqual("USA", model.FilteredUsers[0].Country);
            Assert.AreEqual("user1@example.com", model.FilteredUsers[0].Email);
            Assert.AreEqual(1990, DateTime.Parse(model.FilteredUsers[0].DateOfBirth).Year);
        }

    }
}
public class MockHttpSession : ISession
{
    private readonly Dictionary<string, byte[]> _sessionStorage = new Dictionary<string, byte[]>();

    public string Id => throw new NotImplementedException();
    public bool IsAvailable => true;
    public IEnumerable<string> Keys => _sessionStorage.Keys;

    public void Clear()
    {
        _sessionStorage.Clear();
    }

    public Task CommitAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task LoadAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public void Remove(string key)
    {
        _sessionStorage.Remove(key);
    }

    public void Set(string key, byte[] value)
    {
        _sessionStorage[key] = value;
    }

    public bool TryGetValue(string key, out byte[] value)
    {
        return _sessionStorage.TryGetValue(key, out value);
    }
}