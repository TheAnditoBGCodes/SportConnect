using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;
using NUnit.Framework;
using System.Security.Claims;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using SportConnect.DataAccess.Repository.IRepository;
using SportConnect.Models;
using SportConnect.Web.Controllers;
using SportConnect.Web.Models;
using Microsoft.AspNetCore.Identity;
using System;
using System.Linq.Expressions;

namespace SportConnect.Tests.Controllers
{
    [TestFixture]
    public class SportControllerTests
    {
        private Mock<IRepository<Sport>> _mockSportRepository;
        private Mock<IRepository<Tournament>> _mockTournamentRepository;
        private Mock<IRepository<Participation>> _mockParticipationRepository;
        private SportController _controller;

        [TearDown]
        public void TearDown()
        {
            _controller.Dispose();
        }

        [SetUp]
        public void Setup()
        {
            _mockSportRepository = new Mock<IRepository<Sport>>();
            _mockTournamentRepository = new Mock<IRepository<Tournament>>();
            _mockParticipationRepository = new Mock<IRepository<Participation>>();

            _controller = new SportController(
                _mockSportRepository.Object,
                _mockTournamentRepository.Object,
                _mockParticipationRepository.Object
            );

            var httpContext = new DefaultHttpContext();
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
            Assert.IsInstanceOf<SportController>(_controller);
            Assert.AreEqual(_mockSportRepository.Object, _controller._sportRepository);
            Assert.AreEqual(_mockTournamentRepository.Object, _controller._tournamentRepository);
            Assert.AreEqual(_mockParticipationRepository.Object, _controller._participationRepository);
        }

        [Test]
        public async Task AllSports_NoFilter_ReturnsDefaultViewModel()
        {
            var sports = new List<Sport>
            {
                new Sport { Id = "1", Name = "Tennis", Description = "Tennis description", ImageUrl = "tennis.jpg" },
                new Sport { Id = "2", Name = "Football", Description = "Football description", ImageUrl = "football.jpg" }
            };

            _mockSportRepository.Setup(r => r.GetAll())
                .ReturnsAsync(sports);

            var result = await _controller.AllSports(null) as ViewResult;

            Assert.IsNotNull(result);
            Assert.That(result.Model, Is.InstanceOf(typeof(SportViewModel)));
            var model = result.Model as SportViewModel;
            Assert.IsNotNull(model);
            Assert.AreEqual(2, model.Sports.Count);
            Assert.AreEqual(2, model.FilteredSports.Count);
            Assert.AreEqual(null, model.Name);
        }

        [Test]
        public async Task AllSports_WithFilter_ReturnsFilteredResults()
        {
            var sports = new List<Sport>
            {
                new Sport { Id = "1", Name = "Tennis", Description = "Tennis description", ImageUrl = "tennis.jpg" },
                new Sport { Id = "2", Name = "Football", Description = "Football description", ImageUrl = "football.jpg" }
            };

            _mockSportRepository.Setup(r => r.GetAll())
                .ReturnsAsync(sports);

            var filter = new SportViewModel
            {
                Name = "tenn"
            };

            var result = await _controller.AllSports(filter) as ViewResult;

            Assert.IsNotNull(result);
            var model = result.Model as SportViewModel;
            Assert.IsNotNull(model);
            Assert.AreEqual(2, model.Sports.Count);
            Assert.AreEqual(1, model.FilteredSports.Count);
            Assert.AreEqual("Tennis", model.FilteredSports[0].Name);
            Assert.AreEqual("tenn", model.Name);
        }

        [Test]
        public async Task AddSport_Get_ReturnsView()
        {
            var result = await _controller.AddSport() as ViewResult;

            Assert.IsNotNull(result);
            Assert.That(result.Model, Is.InstanceOf(typeof(SportViewModel)));
        }

        [Test]
        public async Task AddSport_Post_ValidModel_AddsAndRedirects()
        {
            var sportViewModel = new SportViewModel
            {
                Name = "Basketball",
                Description = "Basketball description",
                ImageUrl = "basketball.jpg"
            };

            var sports = new List<Sport>();
            _mockSportRepository.Setup(r => r.GetAll())
                .ReturnsAsync(sports);

            _mockSportRepository.Setup(r => r.Add(It.IsAny<Sport>()))
                .Returns(Task.CompletedTask);

            var result = await _controller.AddSport(sportViewModel) as RedirectToActionResult;

            Assert.IsNotNull(result);
            Assert.AreEqual("AllSports", result.ActionName);
            _mockSportRepository.Verify(r => r.Add(It.Is<Sport>(s =>
                s.Name == sportViewModel.Name &&
                s.Description == sportViewModel.Description &&
                s.ImageUrl == sportViewModel.ImageUrl)), Times.Once);
        }

