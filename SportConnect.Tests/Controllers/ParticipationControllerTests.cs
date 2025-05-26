using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
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
using System.Security.Claims;

namespace SportConnect.Tests.Controllers
{
    [TestFixture]
    public class ParticipationControllerTests
    {
        private Mock<UserManager<SportConnectUser>> _mockUserManager;
        public Mock<ITournamentService> _tournamentService;
        public Mock<IUserService> _userService;
        public Mock<ISportService> _sportService;
        public Mock<IParticipationService> _participationService;
        private Mock<CountryService> _mockCountryService;
        private ParticipationController _controller;

        [TearDown]
        public void TearDown()
        {
            _controller.Dispose();
        }

        [SetUp]
        public void Setup()
        {
            var userStoreMock = new Mock<IUserStore<SportConnectUser>>();
            _mockUserManager = new Mock<UserManager<SportConnectUser>>(
                userStoreMock.Object, null, null, null, null, null, null, null, null);

            _tournamentService = new Mock<ITournamentService>();
            _userService = new Mock<IUserService>();
            _sportService = new Mock<ISportService>();
            _participationService = new Mock<IParticipationService>();
            _mockCountryService = new Mock<CountryService>();

            _controller = new ParticipationController(
                _mockUserManager.Object,
                _tournamentService.Object,
                _userService.Object,
                _sportService.Object,
                _participationService.Object,
                _mockCountryService.Object
            );

            var httpContext = new DefaultHttpContext();
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
            Assert.IsNotNull(_controller); Assert.IsInstanceOf<ParticipationController>(_controller); Assert.AreEqual(_mockUserManager.Object, _controller._userManager);
            Assert.AreEqual(_sportService.Object, _controller._sportService);
            Assert.AreEqual(_tournamentService.Object, _controller._tournamentService);
            Assert.AreEqual(_participationService.Object, _controller._participationService);
            Assert.AreEqual(_userService.Object, _controller._userService);
            Assert.AreEqual(_mockCountryService.Object, _controller._countryService);
        }

        [Test]
        public async Task MyParticipations_NoFilter_ReturnsDefaultViewModel()
        {
            var tournaments = new List<Tournament>
            {
                new Tournament { Id = "1", Name = "Summer Tournament", Date = DateTime.Now.AddMonths(1).ToString() },
                new Tournament { Id = "2", Name = "Winter Tournament", Date = DateTime.Now.AddMonths(3).ToString() }
            };

            _tournamentService.Setup(r => r.GetAll())
                .ReturnsAsync(tournaments);

            var result = await _controller.MyParticipations(null) as ViewResult;

            Assert.IsNotNull(result);
            Assert.That(result.Model, Is.InstanceOf(typeof(TournamentViewModel)));
            var model = result.Model as TournamentViewModel;
            Assert.IsNotNull(model);
            Assert.AreEqual(2, model.Tournaments.Count);
            Assert.AreEqual(2, model.FilteredTournaments.Count);
            Assert.AreEqual(null, model.Name);
            Assert.AreEqual(null, model.SportId);
            Assert.AreEqual(null, model.Country);
            Assert.AreEqual(null, model.StartDate);
            Assert.AreEqual(null, model.OrganizerName);
            Assert.AreEqual(null, model.EndDate);
            Assert.AreEqual(null, model.Approved);
        }

