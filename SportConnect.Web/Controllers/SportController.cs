using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SportConnect.DataAccess.Repository.IRepository;
using SportConnect.Models;
using SportConnect.Utility;
using SportConnect.Web.Models;
using System.Diagnostics;
using System.Linq;

namespace SportConnect.Web.Controllers
{
    public class SportController : Controller
    {
        private readonly ILogger<SportController> _logger;
        public IRepository<Sport> _repository { get; set; }
        public IRepository<Tournament> _tournamentRepository { get; set; }
        public IRepository<Participation> _participationsRepository { get; set; }

        public SportController(ILogger<SportController> logger, IRepository<Sport> repository, IRepository<Tournament> tournamentRepository, IRepository<Participation> participationsRepository)
        {
            _logger = logger;
            _repository = repository;
            _tournamentRepository = tournamentRepository;
            _participationsRepository = participationsRepository;
        }

        [Authorize(Roles = $"{SD.AdminRole}")]
        public IActionResult AddSport()
        {
            return View();
        }

        [Authorize(Roles = $"{SD.AdminRole}")]
        [HttpPost]
        public IActionResult AddSport(Sport sport)
        {
            if (_repository.GetAll().Any(s => s.Name == sport.Name))
            {
                ModelState.AddModelError("Name", "Има такъв спорт.");
            }
            if (_repository.GetAll().Any(s => s.Description == sport.Description))
            {
                ModelState.AddModelError("Description", "Това описание е използвано за друг спорт.");
            }

            if (ModelState.IsValid)
            {
                _repository.Add(sport);
                return RedirectToAction("AllSportsAdmin");
            }

            return View(sport);
        }

        [Authorize(Roles = $"{SD.AdminRole}")]
        public IActionResult EditSport(int id)
        {
            var entity = _repository.GetById(id);
            return View(entity);
        }

        [Authorize(Roles = $"{SD.AdminRole}")]
        [HttpPost]
        public IActionResult EditSport(Sport sport)
        {
            if (!_repository.IsPropertyUnique(s => s.Name == sport.Name && s.Id != sport.Id))
            {
                ModelState.AddModelError("Name", "Има такъв спорт.");
            }

            if (!_repository.IsPropertyUnique(s => s.Description == sport.Description && s.Id != sport.Id))
            {
                ModelState.AddModelError("Description", "Това описание е използвано за друг спорт.");
            }

            if (ModelState.IsValid)
            {
                _repository.Update(sport);
                return RedirectToAction("AllSportsAdmin");
            }

            return View(sport);
        }

        [Authorize(Roles = $"{SD.AdminRole}")]
        public IActionResult AllSportsAdmin()
        {
            return View(_repository.GetAll().ToList());
        }

        [Authorize(Roles = $"{SD.UserRole}")]
        public IActionResult AllSportsUser()
        {
            return View(_repository.GetAll().ToList());
        }

        [Authorize(Roles = $"{SD.AdminRole}")]
        public IActionResult DeleteSport(int id)
        {
            var sport = _repository.GetById(id);
            var tours = _tournamentRepository.AllWithIncludes(x => x.Organizer).Where(x => x.SportId == id);
            var model = new SportDeletionViewModel()
            {
                Name = sport.Name,
                Description = sport.Description,
                Tournaments = tours
            };
            return View(model);
        }

        [HttpPost]
        [Authorize(Roles = $"{SD.AdminRole}")]
        public IActionResult DeleteSport(int id, SportDeletionViewModel? model)
        {
            var tournaments = _tournamentRepository.GetAllBy(t => t.SportId == id).ToList();

            foreach (var tournament in tournaments)
            {
                var participations = _participationsRepository.GetAllBy(p => p.TournamentId == tournament.Id).ToList();
                _participationsRepository.DeleteRange(participations);
            }

            _tournamentRepository.DeleteRange(tournaments);

            var sport = _repository.GetById(id);
            if (sport != null)
            {
                _repository.Delete(sport);
            }
            
            return RedirectToAction("AllSportsAdmin");
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}