        [Test]
        public async Task AddSport_Post_EmptyName_ReturnsViewWithError()
        {
            var sportViewModel = new SportViewModel
            {
                Name = "",
                Description = "Valid description",
                ImageUrl = "image.jpg"
            };

            var result = await _controller.AddSport(sportViewModel) as ViewResult;

            Assert.IsNotNull(result);
            Assert.IsFalse(_controller.ModelState.IsValid);
            Assert.IsTrue(_controller.ModelState["Name"].Errors.Any(e => e.ErrorMessage == "Името е задължително."));
            Assert.IsTrue(_controller.ModelState["ImageUrl"].Errors.Any(e => e.ErrorMessage == "Снимката е задължителна."));
        }

        [Test]
        public async Task AddSport_Post_NameTooShort_ReturnsViewWithError()
        {
            var sportViewModel = new SportViewModel
            {
                Name = "Ten",
                Description = "Valid description",
                ImageUrl = "image.jpg"
            };

            var result = await _controller.AddSport(sportViewModel) as ViewResult;

            Assert.IsNotNull(result);
            Assert.IsFalse(_controller.ModelState.IsValid);
            Assert.IsTrue(_controller.ModelState["Name"].Errors.Any(e => e.ErrorMessage == "Tрябва да е от 5 до 100 символа."));
        }

        [Test]
        public async Task AddSport_Post_EmptyDescription_ReturnsViewWithError()
        {
            var sportViewModel = new SportViewModel
            {
                Name = "Tennis",
                Description = "",
                ImageUrl = "image.jpg"
            };

            var result = await _controller.AddSport(sportViewModel) as ViewResult;

            Assert.IsNotNull(result);
            Assert.IsFalse(_controller.ModelState.IsValid);
            Assert.IsTrue(_controller.ModelState["Description"].Errors.Any(e => e.ErrorMessage == "Описанието е задължително."));
        }

        [Test]
        public async Task AddSport_Post_DescriptionTooShort_ReturnsViewWithError()
        {
            var sportViewModel = new SportViewModel
            {
                Name = "Tennis",
                Description = "Ten",
                ImageUrl = "image.jpg"
            };

            var result = await _controller.AddSport(sportViewModel) as ViewResult;

            Assert.IsNotNull(result);
            Assert.IsFalse(_controller.ModelState.IsValid);
            Assert.IsTrue(_controller.ModelState["Description"].Errors.Any(e => e.ErrorMessage == "Tрябва да е от 5 до 100 символа."));
        }

        [Test]
        public async Task AddSport_Post_DuplicateName_ReturnsViewWithError()
        {
            var existingSports = new List<Sport>
            {
                new Sport { Id = "1", Name = "Tennis", Description = "Different description" }
            };

            _mockSportRepository.Setup(r => r.GetAll())
                .ReturnsAsync(existingSports);

            var sportViewModel = new SportViewModel
            {
                Name = "Tennis",
                Description = "New description",
                ImageUrl = "image.jpg"
            };

            var result = await _controller.AddSport(sportViewModel) as ViewResult;

            Assert.IsNotNull(result);
            Assert.IsFalse(_controller.ModelState.IsValid);
            Assert.IsTrue(_controller.ModelState["Name"].Errors.Any(e => e.ErrorMessage == "Името е използвано от друг спорт."));
        }

        [Test]
        public async Task AddSport_Post_DuplicateDescription_ReturnsViewWithError()
        {
            var existingSports = new List<Sport>
            {
                new Sport { Id = "1", Name = "Different name", Description = "Tennis description" }
            };

            _mockSportRepository.Setup(r => r.GetAll())
                .ReturnsAsync(existingSports);

            var sportViewModel = new SportViewModel
            {
                Name = "Tennis",
                Description = "Tennis description",
                ImageUrl = "image.jpg"
            };

            var result = await _controller.AddSport(sportViewModel) as ViewResult;

            Assert.IsNotNull(result);
            Assert.IsFalse(_controller.ModelState.IsValid);
            Assert.IsTrue(_controller.ModelState["Description"].Errors.Any(e => e.ErrorMessage == "Описанието е използвано от друг спорт."));
        }

        [Test]
        public async Task EditSport_Get_ReturnsView()
        {
            var sport = new Sport
            {
                Id = "1",
                Name = "Tennis",
                Description = "Tennis description",
                ImageUrl = "tennis.jpg"
            };

            _mockSportRepository.Setup(r => r.GetById("1"))
                .ReturnsAsync(sport);

            var result = await _controller.EditSport("1") as ViewResult;

            Assert.IsNotNull(result);
            var model = result.Model as SportViewModel;
            Assert.IsNotNull(model);
            Assert.AreEqual("1", model.Id);
            Assert.AreEqual("Tennis", model.Name);
            Assert.AreEqual("Tennis description", model.Description);
            Assert.AreEqual("tennis.jpg", model.ImageUrl);
        }

