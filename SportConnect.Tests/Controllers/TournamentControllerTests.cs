using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
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
using System.Linq;
using System.Linq.Expressions;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace SportConnect.Tests.Controllers
{
    [TestFixture]
    public class TournamentControllerTests
    {
        private Mock<UserManager<SportConnectUser>> _mockUserManager;
        public Mock<ITournamentService> _tournamentService;
        public Mock<IUserService> _userService;
        public Mock<ISportService> _sportService;
        public Mock<IParticipationService> _participationService;
        private Mock<CountryService> _mockCountryService;
        private TournamentController _controller;
        private ClaimsPrincipal _user;
        private SportConnectUser _currentUser;
        private DefaultHttpContext _httpContext;
        private SessionStorage _sessionStorage;

        [TearDown]
        public void TearDown()
        {
            _controller.Dispose();
        }

        [SetUp]
        public void Setup()
        {
            var mockStore = new Mock<IUserStore<SportConnectUser>>();
            _mockUserManager = new Mock<UserManager<SportConnectUser>>(
                mockStore.Object, null, null, null, null, null, null, null, null);

            _tournamentService = new Mock<ITournamentService>();
            _userService = new Mock<IUserService>();
            _sportService = new Mock<ISportService>();
            _participationService = new Mock<IParticipationService>();
            _mockCountryService = new Mock<CountryService>();

            _currentUser = new SportConnectUser
            {
                Id = "user123",
                UserName = "testuser@example.com",
                FullName = "Test User"
            };

            _user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
{
                new Claim(ClaimTypes.Name, "testuser@example.com"),
                new Claim(ClaimTypes.NameIdentifier, "user123"),
                new Claim(ClaimTypes.Role, SD.UserRole)
}, "mock"));

            _mockUserManager.Setup(x => x.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
    .ReturnsAsync(_currentUser);

            _sessionStorage = new SessionStorage();
            _httpContext = new DefaultHttpContext();
            _httpContext.Session = new TestSession(_sessionStorage);

            _controller = new TournamentController(
    _mockUserManager.Object,
     _tournamentService.Object,
                _userService.Object,
                _sportService.Object,
                _participationService.Object,
    _mockCountryService.Object
);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = _httpContext
            };
            _controller.ControllerContext.HttpContext.User = _user;

            var tempData = new TempDataDictionary(
                new DefaultHttpContext(),
                Mock.Of<ITempDataProvider>()
            );
            _controller.TempData = tempData;
        }

        #region Constructor Tests

        [Test]
        public void Constructor_InitializesProperties()
        {
            Assert.That(_controller._userManager, Is.EqualTo(_mockUserManager.Object));
            Assert.That(_controller._tournamentService, Is.EqualTo(_tournamentService.Object));
            Assert.That(_controller._sportService, Is.EqualTo(_sportService.Object));
            Assert.That(_controller._userService, Is.EqualTo(_userService.Object));
            Assert.That(_controller._participationService, Is.EqualTo(_participationService.Object));
            Assert.That(_controller._countryService, Is.EqualTo(_mockCountryService.Object));
        }

        #endregion

        #region AllTournaments Tests

        [Test]
        public async Task AllTournaments_WithNoFilter_ReturnsEmptyViewModel()
        {
            TournamentViewModel filter = null;

            var result = await _controller.AllTournaments(filter) as ViewResult;

            Assert.That(result, Is.Not.Null);
            Assert.That(result.Model, Is.TypeOf<TournamentViewModel>());
            Assert.That((result.Model as TournamentViewModel).FilteredTournaments, Is.Null);
        }

        [Test]
        public async Task AllTournaments_RemovesReturnUrlFromSession()
        {
            _httpContext.Session.SetString("ReturnUrl", "someUrl");

            await _controller.AllTournaments(new TournamentViewModel());

            Assert.That(_httpContext.Session.GetString("ReturnUrl"), Is.Null);
        }

        [Test]
        public async Task AllTournaments_WithLoggedInUser_SetsUserId()
        {
            var filter = new TournamentViewModel();
            var tournaments = new List<Tournament>();
            _tournamentService.Setup(x => x.AllWithIncludes(
                It.IsAny<Expression<Func<Tournament, object>>>(),
                It.IsAny<Expression<Func<Tournament, object>>>(),
                It.IsAny<Expression<Func<Tournament, object>>>()
            )).ReturnsAsync(tournaments);

            var result = await _controller.AllTournaments(filter) as ViewResult;

            Assert.That(result.ViewData["UserId"], Is.EqualTo("user123"));
        }

        [Test]
        public async Task AllTournaments_WithFilter_AppliesFilters()
        {
            var sports = new List<Sport> { new Sport { Id = "sport1", Name = "Football" } };
            var countries = new List<string> { "Country1", "Country2" };

            var tournaments = new List<Tournament>
            {
                new Tournament
                {
                    Id = "1",
                    Name = "Football Tournament",
                    SportId = "sport1",
                    Organizer = new SportConnectUser { FullName = "Test Organizer" },
                    Country = "Country1",
                    Date = DateTime.Now.AddDays(10).ToString()
                },
                new Tournament
                {
                    Id = "2",
                    Name = "Another Tournament",
                    SportId = "sport2",
                    Organizer = new SportConnectUser { FullName = "Another Organizer" },
                    Country = "Country2",
                    Date = DateTime.Now.AddDays(20).ToString()
                }
            };

            var filter = new TournamentViewModel
            {
                SportId = "sport1",
                OrganizerName = "organizer",
                Country = "Country1",
                Name = "football",
                StartDate = DateTime.Now.AddDays(5),
                EndDate = DateTime.Now.AddDays(15)
            };

            _tournamentService.Setup(x => x.AllWithIncludes(
                It.IsAny<Expression<Func<Tournament, object>>>(),
                It.IsAny<Expression<Func<Tournament, object>>>(),
                It.IsAny<Expression<Func<Tournament, object>>>()
            )).ReturnsAsync(tournaments);

            _sportService.Setup(x => x.GetAll())
                .ReturnsAsync(sports);

            _mockCountryService.Setup(x => x.GetAllCountries())
    .Returns(countries.Select(c => new SelectListItem { Text = c, Value = c }).ToList());

            var result = await _controller.AllTournaments(filter) as ViewResult;
            var viewModel = result.Model as TournamentViewModel;

            Assert.That(result, Is.Not.Null);
            Assert.That(viewModel, Is.Not.Null);
            Assert.That(viewModel.OrganizerName, Is.EqualTo("organizer"));
            Assert.That(viewModel.SportId, Is.EqualTo("sport1"));
            Assert.That(viewModel.Country, Is.EqualTo("Country1"));
            Assert.That(viewModel.Name, Is.EqualTo("football"));
            Assert.That(viewModel.StartDate, Is.EqualTo(filter.StartDate));
            Assert.That(viewModel.EndDate, Is.EqualTo(filter.EndDate));
            Assert.That(result.ViewData["Sports"], Is.TypeOf<SelectList>());
            var selectListItems = result.ViewData["Countries"] as List<SelectListItem>;
            Assert.That(selectListItems.Select(c => c.Value), Is.EquivalentTo(countries));
        }

        [Test]
        public async Task AllTournaments_WithoutLoggedInUser_DoesNotSetUserId()
        {
            _mockUserManager.Setup(x => x.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
    .ReturnsAsync((SportConnectUser)null);

            var filter = new TournamentViewModel();
            var tournaments = new List<Tournament>();
            _tournamentService.Setup(x => x.AllWithIncludes(
                It.IsAny<Expression<Func<Tournament, object>>>(),
                It.IsAny<Expression<Func<Tournament, object>>>(),
                It.IsAny<Expression<Func<Tournament, object>>>()
            )).ReturnsAsync(tournaments);

            var result = await _controller.AllTournaments(filter) as ViewResult;

            Assert.That(result.ViewData["UserId"], Is.Null);
        }

        #endregion

        #region AddTournament Tests

        [Test]
        public async Task AddTournament_Get_ReturnsViewWithModel()
        {
            var returnUrl = "/return/url";
            var sports = new List<Sport> { new Sport { Id = "sport1", Name = "Football" } };
            var countries = new List<string> { "Country1", "Country2" };

            _sportService.Setup(x => x.GetAll())
                .ReturnsAsync(sports);

            _mockCountryService.Setup(x => x.GetAllCountries())
    .Returns(countries.Select(c => new SelectListItem { Text = c, Value = c }).ToList());

            var result = await _controller.AddTournament(returnUrl) as ViewResult;
            var viewModel = result.Model as TournamentViewModel;

            Assert.That(result, Is.Not.Null);
            Assert.That(viewModel, Is.Not.Null);
            Assert.That(viewModel.OrganizerId, Is.EqualTo(_currentUser.Id));
            Assert.That(viewModel.Sports, Is.Not.Null);
            Assert.That(result.ViewData["ReturnUrl"], Is.EqualTo(returnUrl));
        }

        [Test]
        public async Task AddTournament_Post_WithValidModel_AddsAndRedirects()
        {
            var returnUrl = "/return/url";
            var tournament = new TournamentViewModel
            {
                Name = "New Tournament Name",
                Description = "New Tournament Description",
                SportId = "sport1",
                ImageUrl = "image.jpg",
                Country = "Country1",
                Date = DateTime.Now.AddDays(5),
                DateTimer = new TimeSpan(14, 0, 0),
                Deadline = DateTime.Now.AddDays(3),
                DeadlineTime = new TimeSpan(12, 0, 0),
                OrganizerId = _currentUser.Id
            };

            _tournamentService.Setup(x => x.GetAll())
                .ReturnsAsync(new List<Tournament>());

            _tournamentService.Setup(x => x.Add(It.IsAny<Tournament>()))
                .Returns(Task.CompletedTask);

            var result = await _controller.AddTournament(tournament, returnUrl) as RedirectResult;

            Assert.That(result, Is.Not.Null);
            Assert.That(result.Url, Is.EqualTo(returnUrl));
            _tournamentService.Verify(x => x.Add(It.IsAny<Tournament>()), Times.Once);
        }

        [Test]
        public async Task AddTournament_Post_WithInvalidModel_ReturnsViewWithErrors()
        {
            var returnUrl = "/return/url";
            var tournament = new TournamentViewModel
            {
                Name = "New",
                Description = "Desc",
                SportId = null,
                ImageUrl = null,
                Country = null,
                Date = null,
                Deadline = null,
                DateTimer = null,
                DeadlineTime = null,
                OrganizerId = _currentUser.Id
            };

            var existingTournaments = new List<Tournament>
            {
                new Tournament { Name = "Existing Name", Description = "Existing Description" }
            };

            _tournamentService.Setup(x => x.GetAll())
                .ReturnsAsync(existingTournaments);

            var sports = new List<Sport> { new Sport { Id = "sport1", Name = "Football" } };
            var countries = new List<string> { "Country1", "Country2" };

            _sportService.Setup(x => x.GetAll())
                .ReturnsAsync(sports);

            _mockCountryService.Setup(x => x.GetAllCountries())
    .Returns(countries.Select(c => new SelectListItem { Text = c, Value = c }).ToList());

            var result = await _controller.AddTournament(tournament, returnUrl) as ViewResult;

            Assert.That(result, Is.Not.Null);
            Assert.That(result.Model, Is.SameAs(tournament));
            Assert.That(result.ViewData.ModelState.IsValid, Is.False);
            Assert.That(result.ViewData.ModelState.ErrorCount, Is.GreaterThan(0));
            Assert.That(result.ViewData["ReturnUrl"], Is.EqualTo(returnUrl));
            _tournamentService.Verify(x => x.Add(It.IsAny<Tournament>()), Times.Never);
        }

        [Test]
        public async Task AddTournament_Post_WithExistingNameAndDescription_ReturnsViewWithErrors()
        {
            var returnUrl = "/return/url";
            var tournament = new TournamentViewModel
            {
                Name = "Existing Name",
                Description = "Existing Description",
                SportId = "sport1",
                ImageUrl = "image.jpg",
                Country = "Country1",
                Date = DateTime.Now.AddDays(5),
                DateTimer = new TimeSpan(14, 0, 0),
                Deadline = DateTime.Now.AddDays(3),
                DeadlineTime = new TimeSpan(12, 0, 0),
                OrganizerId = _currentUser.Id
            };

            var existingTournaments = new List<Tournament>
            {
                new Tournament { Name = "Existing Name", Description = "Existing Description" }
            };

            _tournamentService.Setup(x => x.GetAll())
                .ReturnsAsync(existingTournaments);

            var sports = new List<Sport> { new Sport { Id = "sport1", Name = "Football" } };

            _sportService.Setup(x => x.GetAll())
                .ReturnsAsync(sports);

            var result = await _controller.AddTournament(tournament, returnUrl) as ViewResult;

            Assert.That(result, Is.Not.Null);
            Assert.That(result.ViewData.ModelState.IsValid, Is.False);
            Assert.That(result.ViewData.ModelState["Name"].Errors.Count, Is.EqualTo(1));
            Assert.That(result.ViewData.ModelState["Description"].Errors.Count, Is.EqualTo(1));
        }

        [Test]
        public async Task AddTournament_Post_WithDeadlineInPast_ReturnsViewWithError()
        {
            var returnUrl = "/return/url";
            var tournament = new TournamentViewModel
            {
                Name = "New Tournament Name",
                Description = "New Tournament Description",
                SportId = "sport1",
                ImageUrl = "image.jpg",
                Country = "Country1",
                Date = DateTime.Now.AddDays(5),
                DateTimer = new TimeSpan(14, 0, 0),
                Deadline = DateTime.Now.AddDays(-1),
                DeadlineTime = new TimeSpan(12, 0, 0),
                OrganizerId = _currentUser.Id
            };

            _tournamentService.Setup(x => x.GetAll())
                .ReturnsAsync(new List<Tournament>());

            var sports = new List<Sport> { new Sport { Id = "sport1", Name = "Football" } };

            _sportService.Setup(x => x.GetAll())
                .ReturnsAsync(sports);

            var result = await _controller.AddTournament(tournament, returnUrl) as ViewResult;

            Assert.That(result, Is.Not.Null);
            Assert.That(result.ViewData.ModelState.IsValid, Is.False);
            Assert.That(result.ViewData.ModelState["DeadlineTime"].Errors.Count, Is.EqualTo(1));
        }

        [Test]
        public async Task AddTournament_Post_WithEventBeforeDeadline_ReturnsViewWithError()
        {
            var returnUrl = "/return/url";
            var now = DateTime.Now;
            var tournament = new TournamentViewModel
            {
                Name = "New Tournament Name",
                Description = "New Tournament Description",
                SportId = "sport1",
                ImageUrl = "image.jpg",
                Country = "Country1",
                Date = now.AddDays(3),
                DateTimer = new TimeSpan(12, 0, 0),
                Deadline = now.AddDays(5),
                DeadlineTime = new TimeSpan(14, 0, 0),
                OrganizerId = _currentUser.Id
            };

            _tournamentService.Setup(x => x.GetAll())
                .ReturnsAsync(new List<Tournament>());

            var sports = new List<Sport> { new Sport { Id = "sport1", Name = "Football" } };

            _sportService.Setup(x => x.GetAll())
                .ReturnsAsync(sports);

            var result = await _controller.AddTournament(tournament, returnUrl) as ViewResult;

            Assert.That(result, Is.Not.Null);
            Assert.That(result.ViewData.ModelState.IsValid, Is.False);
            Assert.That(result.ViewData.ModelState["DateOrder"].Errors.Count, Is.EqualTo(1));
        }

        #endregion

        #region EditTournament Tests

        [Test]
        public async Task EditTournament_Get_WithValidId_ReturnsViewWithModel()
        {
            var tournamentId = "tournament1";
            var returnUrl = "/return/url";

            var tournament = new Tournament
            {
                Id = tournamentId,
                Name = "Test Tournament",
                Description = "Test Description",
                OrganizerId = _currentUser.Id,
                Date = DateTime.Now.AddDays(10).ToString(),
                Deadline = DateTime.Now.AddDays(5).ToString(),
                ImageUrl = "image.jpg",
                Country = "Country1",
                SportId = "sport1"
            };

            var sports = new List<Sport> { new Sport { Id = "sport1", Name = "Football" } };
            var countries = new List<string> { "Country1", "Country2" };

            _tournamentService.Setup(x => x.GetById(tournamentId))
                .ReturnsAsync(tournament);

            _sportService.Setup(x => x.GetAll())
                .ReturnsAsync(sports);

            _mockCountryService.Setup(x => x.GetAllCountries())
    .Returns(countries.Select(c => new SelectListItem { Text = c, Value = c }).ToList());

            var result = await _controller.EditTournament(tournamentId, returnUrl) as ViewResult;
            var viewModel = result.Model as TournamentViewModel;

            Assert.That(result, Is.Not.Null);
            Assert.That(viewModel, Is.Not.Null);
            Assert.That(viewModel.Id, Is.EqualTo(tournamentId));
            Assert.That(viewModel.Name, Is.EqualTo(tournament.Name));
            Assert.That(viewModel.Description, Is.EqualTo(tournament.Description));
            Assert.That(viewModel.OrganizerId, Is.EqualTo(tournament.OrganizerId));
            Assert.That(viewModel.Date?.Date, Is.EqualTo(DateTime.Parse(tournament.Date).Date));
            Assert.That(viewModel.DateTimer, Is.EqualTo(DateTime.Parse(tournament.Date).TimeOfDay));
            Assert.That(viewModel.Deadline?.Date, Is.EqualTo(DateTime.Parse(tournament.Deadline).Date));
            Assert.That(viewModel.DeadlineTime, Is.EqualTo(DateTime.Parse(tournament.Deadline).TimeOfDay));
            Assert.That(viewModel.ImageUrl, Is.EqualTo(tournament.ImageUrl));
            Assert.That(viewModel.Country, Is.EqualTo(tournament.Country));
            Assert.That(viewModel.SportId, Is.EqualTo(tournament.SportId));
            Assert.That(result.ViewData["UserId"], Is.EqualTo(_currentUser.Id));
            Assert.That(result.ViewData["ReturnUrl"], Is.EqualTo(returnUrl));
        }

        [Test]
        public async Task EditTournament_Get_WithInvalidId_ReturnsNotFound()
        {
            var tournamentId = "invalid";
            var returnUrl = "/return/url";

            _tournamentService.Setup(x => x.GetById(tournamentId))
                .ReturnsAsync((Tournament)null);

            var result = await _controller.EditTournament(tournamentId, returnUrl);

            Assert.That(result, Is.TypeOf<ViewResult>());
        }

        [Test]
        public async Task EditTournament_Post_AsOrganizer_WithValidModel_UpdatesAndRedirects()
        {
            var returnUrl = "/return/url";
            var existingTournament = new Tournament
            {
                Id = "tournament1",
                Name = "Old Name",
                Description = "Old Description",
                OrganizerId = _currentUser.Id,
                Date = DateTime.Now.AddDays(5).ToString(),
                Deadline = DateTime.Now.AddDays(3).ToString(),
                ImageUrl = "old-image.jpg",
                Country = "OldCountry",
                SportId = "sport1"
            };

            var tournament = new TournamentViewModel
            {
                Id = "tournament1",
                Name = "Updated Name",
                Description = "Updated Description",
                OrganizerId = _currentUser.Id,
                Date = DateTime.Now.AddDays(10),
                DateTimer = new TimeSpan(14, 0, 0),
                Deadline = DateTime.Now.AddDays(7),
                DeadlineTime = new TimeSpan(12, 0, 0),
                ImageUrl = "new-image.jpg",
                Country = "NewCountry",
                SportId = "sport2"
            };

            _tournamentService.Setup(x => x.GetById("tournament1"))
                .ReturnsAsync(existingTournament);

            _tournamentService.Setup(x => x.GetAll())
                .ReturnsAsync(new List<Tournament>());

            _tournamentService.Setup(x => x.Update(It.IsAny<Tournament>()))
                .Returns(Task.CompletedTask);

            var result = await _controller.EditTournament(tournament, returnUrl) as RedirectResult;

            Assert.That(result, Is.Not.Null);
            Assert.That(result.Url, Is.EqualTo(returnUrl));
            _tournamentService.Verify(x => x.Update(It.Is<Tournament>(t =>
                t.Name == tournament.Name &&
                t.Description == tournament.Description &&
                t.ImageUrl == tournament.ImageUrl &&
                t.Country == tournament.Country &&
                t.SportId == tournament.SportId
            )), Times.Once);
        }

        [Test]
        public async Task EditTournament_Post_AsOrganizer_WithInvalidModel_ReturnsViewWithErrors()
        {
            var returnUrl = "/return/url";
            var existingTournament = new Tournament
            {
                Id = "tournament1",
                Name = "Old Name",
                Description = "Old Description",
                OrganizerId = _currentUser.Id,
                Date = DateTime.Now.AddDays(5).ToString(),
                Deadline = DateTime.Now.AddDays(3).ToString(),
                ImageUrl = "old-image.jpg",
                Country = "OldCountry",
                SportId = "sport1"
            };

            var tournament = new TournamentViewModel
            {
                Id = "tournament1",
                Name = "Updated Name",
                Description = null,
                OrganizerId = _currentUser.Id,
                Date = DateTime.Now.AddDays(10),
                DateTimer = new TimeSpan(14, 0, 0),
                Deadline = DateTime.Now.AddDays(7),
                DeadlineTime = new TimeSpan(12, 0, 0),
                ImageUrl = "new-image.jpg",
                Country = "NewCountry",
                SportId = "sport2"
            };

            _tournamentService.Setup(x => x.GetById("tournament1"))
                .ReturnsAsync(existingTournament);

            _tournamentService.Setup(x => x.GetAll())
                .ReturnsAsync(new List<Tournament>());

            var sports = new List<Sport> { new Sport { Id = "sport1", Name = "Football" } };
            var countries = new List<string> { "Country1", "Country2" };

            _sportService.Setup(x => x.GetAll())
                .ReturnsAsync(sports);
            _mockCountryService.Setup(x => x.GetAllCountries())
    .Returns(countries.Select(c => new SelectListItem { Text = c, Value = c }).ToList());

            var result = await _controller.EditTournament(tournament, returnUrl) as ViewResult;

            Assert.That(result, Is.Not.Null);
            Assert.That(result.ViewData.ModelState.IsValid, Is.False);
            Assert.That(result.ViewData.ModelState.ErrorCount, Is.GreaterThan(0));
            _tournamentService.Verify(x => x.Update(It.IsAny<Tournament>()), Times.Never);
        }

        [Test]
        public async Task EditTournament_Post_AsNonOrganizer_WithValidModel_UpdatesRestrictedFieldsAndRedirects()
        {
            var returnUrl = "/return/url";
            var otherUserId = "otherUser123";
            var existingTournament = new Tournament
            {
                Id = "tournament1",
                Name = "Old Name",
                Description = "Old Description",
                OrganizerId = otherUserId,
                Date = DateTime.Now.AddDays(5).ToString(),
                Deadline = DateTime.Now.AddDays(3).ToString(),
                ImageUrl = "old-image.jpg",
                Country = "OldCountry",
                SportId = "sport1"
            };

            var tournament = new TournamentViewModel
            {
                Id = "tournament1",
                Name = "Updated Name",
                Description = "Updated Description",
                OrganizerId = otherUserId,
                Date = DateTime.Now.AddDays(10),
                DateTimer = new TimeSpan(14, 0, 0),
                Deadline = DateTime.Now.AddDays(7),
                DeadlineTime = new TimeSpan(12, 0, 0),
                ImageUrl = "new-image.jpg",
                Country = "NewCountry",
                SportId = "sport2"
            };

            _tournamentService.Setup(x => x.GetById("tournament1"))
                .ReturnsAsync(existingTournament);

            _tournamentService.Setup(x => x.GetAll())
                .ReturnsAsync(new List<Tournament>());

            _tournamentService.Setup(x => x.Update(It.IsAny<Tournament>()))
                .Returns(Task.CompletedTask);

            var result = await _controller.EditTournament(tournament, returnUrl) as RedirectResult;

            Assert.That(result, Is.Not.Null);
            Assert.That(result.Url, Is.EqualTo(returnUrl));

            _tournamentService.Verify(x => x.Update(It.Is<Tournament>(t =>
    t.Name == tournament.Name &&
    t.Description == tournament.Description &&
    t.ImageUrl == tournament.ImageUrl &&
    t.Country == existingTournament.Country && t.SportId == existingTournament.SportId && t.Date == existingTournament.Date && t.Deadline == existingTournament.Deadline)), Times.Once);
        }

        [Test]
        public async Task EditTournament_Post_AsNonOrganizer_WithInvalidModel_ReturnsViewWithErrors()
        {
            var returnUrl = "/return/url";
            var otherUserId = "otherUser123";
            var existingTournament = new Tournament
            {
                Id = "tournament1",
                Name = "Old Name",
                Description = "Old Description",
                OrganizerId = otherUserId,
                Date = DateTime.Now.AddDays(5).ToString(),
                Deadline = DateTime.Now.AddDays(3).ToString(),
                ImageUrl = "old-image.jpg",
                Country = "OldCountry",
                SportId = "sport1"
            };

            var tournament = new TournamentViewModel
            {
                Id = "tournament1",
                Name = "New",
                Description = "Desc",
                OrganizerId = otherUserId,
                ImageUrl = null,
                Country = existingTournament.Country,
                SportId = existingTournament.SportId
            };

            _tournamentService.Setup(x => x.GetById("tournament1"))
                .ReturnsAsync(existingTournament);

            _tournamentService.Setup(x => x.GetAll())
                .ReturnsAsync(new List<Tournament>());

            var result = await _controller.EditTournament(tournament, returnUrl) as ViewResult;

            Assert.That(result, Is.Not.Null);
            Assert.That(result.ViewData.ModelState.IsValid, Is.False);
            Assert.That(result.ViewData.ModelState.ErrorCount, Is.GreaterThan(0));
            _tournamentService.Verify(x => x.Update(It.IsAny<Tournament>()), Times.Never);
        }

        [Test]
        public async Task EditTournament_Post_WithExistingNameAndDescription_ReturnsViewWithErrors()
        {
            var returnUrl = "/return/url";
            var existingTournament = new Tournament
            {
                Id = "tournament1",
                Name = "Old Name",
                Description = "Old Description",
                OrganizerId = _currentUser.Id,
                Date = DateTime.Now.AddDays(5).ToString(),
                Deadline = DateTime.Now.AddDays(3).ToString(),
                ImageUrl = "old-image.jpg",
                Country = "OldCountry",
                SportId = "sport1"
            };

            var anotherTournament = new Tournament
            {
                Id = "tournament2",
                Name = "Existing Name",
                Description = "Existing Description",
                OrganizerId = "anotherUser",
                Date = DateTime.Now.AddDays(10).ToString(),
                Deadline = DateTime.Now.AddDays(8).ToString(),
                ImageUrl = "another-image.jpg",
                Country = "AnotherCountry",
                SportId = "sport2"
            };

            var tournament = new TournamentViewModel
            {
                Id = "tournament1",
                Name = "Existing Name",
                Description = "Existing Description",
                OrganizerId = _currentUser.Id,
                Date = DateTime.Now.AddDays(10),
                DateTimer = new TimeSpan(14, 0, 0),
                Deadline = DateTime.Now.AddDays(7),
                DeadlineTime = new TimeSpan(12, 0, 0),
                ImageUrl = "new-image.jpg",
                Country = "NewCountry",
                SportId = "sport2"
            };

            _tournamentService.Setup(x => x.GetById("tournament1"))
                .ReturnsAsync(existingTournament);

            _tournamentService.Setup(x => x.GetAll())
                .ReturnsAsync(new List<Tournament> { existingTournament, anotherTournament });

            var sports = new List<Sport> { new Sport { Id = "sport1", Name = "Football" } };
            var countries = new List<string> { "Country1", "Country2" };

            _sportService.Setup(x => x.GetAll())
                .ReturnsAsync(sports);

            _mockCountryService.Setup(x => x.GetAllCountries())
    .Returns(countries.Select(c => new SelectListItem { Text = c, Value = c }).ToList());

            var result = await _controller.EditTournament(tournament, returnUrl) as ViewResult;

            Assert.That(result, Is.Not.Null);
            Assert.That(result.ViewData.ModelState.IsValid, Is.False);
            Assert.That(result.ViewData.ModelState["Name"].Errors.Count, Is.EqualTo(1));
            Assert.That(result.ViewData.ModelState["Description"].Errors.Count, Is.EqualTo(1));
            _tournamentService.Verify(x => x.Update(It.IsAny<Tournament>()), Times.Never);
        }

        [Test]
        public async Task EditTournament_Post_AsOrganizer_WithDeadlineInPast_ReturnsViewWithError()
        {
            var returnUrl = "/return/url";
            var existingTournament = new Tournament
            {
                Id = "tournament1",
                Name = "Old Name",
                Description = "Old Description",
                OrganizerId = _currentUser.Id,
                Date = DateTime.Now.AddDays(5).ToString(),
                Deadline = DateTime.Now.AddDays(3).ToString(),
                ImageUrl = "old-image.jpg",
                Country = "OldCountry",
                SportId = "sport1"
            };

            var tournament = new TournamentViewModel
            {
                Id = "tournament1",
                Name = "Updated Name",
                Description = "Updated Description",
                OrganizerId = _currentUser.Id,
                Date = DateTime.Now.AddDays(10),
                DateTimer = new TimeSpan(14, 0, 0),
                Deadline = DateTime.Now.AddDays(-1),
                DeadlineTime = new TimeSpan(12, 0, 0),
                ImageUrl = "new-image.jpg",
                Country = "NewCountry",
                SportId = "sport2"
            };

            _tournamentService.Setup(x => x.GetById("tournament1"))
                .ReturnsAsync(existingTournament);

            _tournamentService.Setup(x => x.GetAll())
                .ReturnsAsync(new List<Tournament> { existingTournament });

            var sports = new List<Sport> { new Sport { Id = "sport1", Name = "Football" } };
            var countries = new List<string> { "Country1", "Country2" };

            _sportService.Setup(x => x.GetAll())
                .ReturnsAsync(sports);
            _mockCountryService.Setup(x => x.GetAllCountries())
    .Returns(countries.Select(c => new SelectListItem { Text = c, Value = c }).ToList());

            var result = await _controller.EditTournament(tournament, returnUrl) as ViewResult;

            Assert.That(result, Is.Not.Null);
            Assert.That(result.ViewData.ModelState.IsValid, Is.False);
            Assert.That(result.ViewData.ModelState["DeadlineTime"].Errors.Count, Is.EqualTo(1));
            _tournamentService.Verify(x => x.Update(It.IsAny<Tournament>()), Times.Never);
        }

        [Test]
        public async Task EditTournament_Post_AsOrganizer_WithEventBeforeDeadline_ReturnsViewWithError()
        {
            var returnUrl = "/return/url";
            var now = DateTime.Now;
            var existingTournament = new Tournament
            {
                Id = "tournament1",
                Name = "Old Name",
                Description = "Old Description",
                OrganizerId = _currentUser.Id,
                Date = now.AddDays(5).ToString(),
                Deadline = now.AddDays(3).ToString(),
                ImageUrl = "old-image.jpg",
                Country = "OldCountry",
                SportId = "sport1"
            };

            var tournament = new TournamentViewModel
            {
                Id = "tournament1",
                Name = "Updated Name",
                Description = "Updated Description",
                OrganizerId = _currentUser.Id,
                Date = now.AddDays(3),
                DateTimer = new TimeSpan(12, 0, 0),
                Deadline = now.AddDays(5),
                DeadlineTime = new TimeSpan(14, 0, 0),
                ImageUrl = "new-image.jpg",
                Country = "NewCountry",
                SportId = "sport2"
            };

            _tournamentService.Setup(x => x.GetById("tournament1"))
                .ReturnsAsync(existingTournament);

            _tournamentService.Setup(x => x.GetAll())
                .ReturnsAsync(new List<Tournament> { existingTournament });

            var sports = new List<Sport> { new Sport { Id = "sport1", Name = "Football" } };
            var countries = new List<string> { "Country1", "Country2" };

            _sportService.Setup(x => x.GetAll())
                .ReturnsAsync(sports);
            _mockCountryService.Setup(x => x.GetAllCountries())
    .Returns(countries.Select(c => new SelectListItem { Text = c, Value = c }).ToList());

            var result = await _controller.EditTournament(tournament, returnUrl) as ViewResult;

            Assert.That(result, Is.Not.Null);
            Assert.That(result.ViewData.ModelState.IsValid, Is.False);
            Assert.That(result.ViewData.ModelState["DateOrder"].Errors.Count, Is.EqualTo(1));
            _tournamentService.Verify(x => x.Update(It.IsAny<Tournament>()), Times.Never);
        }
        [Test]
        public async Task DeleteTournament_Get_ValidId_ReturnsViewWithModel()
        {
            var tournamentId = "test-id";
            var returnUrl = "/return-url";
            var tournament = new Tournament
            {
                Id = tournamentId,
                Name = "Test Tournament",
                Description = "Test Description",
                ImageUrl = "test.jpg",
                Sport = new Sport { Id = "sport-id" }
            };

            _tournamentService.Setup(r => r.AllWithIncludes(It.IsAny<Expression<Func<Tournament, object>>>()))
                .ReturnsAsync(new List<Tournament> { tournament });

            var result = await _controller.DeleteTournament(tournamentId, returnUrl) as ViewResult;

            Assert.That(result, Is.Not.Null);
            Assert.That(result.ViewName, Is.Null.Or.Empty); Assert.That(result.Model, Is.TypeOf<TournamentViewModel>());
            var model = result.Model as TournamentViewModel;
            Assert.That(model.Id, Is.EqualTo(tournamentId));
            Assert.That(model.Name, Is.EqualTo("Test Tournament"));
            Assert.That(model.SportId, Is.EqualTo("sport-id"));
        }
        [Test]
        public async Task DeleteTournament_Get_InvalidId_ReturnsNotFound()
        {
            _tournamentService.Setup(r => r.AllWithIncludes(It.IsAny<Expression<Func<Tournament, object>>>(), It.IsAny<Expression<Func<Tournament, object>>>()))
    .ReturnsAsync(new List<Tournament>());

            var result = await _controller.DeleteTournament("invalid-id", "/return-url");

            Assert.That(result, Is.TypeOf<ViewResult>());
        }

        [Test]
        public async Task DeleteTournament_Post_ConfirmTextValid_DeletesTournamentAndRedirects()
        {
            var tournamentId = "test-id";
            var returnUrl = "/return-url";
            var tournament = new Tournament { Id = tournamentId };
            var model = new TournamentViewModel { Id = tournamentId };
            var participations = new List<Participation> { new Participation { TournamentId = tournamentId } };

            _tournamentService.Setup(r => r.GetById(tournamentId)).ReturnsAsync(tournament);
            _participationService.Setup(r => r.GetAllBy(It.IsAny<Expression<Func<Participation, bool>>>()))
                .ReturnsAsync(participations);

            var result = await _controller.DeleteTournament("ПОТВЪРДИ", model, returnUrl) as RedirectResult;

            Assert.That(result, Is.Not.Null);
            Assert.That(result.Url, Is.EqualTo(returnUrl));

            _participationService.Verify(r => r.DeleteRange(participations), Times.Once);
            _tournamentService.Verify(r => r.Delete(tournament), Times.Once);
        }

        [Test]
        public async Task DeleteTournament_Post_ConfirmTextInvalid_ReturnsViewWithModel()
        {
            var tournamentId = "test-id";
            var returnUrl = "/return-url";
            var tournament = new Tournament
            {
                Id = tournamentId,
                Name = "Test Tournament",
                Description = "Test Description",
                ImageUrl = "test.jpg",
                SportId = "sport-id"
            };
            var model = new TournamentViewModel { Id = tournamentId };

            _tournamentService.Setup(r => r.GetById(tournamentId)).ReturnsAsync(tournament);

            var result = await _controller.DeleteTournament("WRONG", model, returnUrl) as ViewResult;

            Assert.That(result, Is.Not.Null);
            Assert.That(result.ViewName, Is.Null.Or.Empty); Assert.That(result.Model, Is.TypeOf<TournamentViewModel>());

            var resultModel = result.Model as TournamentViewModel;
            Assert.That(resultModel.Id, Is.EqualTo(tournamentId));
            Assert.That(resultModel.Name, Is.EqualTo("Test Tournament"));
            Assert.That(resultModel.SportId, Is.EqualTo("sport-id"));

            _participationService.Verify(r => r.DeleteRange(It.IsAny<IEnumerable<Participation>>()), Times.Never);
            _tournamentService.Verify(r => r.Delete(It.IsAny<Tournament>()), Times.Never);
        }

        [Test]
        public async Task SportTournaments_ValidId_WithoutFilter_ReturnsViewWithModel()
        {
            var sportId = "sport-id";
            var sport = new Sport { Id = sportId, Name = "Test Sport" };

            _sportService.Setup(r => r.GetById(sportId)).ReturnsAsync(sport);

            var result = await _controller.SportTournaments(sportId, null) as ViewResult;

            Assert.That(result, Is.Not.Null);
            Assert.That(result.ViewName, Is.Null.Or.Empty); Assert.That(result.Model, Is.TypeOf<TournamentViewModel>());
        }

        [Test]
        public async Task SportTournaments_InvalidId_ReturnsNotFound()
        {
            _sportService.Setup(r => r.GetById("invalid-id")).ReturnsAsync((Sport)null);

            var result = await _controller.SportTournaments("invalid-id", null);

            Assert.That(result, Is.TypeOf<ViewResult>());
        }

        [Test]
        public async Task SportTournaments_ValidId_WithFilter_ReturnsFilteredResults()
        {
            var sportId = "sport-id";
            var sport = new Sport { Id = sportId, Name = "Test Sport" };
            var filter = new TournamentViewModel
            {
                Name = "Test",
                Country = "Bulgaria",
                OrganizerName = "Organizer",
                StartDate = DateTime.Now.AddDays(-5),
                EndDate = DateTime.Now.AddDays(5)
            };

            var tournaments = new List<Tournament>
    {
        new Tournament
        {
            Id = "t1",
            Name = "Test Tournament",
            Country = "Bulgaria",
            Date = DateTime.Now.ToString(),
            SportId = sportId,
            Organizer = new SportConnectUser { FullName = "Organizer Name" }
        },
        new Tournament
        {
            Id = "t2",
            Name = "Another Tournament",
            Country = "Greece",
            SportId = sportId
        }
    };

            _sportService.Setup(r => r.GetById(sportId)).ReturnsAsync(sport);
            _tournamentService.Setup(r => r.AllWithIncludes(
                It.IsAny<Expression<Func<Tournament, object>>>(),
                It.IsAny<Expression<Func<Tournament, object>>>(),
                It.IsAny<Expression<Func<Tournament, object>>>()))
                    .ReturnsAsync(tournaments); _mockCountryService.Setup(c => c.GetAllCountries()).Returns(
    new List<SelectListItem>
    {
        new SelectListItem { Text = "Bulgaria", Value = "Bulgaria" },
        new SelectListItem { Text = "Greece", Value = "Greece" }
    });

            var result = await _controller.SportTournaments(sportId, filter) as ViewResult;

            Assert.That(result, Is.Not.Null);
            Assert.That(result.Model, Is.TypeOf<TournamentViewModel>());

            var model = result.Model as TournamentViewModel;
            Assert.That(model.FilteredTournaments, Is.Not.Null);
            Assert.That(model.SportId, Is.EqualTo(sportId));
            Assert.That(model.Name, Is.EqualTo("Test"));
            Assert.That(model.Country, Is.EqualTo("Bulgaria"));
        }

        [Test]
        public async Task UserTournaments_ValidIds_ReturnsViewWithModel()
        {
            string userId = "user-id";
            string tournamentId = "tournament-id";
            var user = new SportConnectUser { Id = userId, UserName = "TestUser" };
            var tournament = new Tournament { Id = tournamentId };

            _userService.Setup(r => r.GetById(userId)).ReturnsAsync(user);
            _tournamentService.Setup(r => r.GetById(tournamentId)).ReturnsAsync(tournament);
            _tournamentService.Setup(r => r.AllWithIncludes(
                It.IsAny<Expression<Func<Tournament, object>>>(),
                It.IsAny<Expression<Func<Tournament, object>>>(),
                It.IsAny<Expression<Func<Tournament, object>>>()))
                    .ReturnsAsync(new List<Tournament>());

            var result = await _controller.UserTournaments(userId, tournamentId, null, null) as ViewResult;

            Assert.That(result, Is.Not.Null);
            Assert.That(result.ViewName, Is.Null.Or.Empty); Assert.That(result.Model, Is.TypeOf<TournamentViewModel>());
            Assert.That(result.ViewData["OtherUserId"], Is.EqualTo(userId));
            Assert.That(result.ViewData["CheckedUser"], Is.EqualTo("TestUser"));
        }

        [Test]
        public async Task UserTournaments_InvalidIds_ReturnsNotFound()
        {
            _userService.Setup(r => r.GetById("invalid-user")).ReturnsAsync((SportConnectUser)null);
            _tournamentService.Setup(r => r.GetById("valid-tournament")).ReturnsAsync(new Tournament());

            var result = await _controller.UserTournaments("invalid-user", "valid-tournament", null, null);

            Assert.That(result, Is.TypeOf<ViewResult>());
        }

        [Test]
        public async Task UserTournaments_WithFilter_ReturnsFilteredResults()
        {
            string userId = "user-id";
            string tournamentId = "tournament-id";
            var user = new SportConnectUser { Id = userId, UserName = "TestUser" };
            var tournament = new Tournament { Id = tournamentId };

            var filter = new TournamentViewModel
            {
                Name = "Test",
                Country = "Bulgaria",
                SportId = "sport-id",
            };

            var tournaments = new List<Tournament>
    {
        new Tournament
        {
            Id = "t1",
            Name = "Test Tournament",
            Country = "Bulgaria",
            Date = DateTime.Now.ToString(),
            SportId = "sport-id",
            OrganizerId = userId
        },
        new Tournament
        {
            Id = "t2",
            Name = "Another Tournament",
            SportId = "sporter-id",
            Country = "Greece",
            OrganizerId = userId
        }
    };

            _userService.Setup(r => r.GetById(userId)).ReturnsAsync(user);
            _tournamentService.Setup(r => r.GetById(tournamentId)).ReturnsAsync(tournament);
            _tournamentService.Setup(r => r.AllWithIncludes(
                It.IsAny<Expression<Func<Tournament, object>>>(),
                It.IsAny<Expression<Func<Tournament, object>>>(),
                It.IsAny<Expression<Func<Tournament, object>>>()))
                    .ReturnsAsync(tournaments);
            _sportService.Setup(r => r.GetAll()).ReturnsAsync(new List<Sport>()); _mockCountryService.Setup(c => c.GetAllCountries()).Returns(
    new List<SelectListItem>
    {
        new SelectListItem { Text = "Bulgaria", Value = "Bulgaria" },
        new SelectListItem { Text = "Greece", Value = "Greece" }
    });

            var result = await _controller.UserTournaments(userId, tournamentId, filter, "/return-url") as ViewResult;

            Assert.That(result, Is.Not.Null);
            Assert.That(result.Model, Is.TypeOf<TournamentViewModel>());

            var model = result.Model as TournamentViewModel;
            Assert.That(model.FilteredTournaments, Is.Not.Null);
            Assert.That(model.PreviousId, Is.EqualTo(tournamentId));
            Assert.That(model.Name, Is.EqualTo("Test"));
            Assert.That(model.Country, Is.EqualTo("Bulgaria"));
            Assert.That(model.SportId, Is.EqualTo("sport-id"));
        }

        [Test]
        public async Task MyTournaments_WithoutFilter_ReturnsViewWithModel()
        {
            var currentUserId = "current-user-id";
            var currentUser = new SportConnectUser { Id = currentUserId };

            _mockUserManager.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
                .ReturnsAsync(currentUser);
            _tournamentService.Setup(r => r.AllWithIncludes(
                It.IsAny<Expression<Func<Tournament, object>>>(),
                It.IsAny<Expression<Func<Tournament, object>>>(),
                It.IsAny<Expression<Func<Tournament, object>>>()))
                    .ReturnsAsync(new List<Tournament>());

            var result = await _controller.MyTournaments(null) as ViewResult;

            Assert.That(result, Is.Not.Null);
            Assert.That(result.ViewName, Is.Null.Or.Empty); Assert.That(result.Model, Is.TypeOf<TournamentViewModel>());
            Assert.That(result.ViewData["UserId"], Is.EqualTo(currentUserId));
        }

        [Test]
        public async Task MyTournaments_WithFilter_ReturnsFilteredResults()
        {
            var currentUserId = "current-user-id";
            var currentUser = new SportConnectUser { Id = currentUserId };

            var filter = new TournamentViewModel
            {
                Name = "Test",
                Country = "Bulgaria",
                SportId = "sport-id",
            };

            var tournaments = new List<Tournament>
    {
        new Tournament
        {
            Id = "t1",
            Name = "Test Tournament",
            Country = "Bulgaria",
            Date = DateTime.Now.ToString(),
            SportId = "sport-id",
            OrganizerId = currentUserId
        },
        new Tournament
        {
            Id = "t2",
            SportId = "sportere-id",
            Name = "Another Tournament",
            Country = "Greece",
            OrganizerId = currentUserId
        }
    };

            _mockUserManager.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
                .ReturnsAsync(currentUser);
            _tournamentService.Setup(r => r.AllWithIncludes(
                It.IsAny<Expression<Func<Tournament, object>>>(),
                It.IsAny<Expression<Func<Tournament, object>>>(),
                It.IsAny<Expression<Func<Tournament, object>>>()))
                    .ReturnsAsync(tournaments);
            _sportService.Setup(r => r.GetAll()).ReturnsAsync(new List<Sport>()); _mockCountryService.Setup(c => c.GetAllCountries()).Returns(
    new List<SelectListItem>
    {
        new SelectListItem { Text = "Bulgaria", Value = "Bulgaria" },
        new SelectListItem { Text = "Greece", Value = "Greece" }
    });

            var result = await _controller.MyTournaments(filter) as ViewResult;

            Assert.That(result, Is.Not.Null);
            Assert.That(result.Model, Is.TypeOf<TournamentViewModel>());

            var model = result.Model as TournamentViewModel;
            Assert.That(model.FilteredTournaments, Is.Not.Null);
            Assert.That(model.Name, Is.EqualTo("Test"));
            Assert.That(model.Country, Is.EqualTo("Bulgaria"));
            Assert.That(model.SportId, Is.EqualTo("sport-id"));
        }

        [Test]
        public void Error_ReturnsErrorView()
        {
            var httpContext = new DefaultHttpContext();
            _controller.ControllerContext = new ControllerContext()
            {
                HttpContext = httpContext
            };

            var result = _controller.Error() as ViewResult;

            Assert.That(result, Is.Not.Null);
            Assert.That(result.ViewName, Is.Null.Or.Empty); Assert.That(result.Model, Is.TypeOf<ErrorViewModel>());

            var model = result.Model as ErrorViewModel;
            Assert.That(model.RequestId, Is.Not.Null);
        }
    }

    public class TestSession : ISession
    {
        private readonly SessionStorage _sessionStorage;

        public TestSession(SessionStorage sessionStorage)
        {
            _sessionStorage = sessionStorage;
        }

        public string Id => "test_session_id";
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

    public class SessionStorage : Dictionary<string, byte[]>
    {
        public void SetString(string key, string value)
        {
            this[key] = System.Text.Encoding.UTF8.GetBytes(value);
        }

        public string GetString(string key)
        {
            if (TryGetValue(key, out byte[] value))
            {
                return System.Text.Encoding.UTF8.GetString(value);
            }
            return null;
        }
    }

    public static class SessionExtensions
    {
        public static void SetString(this ISession session, string key, string value)
        {
            session.Set(key, System.Text.Encoding.UTF8.GetBytes(value));
        }

        public static string GetString(this ISession session, string key)
        {
            var data = session.Get(key);
            if (data == null)
            {
                return null;
            }
            return System.Text.Encoding.UTF8.GetString(data);
        }
    }

    public static class TournamentViewModelExtensions
    {
        public static async Task<Tournament> ToTournament(this TournamentViewModel model)
        {
            return new Tournament
            {
                Id = model.Id ?? Guid.NewGuid().ToString(),
                Name = model.Name,
                Description = model.Description,
                OrganizerId = model.OrganizerId,
                Date = model.Date.ToString(),
                Deadline = model.Deadline.ToString(),
                ImageUrl = model.ImageUrl,
                Country = model.Country,
                SportId = model.SportId
            };
        }
    }
}
#endregion