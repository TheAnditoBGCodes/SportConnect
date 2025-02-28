using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SportConnect.DataAccess.Repository.IRepository;
using SportConnect.Models;
using SportConnect.Utility;
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

        [Authorize(Roles = $"{SD.AdminRole}")]
        public IActionResult AddTournamentAdmin()
        {
            var model = new TournamentViewModel()
            {
                Sports = new SelectList(_sportRepository.GetAll(), "Id", "Name")
            };
            return View(model);
        }

        [Authorize(Roles = $"{SD.AdminRole}")]
        [HttpPost]
        public IActionResult AddTournamentAdmin(TournamentViewModel tournament)
        {
            var user = _userManager.GetUserAsync(this.User).Result;

            if (user == null)
            {
                return RedirectToAction("AllTournamentsAdmin", "Tournament");
            }

            tournament.OrganizerId = user.Id;

            // Combine Date and Time
            if (tournament.Date.HasValue && tournament.DateTimer.HasValue)
            {
                // Combine the Date part with the DateTimer (time) part
                var combinedDate = tournament.Date.Value.Date + tournament.DateTimer.Value.TimeOfDay;
                tournament.Date = combinedDate;
            }

            // Combine Deadline and DeadlineTime
            if (tournament.Deadline.HasValue && tournament.DeadlineTime.HasValue)
            {
                // Combine the Deadline part with the DeadlineTime (time) part
                var combinedDeadline = tournament.Deadline.Value.Date + tournament.DeadlineTime.Value.TimeOfDay;
                tournament.Deadline = combinedDeadline;
            }

            if (!ModelState.IsValid)
            {
                tournament.Sports = new SelectList(_sportRepository.GetAll(), "Id", "Name");
                return View(tournament);
            }

            _repository.Add(tournament.ToTournament());
            return RedirectToAction("AllTournamentsAdmin", "Tournament");
        }

        [Authorize(Roles = $"{SD.AdminRole}")]
        public IActionResult AddTournamentMyAdmin()
        {
            var model = new TournamentViewModel()
            {
                Sports = new SelectList(_sportRepository.GetAll(), "Id", "Name")
            };
            return View(model);
        }

        [Authorize(Roles = $"{SD.AdminRole}")]
        [HttpPost]
        public IActionResult AddTournamentMyAdmin(TournamentViewModel tournament)
        {
            var user = _userManager.GetUserAsync(this.User).Result;

            if (user == null)
            {
                return RedirectToAction("AllTournamentsMyAdmin", "Tournament");
            }

            tournament.OrganizerId = user.Id;

            // Combine Date and Time
            if (tournament.Date.HasValue && tournament.DateTimer.HasValue)
            {
                // Combine the Date part with the DateTimer (time) part
                var combinedDate = tournament.Date.Value.Date + tournament.DateTimer.Value.TimeOfDay;
                tournament.Date = combinedDate;
            }

            // Combine Deadline and DeadlineTime
            if (tournament.Deadline.HasValue && tournament.DeadlineTime.HasValue)
            {
                // Combine the Deadline part with the DeadlineTime (time) part
                var combinedDeadline = tournament.Deadline.Value.Date + tournament.DeadlineTime.Value.TimeOfDay;
                tournament.Deadline = combinedDeadline;
            }

            if (!ModelState.IsValid)
            {
                tournament.Sports = new SelectList(_sportRepository.GetAll(), "Id", "Name");
                return View(tournament);
            }

            _repository.Add(tournament.ToTournament());
            return RedirectToAction("AllTournamentsMyAdmin", "Tournament");
        }

        [Authorize(Roles = $"{SD.AdminRole},{SD.UserRole}")]
        [HttpPost]
        public IActionResult AddTournamentMy(TournamentViewModel tournament)
        {
            var user = _userManager.GetUserAsync(this.User).Result;
            tournament.OrganizerId = user.Id;

            if (!ModelState.IsValid)
            {
                tournament.Sports = new SelectList(_sportRepository.GetAll(), "Id", "Name");
                return View(tournament);
            }

            _repository.Add(tournament.ToTournament());
            return RedirectToAction("AllTournamentsMy", "Tournament");
        }

        [Authorize(Roles = $"{SD.AdminRole}")]
        public IActionResult TournamentDetailsAdmin(int id)
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

        [Authorize(Roles = $"{SD.AdminRole}")]
        public IActionResult TournamentDetailsMyAdmin(int id)
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

        [Authorize(Roles = $"{SD.AdminRole},{SD.UserRole}")]
        public IActionResult TournamentDetailsMy(int id)
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

        [Authorize(Roles = $"{SD.AdminRole}")]
        public IActionResult EditTournamentAdmin(int id)
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
                SportId = tournament.SportId,
                Sports = new SelectList(_sportRepository.GetAll(), "Id", "Name", tournament.SportId)
            };
            return View(model);
        }

        [Authorize(Roles = $"{SD.AdminRole}")]
        [HttpPost]
        public IActionResult EditTournamentAdmin(TournamentViewModel viewModel)
        {
            if (!ModelState.IsValid)
            {
                viewModel.Sports = new SelectList(_sportRepository.GetAll(), "Id", "Name", viewModel.SportId);
                return View(viewModel);
            }

            var tournament = _repository.GetById(viewModel.Id ?? 0);

            // Combine Date and Time for Date property
            if (viewModel.Date.HasValue && viewModel.DateTimer.HasValue)
            {
                tournament.Date = viewModel.Date.Value.Date + viewModel.DateTimer.Value.TimeOfDay;
            }

            // Combine Deadline Date and Time for Deadline property
            if (viewModel.Deadline.HasValue && viewModel.DeadlineTime.HasValue)
            {
                tournament.Deadline = viewModel.Deadline.Value.Date + viewModel.DeadlineTime.Value.TimeOfDay;
            }

            tournament.Name = viewModel.Name;
            tournament.Description = viewModel.Description;
            tournament.Location = viewModel.Location;
            tournament.SportId = viewModel.SportId ?? tournament.SportId;

            _repository.Update(tournament);
            return RedirectToAction("AllTournamentsAdmin", "Tournament");
        }

        [Authorize(Roles = $"{SD.AdminRole}")]
        public IActionResult EditTournamentMyAdmin(int id)
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
                SportId = tournament.SportId,
                Sports = new SelectList(_sportRepository.GetAll(), "Id", "Name", tournament.SportId)
            };
            return View(model);
        }

        [Authorize(Roles = $"{SD.AdminRole}")]
        [HttpPost]
        public IActionResult EditTournamentMyAdmin(TournamentViewModel viewModel)
        {
            if (!ModelState.IsValid)
            {
                viewModel.Sports = new SelectList(_sportRepository.GetAll(), "Id", "Name", viewModel.SportId);
                return View(viewModel);
            }

            var tournament = _repository.GetById(viewModel.Id ?? 0);

            // Combine Date and Time for Date property
            if (viewModel.Date.HasValue && viewModel.DateTimer.HasValue)
            {
                tournament.Date = viewModel.Date.Value.Date + viewModel.DateTimer.Value.TimeOfDay;
            }

            // Combine Deadline Date and Time for Deadline property
            if (viewModel.Deadline.HasValue && viewModel.DeadlineTime.HasValue)
            {
                tournament.Deadline = viewModel.Deadline.Value.Date + viewModel.DeadlineTime.Value.TimeOfDay;
            }

            tournament.Name = viewModel.Name;
            tournament.Description = viewModel.Description;
            tournament.Location = viewModel.Location;
            tournament.SportId = viewModel.SportId ?? tournament.SportId;

            _repository.Update(tournament);
            return RedirectToAction("AllTournamentsMyAdmin", "Tournament");
        }

        [Authorize(Roles = $"{SD.AdminRole},{SD.UserRole}")]
        public IActionResult EditTournamentMy(int id)
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
                Sports = new SelectList(_sportRepository.GetAll(), "Id", "Name", tournament.SportId)
            };
            return View(model);
        }

        [Authorize(Roles = $"{SD.AdminRole},{SD.UserRole}")]
        [HttpPost]
        public IActionResult EditTournamentMy(TournamentViewModel viewModel)
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
            return RedirectToAction("AllTournamentsMy", "Tournament");
        }

        [Authorize(Roles = $"{SD.AdminRole}")]
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

        [Authorize(Roles = $"{SD.AdminRole}")]
        public async Task<IActionResult> AllTournamentsAdmin(TournamentFilterViewModel? filter)
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
                query = query.Where(p => p.Date.Date == filter.Date.Value.Date);
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

        [Authorize(Roles = $"{SD.UserRole}")]
        public async Task<IActionResult> AllTournamentsMy(TournamentFilterViewModel? filter)
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
                query = query.Where(p => p.Date.Date == filter.Date.Value.Date);
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
                Tournaments = query.Include(x => x.Organizer).Include(x => x.Sport).Where(x => x.OrganizerId == currentUser.Id).ToList(),
                UserId = currentUser.Id,
                UserParticipations = userParticipations.ToList()
            };

            return View(model);
        }

        [Authorize(Roles = $"{SD.AdminRole}")]
        public async Task<IActionResult> AllTournamentsMyAdmin(TournamentFilterViewModel? filter)
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
                query = query.Where(p => p.Date.Date == filter.Date.Value.Date);
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
                Tournaments = query.Include(x => x.Organizer).Include(x => x.Sport).Where(x => x.OrganizerId == currentUser.Id).ToList(),
                UserId = currentUser.Id,
                UserParticipations = userParticipations.ToList()
            };

            return View(model);
        }

        [Authorize(Roles = $"{SD.AdminRole}")]
        public IActionResult DeleteTournamentAdmin(int id)
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

        [Authorize(Roles = $"{SD.AdminRole}")]
        [HttpPost]
        public IActionResult DeleteTournamentAdmin(int id, string ConfirmText, TournamentDeletionViewModel model)
        {
            if (ConfirmText == "ÏÎÒÂÚÐÄÈ")
            {
                var range = _participationsRepository.GetAllBy(x => x.TournamentId == id);
                _participationsRepository.DeleteRange(range);
                var entity = _repository.GetById(id);
                _repository.Delete(entity);
                return RedirectToAction("AllTournamentsAdmin");
            }
            return View(model);
        }


        [Authorize(Roles = $"{SD.AdminRole}")]
        public IActionResult DeleteTournamentMyAdmin(int id)
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

        [Authorize(Roles = $"{SD.AdminRole}")]
        [HttpPost]
        public IActionResult DeleteTournamentMyAdmin(int id, string ConfirmText, TournamentDeletionViewModel model)
        {
            if (ConfirmText == "ÏÎÒÂÚÐÄÈ")
            {
                var range = _participationsRepository.GetAllBy(x => x.TournamentId == id);
                _participationsRepository.DeleteRange(range);
                var entity = _repository.GetById(id);
                _repository.Delete(entity);
                return RedirectToAction("AllTournamentsMyAdmin");
            }
            return View(model);
        }

        [Authorize(Roles = $"{SD.AdminRole},{SD.UserRole}")]
        public IActionResult DeleteTournamentMy(int id)
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

        [Authorize(Roles = $"{SD.AdminRole},{SD.UserRole}")]
        [HttpPost]
        public IActionResult DeleteTournamentMy(int id, TournamentDeletionViewModel model)
        {
            var range = _participationsRepository.GetAllBy(x => x.TournamentId == id);
            _participationsRepository.DeleteRange(range);
            var entity = _repository.GetById(id);
            _repository.Delete(entity);
            return RedirectToAction("AllTournamentsMy", "Tournament");
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}