        [Test]
        public async Task EditSport_Post_ValidModel_UpdatesAndRedirects()
        {
            var existingSport = new Sport
            {
                Id = "1",
                Name = "Tennis",
                Description = "Tennis description",
                ImageUrl = "tennis.jpg"
            };

            var existingSports = new List<Sport> { existingSport };

            _mockSportRepository.Setup(r => r.GetAll())
                .ReturnsAsync(existingSports);

            _mockSportRepository.Setup(r => r.GetById("1"))
                .ReturnsAsync(existingSport);

            _mockSportRepository.Setup(r => r.Update(It.IsAny<Sport>()))
                .Returns(Task.CompletedTask);

            var updatedSport = new SportViewModel
            {
                Id = "1",
                Name = "Updated Tennis",
                Description = "Updated description",
                ImageUrl = "updated.jpg"
            };

            var result = await _controller.EditSport(updatedSport, null) as RedirectToActionResult;

            Assert.IsNotNull(result);
            Assert.AreEqual("AllSports", result.ActionName);
            _mockSportRepository.Verify(r => r.Update(It.Is<Sport>(s =>
                s.Id == "1" &&
                s.Name == "Updated Tennis" &&
                s.Description == "Updated description" &&
                s.ImageUrl == "updated.jpg")), Times.Once);
        }

        [Test]
        public async Task EditSport_Post_EmptyName_ReturnsViewWithError()
        {
            var sportViewModel = new SportViewModel
            {
                Id = "1",
                Name = "",
                Description = "Valid description",
                ImageUrl = "image.jpg"
            };

            var result = await _controller.EditSport(sportViewModel, null) as ViewResult;

            Assert.IsNotNull(result);
            Assert.IsFalse(_controller.ModelState.IsValid);
            Assert.IsTrue(_controller.ModelState["Name"].Errors.Any(e => e.ErrorMessage == "Името е задължително."));
        }

        [Test]
        public async Task EditSport_Post_NameTooShort_ReturnsViewWithError()
        {
            var sportViewModel = new SportViewModel
            {
                Id = "1",
                Name = "Ten",
                Description = "Valid description",
                ImageUrl = "image.jpg"
            };

            var result = await _controller.EditSport(sportViewModel, null) as ViewResult;

            Assert.IsNotNull(result);
            Assert.IsFalse(_controller.ModelState.IsValid);
            Assert.IsTrue(_controller.ModelState["Name"].Errors.Any(e => e.ErrorMessage == "Tрябва да е от 5 до 100 символа."));
        }

        [Test]
        public async Task EditSport_Post_EmptyDescription_ReturnsViewWithError()
        {
            var sportViewModel = new SportViewModel
            {
                Id = "1",
                Name = "Tennis",
                Description = "",
                ImageUrl = "image.jpg"
            };

            var result = await _controller.EditSport(sportViewModel, null) as ViewResult;

            Assert.IsNotNull(result);
            Assert.IsFalse(_controller.ModelState.IsValid);
            Assert.IsTrue(_controller.ModelState["Description"].Errors.Any(e => e.ErrorMessage == "Описанието е задължително."));
        }

        [Test]
        public async Task EditSport_Post_DescriptionTooShort_ReturnsViewWithError()
        {
            var sportViewModel = new SportViewModel
            {
                Id = "1",
                Name = "Tennis",
                Description = "Ten",
                ImageUrl = "image.jpg"
            };

            var result = await _controller.EditSport(sportViewModel, null) as ViewResult;

            Assert.IsNotNull(result);
            Assert.IsFalse(_controller.ModelState.IsValid);
            Assert.IsTrue(_controller.ModelState["Description"].Errors.Any(e => e.ErrorMessage == "Tрябва да е от 5 до 100 символа."));
        }

        [Test]
        public async Task EditSport_Post_DuplicateName_ReturnsViewWithError()
        {
            var existingSports = new List<Sport>
            {
                new Sport { Id = "1", Name = "Tennis", Description = "Tennis description" },
                new Sport { Id = "2", Name = "Football", Description = "Football description" }
            };

            _mockSportRepository.Setup(r => r.GetAll())
                .ReturnsAsync(existingSports);

            var sportViewModel = new SportViewModel
            {
                Id = "1",
                Name = "Football",
                Description = "Tennis description",
                ImageUrl = "image.jpg"
            };

            var result = await _controller.EditSport(sportViewModel, null) as ViewResult;

            Assert.IsNotNull(result);
            Assert.IsFalse(_controller.ModelState.IsValid);
            Assert.IsTrue(_controller.ModelState["Name"].Errors.Any(e => e.ErrorMessage == "Името е използвано от друг спорт."));
        }

