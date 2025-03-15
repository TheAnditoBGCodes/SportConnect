using CloudinaryDotNet;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SportConnect.DataAccess.Repository.IRepository;
using SportConnect.Models;
using SportConnect.Services;
using SportConnect.Utility;
using SportConnect.Web.Models;
using System.Composition;
using System.Diagnostics;
using System.Security.Claims;
using System.Text.Json;
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
        private readonly HttpClient _httpClient;
        private readonly Cloudinary _cloudinary;
        private readonly CloudinaryService _cloudinaryService;

        public TournamentController(ILogger<TournamentController> logger, UserManager<SportConnectUser> userManager, IRepository<Tournament> repository, IRepository<Sport> sportRepository, IRepository<Participation> participationsRepository, HttpClient httpClient, Cloudinary cloudinary, CloudinaryService cloudinaryService)
        {
            _logger = logger;
            _userManager = userManager;
            _repository = repository;
            _sportRepository = sportRepository;
            _participationsRepository = participationsRepository;
            _httpClient = httpClient;
            _cloudinary = cloudinary;
            _cloudinaryService = cloudinaryService;
        }

        private async Task<List<SelectListItem>> GetAllCountries()
        {
            var response = await _httpClient.GetStringAsync("https://restcountries.com/v3.1/all");
            var countries = System.Text.Json.JsonSerializer.Deserialize<List<CountryResponse>>(response, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            return countries?
                .OrderBy(c => c.Name.Common)
                .Select(c => new SelectListItem
                {
                    Value = c.Name.Common,
                    Text = c.Name.Common
                }).ToList() ?? new List<SelectListItem>();
        }

        [AllowAnonymous]
        public async Task<IActionResult> AllTournaments(TournamentFilterViewModel? filter)
        {
            if (filter == null)
            {
                return View(new TournamentFilterViewModel());
            }

            var query = _repository.GetAll().AsQueryable();

            if (filter.SportId != null)
            {
                query = query.Where(p => p.SportId == filter.SportId.Value);
            }

            ViewBag.Sports = new SelectList(_sportRepository.GetAll(), "Id", "Name");

            var model = new TournamentFilterViewModel
            {
                SportId = filter.SportId,
                FilteredTournaments = query.Include(x => x.Organizer).Include(x => x.Sport).ToList(),
                Tournaments = _repository.GetAll().ToList(),
            };

            return View(model);
        }

        [Authorize(Roles = $"{SD.AdminRole},{SD.UserRole}")]
        public async Task<IActionResult> AddTournament(string returnUrl = null)
        {
            var model = new TournamentViewModel()
            {
                CountryList = await GetAllCountries(),
                Sports = new SelectList(_sportRepository.GetAll(), "Id", "Name")
            };
            ViewBag.ReturnUrl = returnUrl ?? Url.Action("AllTournaments", "Tournament");
            return View(model);
        }

        [Authorize(Roles = $"{SD.AdminRole},{SD.UserRole}")]
        [HttpPost]
        public async Task<IActionResult> AddTournament(TournamentViewModel tournament, string returnUrl = null)
        {
            var user = _userManager.GetUserAsync(this.User).Result;
            tournament.OrganizerId = user.Id;

            if (string.IsNullOrWhiteSpace(tournament.Name) || tournament.Name.Length < 5 || tournament.Name.Length > 100)
            {
                ModelState.AddModelError("Name", "Името трябва да е между 5 и 100 символа");
            }

            if (string.IsNullOrWhiteSpace(tournament.Description) || tournament.Description.Length < 5 || tournament.Description.Length > 100)
            {
                ModelState.AddModelError("Description", "Описанието трябва да е между 5 и 100 символа");
            }

            if (_repository.GetAll().Any(s => s.Name == tournament.Name))
            {
                ModelState.AddModelError("Name", "Името е вече заето.");
            }

            if (_repository.GetAll().Any(s => s.Description == tournament.Description))
            {
                ModelState.AddModelError("Description", "Описанието е вече заето.");
            }
            if (tournament.SportId == null)
            {
                ModelState.AddModelError("SportId", "Спортът е задължителен");
            }

            if (tournament.ImageUrl == null)
            {
                ModelState.AddModelError("ImageUrl", "Задължителна");
            }


            if (tournament.Country == null)
            {
                ModelState.AddModelError("Country", "Задължителна");
            }

            if (!tournament.Date.HasValue)
            {
                ModelState.AddModelError("Date", "Датата е задължителна");
            }

            if (!tournament.Deadline.HasValue)
            {
                ModelState.AddModelError("Deadline", "Задължителен");
            }

            if (!tournament.DateTimer.HasValue)
            {
                ModelState.AddModelError("DateTimer", "Часът е задължителен");
            }

            if (!tournament.DeadlineTime.HasValue)
            {
                ModelState.AddModelError("DeadlineTime", "Часът е задължителен");
            }

            if (tournament.Date.HasValue && tournament.DateTimer.HasValue)
            {
                var eventDateTime = tournament.Date.Value.Date + tournament.DateTimer.Value.TimeOfDay;
                tournament.Date = eventDateTime;
            }

            if (tournament.Deadline.HasValue && tournament.DeadlineTime.HasValue)
            {
                var deadlineDateTime = tournament.Deadline.Value.Date + tournament.DeadlineTime.Value.TimeOfDay;
                tournament.Deadline = deadlineDateTime;
            }

            if (tournament.Deadline.HasValue && tournament.Date.HasValue && tournament.Deadline > tournament.Date)
            {
                ModelState.AddModelError("DateOrder", "Турнира трябва да почва след крайния срок.");
            }

            if (ModelState.IsValid)
            {
                _repository.Add(tournament.GetTournament());
                return Redirect(returnUrl);
            }

            ViewBag.ReturnUrl = returnUrl ?? Url.Action("AllTournaments", "Tournament");
            tournament.Sports = new SelectList(_sportRepository.GetAll(), "Id", "Name");
            tournament.CountryList = await GetAllCountries();
            return View(tournament);
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
                Location = tournament.Country,
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
                Location = tournament.Country,
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
                Location = tournament.Country,
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

            if (tournament == null)
            {
                return NotFound();
            }

            var model = new TournamentViewModel()
            {
                Id = tournament.Id,
                OrganizerId = tournament.OrganizerId,
                Date = tournament.Date.Date, // Extract only the date part
                DateTimer = tournament.Date, // Full DateTime, used for time extraction
                Deadline = tournament.Deadline.Date, // Extract only the date part
                DeadlineTime = tournament.Deadline, // Full DateTime, used for time extraction
                Description = tournament.Description,
                Location = tournament.Country,
                Name = tournament.Name,
                SportId = tournament.SportId,
                Sports = new SelectList(_sportRepository.GetAll(), "Id", "Name", tournament.SportId)
            };

            return View(model);
        }

        [HttpPost]
        [Authorize(Roles = $"{SD.AdminRole}")]
        public IActionResult EditTournamentAdmin(TournamentViewModel model)
        {
            var user = _userManager.GetUserAsync(this.User).Result;

            if (user == null)
            {
                return RedirectToAction("AllTournamentsAdmin", "Tournament");
            }

            model.OrganizerId = user.Id;

            // Combine Date and Time
            if (model.Date.HasValue && model.DateTimer.HasValue)
            {
                var combinedDate = model.Date.Value.Date + model.DateTimer.Value.TimeOfDay;
                model.Date = combinedDate;
            }

            // Combine Deadline and DeadlineTime
            if (model.Deadline.HasValue && model.DeadlineTime.HasValue)
            {
                var combinedDeadline = model.Deadline.Value.Date + model.DeadlineTime.Value.TimeOfDay;
                model.Deadline = combinedDeadline;
            }

            // Check if Deadline is before Date
            if (model.Deadline.HasValue && model.Date.HasValue && model.Deadline > model.Date)
            {
                ModelState.AddModelError("DateOrder", "Êðàéíèÿò ñðîê íà òóðíèðà íå ìîæå äà áúäå ñëåä äàòàòà íà ïðîâåæäàíå.");
            }

            if (!_repository.IsPropertyUnique(s => s.Name == model.Name && s.Id != model.Id))
            {
                ModelState.AddModelError("Name", "Èìà òàêúâ ñïîðò.");
            }
            if (!_repository.IsPropertyUnique(s => s.Description == model.Description && s.Id != model.Id))
            {
                ModelState.AddModelError("Description", "Òîâà îïèñàíèå å èçïîëçâàíî çà äðóã ñïîðò.");
            }

            if (!ModelState.IsValid)
            {
                model.Sports = new SelectList(_sportRepository.GetAll(), "Id", "Name");
                return View(model);
            }

            return RedirectToAction("AllTournamentsAdmin", "Tournament");
        }

        [Authorize(Roles = $"{SD.AdminRole}")]
        public IActionResult EditTournamentMyAdmin(int id)
        {
            var tournament = _repository.GetById(id);

            if (tournament == null)
            {
                return NotFound();
            }

            var model = new TournamentViewModel()
            {
                Id = tournament.Id,
                OrganizerId = tournament.OrganizerId,
                Date = tournament.Date.Date, // Extract only the date part
                DateTimer = tournament.Date, // Full DateTime, used for time extraction
                Deadline = tournament.Deadline.Date, // Extract only the date part
                DeadlineTime = tournament.Deadline, // Full DateTime, used for time extraction
                Description = tournament.Description,
                Location = tournament.Country,
                Name = tournament.Name,
                SportId = tournament.SportId,
                Sports = new SelectList(_sportRepository.GetAll(), "Id", "Name", tournament.SportId)
            };

            return View(model);
        }

        [HttpPost]
        [Authorize(Roles = $"{SD.AdminRole}")]
        public IActionResult EditTournamentMyAdmin(TournamentViewModel model)
        {
            var user = _userManager.GetUserAsync(this.User).Result;

            if (user == null)
            {
                return RedirectToAction("AllTournamentsMyAdmin", "Tournament");
            }

            model.OrganizerId = user.Id;

            // Combine Date and Time
            if (model.Date.HasValue && model.DateTimer.HasValue)
            {
                var combinedDate = model.Date.Value.Date + model.DateTimer.Value.TimeOfDay;
                model.Date = combinedDate;
            }

            // Combine Deadline and DeadlineTime
            if (model.Deadline.HasValue && model.DeadlineTime.HasValue)
            {
                var combinedDeadline = model.Deadline.Value.Date + model.DeadlineTime.Value.TimeOfDay;
                model.Deadline = combinedDeadline;
            }

            // Check if Deadline is before Date
            if (model.Deadline.HasValue && model.Date.HasValue && model.Deadline > model.Date)
            {
                ModelState.AddModelError("DateOrder", "Êðàéíèÿò ñðîê íà òóðíèðà íå ìîæå äà áúäå ñëåä äàòàòà íà ïðîâåæäàíå.");
            }

            if (!_repository.IsPropertyUnique(s => s.Name == model.Name && s.Id != model.Id))
            {
                ModelState.AddModelError("Name", "Èìà òàêúâ ñïîðò.");
            }
            if (!_repository.IsPropertyUnique(s => s.Description == model.Description && s.Id != model.Id))
            {
                ModelState.AddModelError("Description", "Òîâà îïèñàíèå å èçïîëçâàíî çà äðóã ñïîðò.");
            }

            if (!ModelState.IsValid)
            {
                model.Sports = new SelectList(_sportRepository.GetAll(), "Id", "Name");
                return View(model);
            }

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
                Location = tournament.Country,
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
            tournament.Country = viewModel.Location;
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
                Location = tournament.Country,
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
                Location = tournament.Country,
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
                Location = tournament.Country,
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