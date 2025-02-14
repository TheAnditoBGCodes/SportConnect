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


        public IActionResult TournamentDetails(int id)
        {
            var range = _participationsRepository.AllWithIncludes(x => x.Tournament, x => x.Participant).Where(x => x.TournamentId == id);
            var tournament = _repository.AllWithIncludes(x => x.Organizer, x => x.Sport).FirstOrDefault(x => x.Id == id);
            var model = new TournamentDeletionViewModel()
            {
                Id = tournament.Id,
                OrganizerName = tournament.Organizer.FullName,
                Date = tournament.Date,
                Deadline = tournament.Deadline,
                Description = tournament.Description,
                Location = tournament.Location,
                Name = tournament.Name,
                SportName = tournament.Sport.Name,
                Participations = range
            };
            return View(model);
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
            var tournaments = _repository.AllWithIncludes(x => x.Organizer, x => x.Sport).Where(x => x.SportId == id);
            var sport = _sportRepository.GetById(id);
            var model = new SportDeletionViewModel()
            {
                Name = sport.Name,
                Description = sport.Description,
                Tournaments = tournaments
            };
            return View(model);
        }
        public async Task<IActionResult> AllTournaments(TournamentFilterViewModel? filter)
        {
            if (filter == null)
            {
                // Handle null filter case, return empty model or other fallback
                return View(new TournamentFilterViewModel());
            }

            var query = _repository.GetAll().AsQueryable();

            if (filter.SportId != null)
            {
                query = query.Where(p => p.SportId == filter.SportId.Value);
            }
            if (filter.Date != null)
            {
                query = query.Where(p => p.Date == filter.Date.Value);
            }

            var currentUser = await _userManager.GetUserAsync(this.User);
            if (currentUser == null)
            {
                return Redirect("/Identity/Account/Login");
            }

            var sportsList = _sportRepository.GetAll();
            var defaultSportName = sportsList.Select(x => x.Name).FirstOrDefault() ?? "Default Sport Name";

            var userParticipations = _participationsRepository.GetAllBy(x => x.ParticipantId == currentUser.Id) ?? new List<Participation>();

            var model = new TournamentFilterViewModel
            {
                SportId = filter.SportId,
                Date = filter.Date,
                Sports = new SelectList(sportsList, "Id", "Name", defaultSportName),
                Tournaments = query.Include(x => x.Organizer).Include(x => x.Sport).ToList(),
                UserId = currentUser.Id,
                UserParticipations = userParticipations.ToList()
            };

            return View(model);
        }

        public IActionResult DeleteTournament(int id)
        {
            var range = _participationsRepository.AllWithIncludes(x => x.Tournament, x => x.Participant).Where(x => x.TournamentId == id);
            var tournament = _repository.AllWithIncludes(x => x.Organizer, x => x.Sport).FirstOrDefault(x => x.Id == id);
            var model = new TournamentDeletionViewModel()
            {
                Id = tournament.Id,
                OrganizerName = tournament.Organizer.FullName,
                Date = tournament.Date,
                Deadline = tournament.Deadline,
                Description = tournament.Description,
                Location = tournament.Location,
                Name = tournament.Name,
                SportName = tournament.Sport.Name,
                Participations = range
            };
            return View(model);
        }

        [HttpPost]
        public IActionResult DeleteTournament(int id, TournamentDeletionViewModel model)
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