        [Test]
        public async Task MyParticipations_WithFilter_ReturnsFilteredResults()
        {
            var userId = "user123";
            var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
            }));

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };
            _controller.ControllerContext.HttpContext.Session = new MockHttpSession();

            var mockUser = new SportConnectUser { Id = userId };
            _mockUserManager.Setup(u => u.GetUserAsync(user))
                .ReturnsAsync(mockUser);

            var tournaments = new List<Tournament>
            {
                new Tournament
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = "Tennis Open",
                    Date = DateTime.Now.AddDays(10).ToString(),
                    Country = "USA",
                    SportId = Guid.NewGuid().ToString(),
                    Sport = new Sport { Name = "Tennis" },
                    Organizer = new SportConnectUser { UserName = "organizer1", FullName = "Org One" },
                    Participations = new List<Participation>
                    {
                        new Participation { ParticipantId = userId, Approved = true }
                    }
                },
                new Tournament
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = "Football Championship",
                    Date = DateTime.Now.AddDays(20).ToString(),
                    Country = "Germany",
                    SportId = Guid.NewGuid().ToString(),
                    Sport = new Sport { Name = "Football" },
                    Organizer = new SportConnectUser { UserName = "organizer2", FullName = "Org Two" },
                    Participations = new List<Participation>
                    {
                        new Participation { ParticipantId = userId, Approved = false }
                    }
                }
            };

            _tournamentService.Setup(r => r.AllWithIncludes(
                It.IsAny<System.Linq.Expressions.Expression<Func<Tournament, object>>>(),
                It.IsAny<System.Linq.Expressions.Expression<Func<Tournament, object>>>(),
                It.IsAny<System.Linq.Expressions.Expression<Func<Tournament, object>>>()))
                .ReturnsAsync(tournaments);

            var filter = new TournamentViewModel
            {
                Approved = true,
                Name = "Tennis"
            };

            var result = await _controller.MyParticipations(filter) as ViewResult;

            Assert.IsNotNull(result);
            var model = result.Model as TournamentViewModel;
            Assert.IsNotNull(model);
            Assert.AreEqual(1, model.FilteredTournaments.Count);
            Assert.AreEqual("Tennis Open", model.FilteredTournaments[0].Name);
        }
        [Test]
        public async Task TournamentParticipations_NoFilter_ReturnsDefaultViewModel()
        {
            var tournamentId = Guid.NewGuid().ToString();
            var tournament = new Tournament { Id = tournamentId, Name = "Test Tournament" };

            var user1 = new SportConnectUser
            {
                Id = "user123",
                UserName = "testUser1",
                Participations = new List<Participation>
        {
            new Participation { TournamentId = tournamentId }
        }
            };

            var user2 = new SportConnectUser
            {
                Id = "user456",
                UserName = "testUser2",
                Participations = new List<Participation>
        {
            new Participation { TournamentId = tournamentId }
        }
            };

            var users = new List<SportConnectUser> { user1, user2 };

            _tournamentService.Setup(r => r.GetById(tournamentId))
    .ReturnsAsync(tournament);

            _userService.Setup(r => r.GetAll())
                .ReturnsAsync(users);

            var result = await _controller.TournamentParticipations(tournamentId, "/return", null) as ViewResult;

            Assert.IsNotNull(result);
            Assert.That(result.Model, Is.InstanceOf(typeof(UserViewModel)));

            var model = result.Model as UserViewModel;
            Assert.IsNotNull(model);
            Assert.AreEqual(2, model.Users.Count);
            Assert.AreEqual(2, model.FilteredUsers.Count);

            Assert.IsNull(model.BirthYear);
            Assert.IsNull(model.UserName);
            Assert.IsNull(model.Country);
            Assert.IsNull(model.Email);
            Assert.IsNull(model.Approved);
        }
        [Test]
        public async Task TournamentParticipations_WithFilter_ReturnsFilteredResults()
        {
            var tournamentId = Guid.NewGuid().ToString();
            var userId = "user123";
            var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
            }));

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };
            _controller.ControllerContext.HttpContext.Session = new MockHttpSession();

            var mockUser = new SportConnectUser { Id = userId };
            _mockUserManager.Setup(u => u.GetUserAsync(user))
                .ReturnsAsync(mockUser);

            var users = new List<SportConnectUser>
            {
                new SportConnectUser
                {
                    Id = "user1",
                    UserName = "tennisplayer",
                    FullName = "Tennis Player",
                    Email = "tennis@example.com",
                    Country = "USA",
                    DateOfBirth = "1990-05-15",
                    Participations = new List<Participation>
                    {
                        new Participation { TournamentId = Guid.Parse(tournamentId).ToString(), ParticipantId = "user1", Approved = true }
                    }
                },
                new SportConnectUser
                {
                    Id = "user2",
                    UserName = "footballplayer",
                    FullName = "Football Player",
                    Email = "football@example.com",
                    Country = "Spain",
                    DateOfBirth = "1995-08-20",
                    Participations = new List<Participation>
                    {
                        new Participation { TournamentId = Guid.Parse(tournamentId).ToString(), ParticipantId = "user2", Approved = false }
                    }
                }
            };

            _userService.Setup(r => r.AllWithIncludes(
                It.IsAny<System.Linq.Expressions.Expression<Func<SportConnectUser, object>>>()))
                .ReturnsAsync(users);

            var tournament = new Tournament
            {
                Id = Guid.Parse(tournamentId).ToString(),
                Name = "Tennis Open"
            };

            _tournamentService.Setup(r => r.GetById(tournamentId))
                .ReturnsAsync(tournament);

            var sports = new List<Sport>
            {
                new Sport { Id = Guid.NewGuid().ToString(), Name = "Tennis" },
                new Sport { Id = Guid.NewGuid().ToString(), Name = "Football" }
            };

            _sportService.Setup(r => r.GetAll())
                .ReturnsAsync(sports);
            _mockCountryService.Setup(c => c.GetAllCountries())
    .Returns(new List<SelectListItem>
    {
        new SelectListItem { Text = "USA", Value = "USA" },
        new SelectListItem { Text = "Spain", Value = "Spain" },
        new SelectListItem { Text = "Germany", Value = "Germany" }
    });

            var filter = new UserViewModel
            {
                UserName = "Tennis",
                Country = "USA",
                Approved = true
            };

            var result = await _controller.TournamentParticipations(tournamentId, "/return", filter) as ViewResult;

            Assert.IsNotNull(result);
            var model = result.Model as UserViewModel;
            Assert.IsNotNull(model);
            Assert.AreEqual(1, model.FilteredUsers.Count);
            Assert.AreEqual("tennisplayer", model.FilteredUsers[0].UserName);
        }
        [Test]
        public async Task UserParticipations_NoFilter_ReturnsDefaultViewModel()
        {
            var userId = "user123";
            var tournamentId = "1";

            var user = new SportConnectUser
            {
                Id = userId,
                UserName = "testUser"
            };

            _userService.Setup(r => r.GetById(userId))
                .ReturnsAsync(user);

            var tournament = new Tournament { Id = tournamentId, Name = "Test Tournament" };
            _tournamentService.Setup(r => r.GetById(tournamentId))
                .ReturnsAsync(tournament);

            var tournaments = new List<Tournament>
    {
        new Tournament { Id = "1", Name = "Summer Tournament", Date = DateTime.Now.AddMonths(1).ToString() },
        new Tournament { Id = "2", Name = "Winter Tournament", Date = DateTime.Now.AddMonths(3).ToString() }
    };

            _tournamentService.Setup(r => r.GetAll())
                .ReturnsAsync(tournaments);

            var result = await _controller.UserParticipations(userId, tournamentId, null, "/return") as ViewResult;

            Assert.IsNotNull(result);
            Assert.That(result.Model, Is.InstanceOf(typeof(TournamentViewModel)));

            var model = result.Model as TournamentViewModel;
            Assert.IsNotNull(model);
            Assert.AreEqual(2, model.Tournaments.Count);
            Assert.AreEqual(2, model.FilteredTournaments.Count);
            Assert.IsNull(model.Name);
            Assert.IsNull(model.SportId);
            Assert.IsNull(model.Country);
            Assert.IsNull(model.StartDate);
            Assert.IsNull(model.OrganizerName);
            Assert.IsNull(model.EndDate);
            Assert.IsNull(model.Approved);
        }

        [Test]
        public async Task UserParticipations_WithFilter_ReturnsFilteredResults()
        {
            var userId = "user123";
            var tournamentId = "1";

            var user = new SportConnectUser
            {
                Id = userId,
                UserName = "userA"
            };

            _userService.Setup(r => r.GetById(userId))
                .ReturnsAsync(user);

            var tournament = new Tournament { Id = tournamentId, Name = "Test Tournament" };
            _tournamentService.Setup(r => r.GetById(tournamentId))
                .ReturnsAsync(tournament);

            var tournaments = new List<Tournament>
    {
        new Tournament
        {
            Id = "1",
            Name = "Tennis Open",
            Date = DateTime.Now.AddDays(10).ToString(),
            Country = "USA",
            SportId = "sport1",
            Sport = new Sport { Name = "Tennis" },
            Organizer = new SportConnectUser { UserName = "organizer1", FullName = "Org One" },
            Participations = new List<Participation>
            {
                new Participation { ParticipantId = userId, Approved = true }
            }
        },
        new Tournament
        {
            Id = "2",
            Name = "Football Championship",
            Date = DateTime.Now.AddDays(20).ToString(),
            Country = "Germany",
            SportId = "sport2",
            Sport = new Sport { Name = "Football" },
            Organizer = new SportConnectUser { UserName = "organizer2", FullName = "Org Two" },
            Participations = new List<Participation>
            {
                new Participation { ParticipantId = userId, Approved = false }
            }
        }
    };

            _tournamentService.Setup(r => r.GetAll())
    .ReturnsAsync(tournaments);

            _tournamentService.Setup(r => r.AllWithIncludes(
    It.IsAny<System.Linq.Expressions.Expression<Func<Tournament, object>>>(),
    It.IsAny<System.Linq.Expressions.Expression<Func<Tournament, object>>>(),
    It.IsAny<System.Linq.Expressions.Expression<Func<Tournament, object>>>()))
    .ReturnsAsync(tournaments);

            _sportService.Setup(r => r.GetAll())
    .ReturnsAsync(new List<Sport> { new Sport { Id = "sport1", Name = "Tennis" } });

            var filter = new TournamentViewModel
            {
                Approved = true,
                Country = "USA"
            };

            var result = await _controller.UserParticipations(userId, tournamentId, filter, "/return") as ViewResult;

            Assert.IsNotNull(result);
            var model = result.Model as TournamentViewModel;
            Assert.IsNotNull(model);
            Assert.AreEqual(1, model.FilteredTournaments.Count);
            Assert.AreEqual("Tennis Open", model.FilteredTournaments[0].Name);
            Assert.AreEqual(true, model.Approved);
            Assert.AreEqual("USA", model.Country);
        }
        [Test]
        public async Task AddParticipation_AddsParticipation_AndRedirects()
        {
            var userId = "user123";
            var tournamentId = "tour123"; var returnUrl = "/return";
            var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
        new Claim(ClaimTypes.NameIdentifier, userId),
            }));
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };
            var mockUser = new SportConnectUser { Id = userId };
            _mockUserManager.Setup(u => u.GetUserAsync(user))
                .ReturnsAsync(mockUser);

            _tournamentService.Setup(r => r.GetById(tournamentId))
    .ReturnsAsync(new Tournament { Id = tournamentId });

            _participationService.Setup(r => r.Add(It.IsAny<Participation>()))
                .Returns(Task.CompletedTask);

            var result = await _controller.AddParticipation(tournamentId, returnUrl) as RedirectResult;

            Assert.IsNotNull(result);
            Assert.AreEqual(returnUrl, result.Url);
            _participationService.Verify(r => r.Add(It.Is<Participation>(p =>
                p.ParticipantId == userId &&
                p.TournamentId == tournamentId && p.Approved == false)), Times.Once);
        }
        [Test]
        public async Task DeleteParticipation_RemovesParticipation_AndRedirects()
        {
            var userId = "user123";
            var tournamentId = "tournament123"; var returnUrl = "/return";

            var participation = new Participation
            {
                ParticipantId = userId,
                TournamentId = tournamentId
            };

            var tournament = new Tournament
            {
                Id = tournamentId,
                Participations = new List<Participation> { participation }
            };

            var user = new SportConnectUser { Id = userId };

            _userService.Setup(r => r.GetById(userId))
                .ReturnsAsync(user);

            _participationService.Setup(r => r.GetAll())
                .ReturnsAsync(new List<Participation> { participation });

            _tournamentService.Setup(r => r.GetById(tournamentId))
                .ReturnsAsync(tournament);

            _participationService.Setup(r => r.Delete(It.IsAny<Participation>()))
                .Returns(Task.CompletedTask);

            var result = await _controller.DeleteParticipation(tournamentId, userId, returnUrl) as RedirectResult;

            Assert.IsNotNull(result);
            Assert.AreEqual(returnUrl, result.Url);
            _participationService.Verify(r => r.Delete(It.Is<Participation>(p =>
                p.ParticipantId == userId &&
                p.TournamentId == tournamentId)), Times.Once);
        }
        [Test]
        public async Task ApproveParticipation_UpdatesParticipation_AndRedirects()
        {
            var userId = "user123";
            var tournamentId = "tournament123";
            var returnUrl = "/return";

            var participation = new Participation
            {
                ParticipantId = userId,
                TournamentId = tournamentId,
                Approved = false
            };

            var tournament = new Tournament
            {
                Id = tournamentId
            };

            var user = new SportConnectUser { Id = userId };

            _userService.Setup(r => r.GetById(userId))
                .ReturnsAsync(user);

            _tournamentService.Setup(r => r.GetById(tournamentId))
                .ReturnsAsync(tournament);

            _participationService.Setup(r => r.GetAll())
                .ReturnsAsync(new List<Participation> { participation });

            _participationService.Setup(r => r.Update(It.IsAny<Participation>()))
                .Returns(Task.CompletedTask);

            var result = await _controller.ApproveParticipation(tournamentId, userId, returnUrl) as RedirectResult;

            Assert.IsNotNull(result);
            Assert.AreEqual(returnUrl, result.Url);
            _participationService.Verify(r => r.Update(It.Is<Participation>(p =>
                p.ParticipantId == userId &&
                p.TournamentId == tournamentId &&
                p.Approved == true)), Times.Once);
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
}