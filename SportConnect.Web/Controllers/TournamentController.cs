using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SportConnect.DataAccess.Repository.IRepository;
using SportConnect.Models;
using SportConnect.Web.Models;
using System.Composition;
using System.Diagnostics;
using System.Security.Claims;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace SportConnect.Web.Controllers
{
    public class TournamentController : Controller
    {
        private readonly ILogger<TournamentController> _logger;
        private readonly UserManager<SportConnectUser> _userManager;
        public IRepository<Tournament> _repository { get; set; }
        public IRepository<Sport> _sportRepository { get; set; }
        public IRepository<Participation> _participationsRepository { get; set; }

        public TournamentController(ILogger<TournamentController> logger, UserManager<SportConnectUser> userManager, IRepository<Tournament> repository, IRepository<Sport> sportRepository, IRepository<Participation> participationsRepository)
        {
            _logger = logger;
            _userManager = userManager;
            _repository = repository;
            _sportRepository = sportRepository;
            _participationsRepository = participationsRepository;
        }

        public IActionResult AddTournament()
        {
            var model = new TournamentViewModel()
            {
                Sports = new SelectList(_sportRepository.GetAll(), "Id", "Name")
            };
            return View(model);
        }

        [HttpPost]
        public IActionResult AddTournament(TournamentViewModel tournament)
        {
            var user = _userManager.GetUserAsync(this.User).Result;

            if (user == null)
            {
                return RedirectToAction("AllTournaments", "Tournament");
            }

            tournament.OrganizerId = user.Id;

            if (!ModelState.IsValid)
            {
                tournament.Sports = new SelectList(_sportRepository.GetAll(), "Id", "Name");
                return View(tournament);
            }

            _repository.Add(tournament.ToTournament());
            return RedirectToAction("AllTournaments", "Tournament");
        }


        public IActionResult EditTournament(int id)
        {
            var tournament = _repository.GetById(id);
            var model = new TournamentViewModel()
            {
                Id = tournament.Id,
                OrganizerId = tournament.OrganizerId,
                Date = tournament.Date,
                Deadline = tournament.Deadline,
                Description = tournament.Description,
                Location = tournament.Location,
                Name = tournament.Name,
                Sports = new SelectList(_sportRepository.GetAll(), "Id", "Name")
            };
            return View(model);
        }

        [HttpPost]
        public IActionResult EditTournament(TournamentViewModel viewModel)
        {
            if (!ModelState.IsValid)
            {
                viewModel.Sports = new SelectList(_sportRepository.GetAll(), "Id", "Name", viewModel.SportId);
                return View(viewModel);
            }

            var tournament = _repository.GetById(viewModel.Id ?? 0);
            tournament.Name = viewModel.Name;
            tournament.Description = viewModel.Description;
            tournament.Date = viewModel.Date ?? tournament.Date;
            tournament.Deadline = viewModel.Deadline ?? tournament.Deadline;
            tournament.Location = viewModel.Location;
            tournament.SportId = viewModel.SportId ?? tournament.SportId;

            _repository.Update(tournament);
            return RedirectToAction("AllTournaments", "Tournament");
        }

        public IActionResult SportTournaments(int id)
        {
            var model = _repository.AllWithIncludes(x => x.Organizer, x => x.Sport)
                .Select(x => new TournamentViewModel
                {
                    Id = x.Id,
                    Name = x.Name,
                    Description = x.Description,
                    Date = x.Date,
                    Deadline = x.Deadline,
                    OrganizerName = x.Organizer.FullName,
                    Location = x.Location,
                    SportName = x.Sport.Name,
                    SportId = x.SportId
                });
            var tournaments = model.Where(x => x.SportId == id);
            return View(tournaments.ToList());
        }

        public IActionResult AllTournaments(TournamentFilterViewModel filter)
        {
            var query = _repository.GetAll().AsQueryable();

            if (filter.Date != null)
            {
                query = query.Where(x => x.Date.Date == filter.Date.Value.Date);
            }
            if (filter.SportId != null)
            {
                query = query.Where(x => x.SportId == filter.SportId.Value);
            }

            var model = new TournamentFilterViewModel
            {
                Date = filter.Date,
                SportId = filter.SportId,
                Sports = new SelectList(_sportRepository.GetAll(), "Id", "Name"),
                Tournaments = query.Include(x => x.Organizer).Include(x => x.Sport).ToList()
            };

            return View(model);
        }

        public IActionResult DeleteTournament(int id)
        {
            var range = _participationsRepository.GetAllBy(x => x.TournamentId == id);
            _participationsRepository.DeleteRange(range);
            var entity = _repository.GetById(id);
            _repository.Delete(entity);
            return RedirectToAction("AllTournaments");
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}