        [Test]
        public async Task EditSport_Post_DuplicateDescription_ReturnsViewWithError()
        {
            var existingSports = new List<Sport>
            {
                new Sport { Id = "1", Name = "Tennis", Description = "Tennis description" },
                new Sport { Id = "2", Name = "Football", Description = "Football description" }
            };

            _mockSportRepository.Setup(r => r.GetAll())
                .ReturnsAsync(existingSports);

            var sportViewModel = new SportViewModel
            {
                Id = "1",
                Name = "Tennis",
                Description = "Football description",
                ImageUrl = "image.jpg"
            };

            var result = await _controller.EditSport(sportViewModel, null) as ViewResult;

            Assert.IsNotNull(result);
            Assert.IsFalse(_controller.ModelState.IsValid);
            Assert.IsTrue(_controller.ModelState["Description"].Errors.Any(e => e.ErrorMessage == "Описанието е използвано от друг спорт."));
        }

        [Test]
        public async Task DeleteSport_Get_ReturnsView()
        {
            var sport = new Sport
            {
                Id = "1",
                Name = "Tennis",
                Description = "Tennis description",
                ImageUrl = "tennis.jpg"
            };

            _mockSportRepository.Setup(r => r.GetById("1"))
                .ReturnsAsync(sport);

            var result = await _controller.DeleteSport("1") as ViewResult;

            Assert.IsNotNull(result);
            var model = result.Model as SportViewModel;
            Assert.IsNotNull(model);
            Assert.AreEqual("Tennis", model.Name);
            Assert.AreEqual("Tennis description", model.Description);
            Assert.AreEqual("tennis.jpg", model.ImageUrl);
        }

        [Test]
        public async Task DeleteSport_Post_ValidConfirmation_DeletesAndRedirects()
        {
            var sportId = "1";
            var sport = new Sport
            {
                Id = sportId,
                Name = "Tennis",
                Description = "Tennis description"
            };

            var tournaments = new List<Tournament>
            {
                new Tournament { Id = "t1", SportId = sportId },
                new Tournament { Id = "t2", SportId = sportId }
            };

            var participations = new List<Participation>
            {
                new Participation { TournamentId = "t1", ParticipantId = "user1" },
                new Participation { TournamentId = "t1", ParticipantId = "user2" },
                new Participation { TournamentId = "t2", ParticipantId = "user3" }
            };

            _mockSportRepository.Setup(r => r.GetById(sportId))
                .ReturnsAsync(sport);

            _mockTournamentRepository.Setup(r => r.GetAllBy(It.IsAny<Expression<Func<Tournament, bool>>>()))
                .ReturnsAsync(tournaments);

            _mockParticipationRepository.Setup(r => r.GetAllBy(It.IsAny<Expression<Func<Participation, bool>>>()))
                .ReturnsAsync(participations.Where(p => p.TournamentId == "t1").ToList());

            _mockParticipationRepository.Setup(r => r.DeleteRange(It.IsAny<IEnumerable<Participation>>()))
                .Returns(Task.CompletedTask);

            _mockTournamentRepository.Setup(r => r.DeleteRange(It.IsAny<IEnumerable<Tournament>>()))
                .Returns(Task.CompletedTask);

            _mockSportRepository.Setup(r => r.Delete(It.IsAny<Sport>()))
                .Returns(Task.CompletedTask);

            var result = await _controller.DeleteSport(sportId, "ПОТВЪРДИ", new SportViewModel()) as RedirectToActionResult;

            Assert.IsNotNull(result);
            Assert.AreEqual("AllSports", result.ActionName);

            _mockParticipationRepository.Verify(r => r.DeleteRange(It.IsAny<IEnumerable<Participation>>()), Times.Exactly(2));
            _mockTournamentRepository.Verify(r => r.DeleteRange(It.IsAny<IEnumerable<Tournament>>()), Times.Once);
            _mockSportRepository.Verify(r => r.Delete(It.Is<Sport>(s => s.Id == sportId)), Times.Once);
        }

        [Test]
        public async Task DeleteSport_Post_InvalidConfirmation_ReturnsView()
        {
            var sportId = "1";
            var sport = new Sport
            {
                Id = sportId,
                Name = "Tennis",
                Description = "Tennis description",
                ImageUrl = "tennis.jpg"
            };

            _mockSportRepository.Setup(r => r.GetById(sportId))
                .ReturnsAsync(sport);

            var result = await _controller.DeleteSport(sportId, "wrong", new SportViewModel()) as ViewResult;

            Assert.IsNotNull(result);
            var model = result.Model as SportViewModel;
            Assert.IsNotNull(model);
            Assert.AreEqual("Tennis", model.Name);
            Assert.AreEqual("Tennis description", model.Description);
            Assert.AreEqual("tennis.jpg", model.ImageUrl);

            _mockSportRepository.Verify(r => r.Delete(It.IsAny<Sport>()), Times.Never);
        }
    }
}