using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SportConnect.DataAccess.Repository.IRepository;
using SportConnect.Models;
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

        public IActionResult AddSport()
        {
            return View();
        }

        [HttpPost]
        public IActionResult AddSport(Sport sport)
        {
            _repository.Add(sport);
            return RedirectToAction("AllSports");
        }

        public IActionResult EditSport(int id)
        {
            var entity = _repository.GetById(id);
            return View(entity);
        }

        [HttpPost]
        public IActionResult EditSport(Sport sport)
        {
            _repository.Update(sport);
            return RedirectToAction("AllSports");
        }

        public IActionResult AllSports()
        {
            return View(_repository.GetAll().ToList());
        }

        public IActionResult DeleteSport(int id)
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

            return RedirectToAction("AllSports");
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
