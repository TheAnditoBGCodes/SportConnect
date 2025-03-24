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
        public async Task<IActionResult> AllTournaments(TournamentViewModel? filter, string returnUrl = null)
        {
            if (filter == null)
            {
                return View(new TournamentViewModel());
            }

            var query = (await _repository.GetAll()).AsQueryable();

            if (filter.SportId != null)
            {
                query = query.Where(p => p.SportId == filter.SportId.Value);
            }

            if (filter.Country != null)
            {
                query = query.Where(p => p.Country == filter.Country);
            }

            if (!string.IsNullOrWhiteSpace(filter.Name))
            {
                string filteredname = filter.Name.Trim().ToLower();
                query = query.Where(p => p.Name.ToLower().Contains(filteredname));
            }

            if (!string.IsNullOrWhiteSpace(filter.OrganizerName))
            {
                string filteredname = filter.OrganizerName.Trim().ToLower();
                query = query.Where(p => p.Organizer.FullName.ToLower().Contains(filteredname));
            }

            if (filter.StartDate != null)
            {
                query = query.Where(p => p.Date >= filter.StartDate);
            }

            if (filter.EndDate != null)
            {
                query = query.Where(p => p.Date <= filter.EndDate);
            }

            ViewBag.Sports = new SelectList(await _sportRepository.GetAll(), "Id", "Name");
            ViewBag.Countries = await GetAllCountries();

            var model = new TournamentViewModel
            {
                SportId = filter.SportId,
                Country = filter.Country,
                Name = filter.Name,
                OrganizerName = filter.OrganizerName,
                StartDate = filter.StartDate,
                EndDate = filter.EndDate,
                FilteredTournaments = query.Include(x => x.Organizer).Include(x => x.Sport).ToList(),
                Tournaments = (await _repository.GetAll()).ToList(),
            };

            ViewBag.ReturnUrl = returnUrl ?? Url.Action("AllTournaments", "Tournament");
            return View(model);
        }

        [Authorize(Roles = $"{SD.AdminRole},{SD.UserRole}")]
        public async Task<IActionResult> AddTournament(string returnUrl = null)
        {
            var currentUser = await _userManager.GetUserAsync(this.User);
            var model = new TournamentViewModel()
            {
                OrganizerId = currentUser.Id,
                CountryList = await GetAllCountries(),
                Sports = new SelectList(await _sportRepository.GetAll(), "Id", "Name")
            };
            ViewBag.ReturnUrl = returnUrl ?? Url.Action("AllTournaments", "Tournament");
            return View(model);
        }

        [Authorize(Roles = $"{SD.AdminRole},{SD.UserRole}")]
        [HttpPost]
        public async Task<IActionResult> AddTournament(TournamentViewModel tournament, string returnUrl = null)
        {
            if (string.IsNullOrWhiteSpace(tournament.Name) || tournament.Name.Length < 5 || tournament.Name.Length > 100)
            {
                ModelState.AddModelError("Name", "Името трябва да е между 5 и 100 символа");
            }

            if (string.IsNullOrWhiteSpace(tournament.Description) || tournament.Description.Length < 5 || tournament.Description.Length > 100)
            {
                ModelState.AddModelError("Description", "Описанието трябва да е между 5 и 100 символа");
            }

            if ((await _repository.GetAll()).Any(s => s.Name == tournament.Name))
            {
                ModelState.AddModelError("Name", "Името е вече заето.");
            }

            if ((await _repository.GetAll()).Any(s => s.Description == tournament.Description))
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
                ModelState.AddModelError("Deadline", "Краен срок е задължителен");
            }

            if (!tournament.DateTimer.HasValue)
            {
                ModelState.AddModelError("DateTimer", "Задължителен");
            }

            if (!tournament.DeadlineTime.HasValue)
            {
                ModelState.AddModelError("DeadlineTime", "Задължителен");
            }

            DateTime eventDateTime = tournament.Deadline.Value.Date.Add(tournament.DeadlineTime.Value);
            DateTime deadlineDateTime = tournament.Date.Value.Date.Add(tournament.DateTimer.Value);

            // Combine the Date and Time for Deadline and Event Date
            if (tournament.Deadline.HasValue && tournament.DeadlineTime.HasValue)
            {
                deadlineDateTime = tournament.Deadline.Value.Date.Add(tournament.DeadlineTime.Value);

                var deadlineDateTimeTruncated = deadlineDateTime.Date.AddHours(deadlineDateTime.TimeOfDay.Hours).AddMinutes(deadlineDateTime.TimeOfDay.Minutes);
                var nowTruncated = DateTime.Now.Date.AddHours(DateTime.Now.TimeOfDay.Hours).AddMinutes(DateTime.Now.TimeOfDay.Minutes);
                if (deadlineDateTimeTruncated < nowTruncated)
                {
                    ModelState.AddModelError("DeadlineTime", "Не може да е в миналото.");
                }
            }

            if (tournament.Date.HasValue && tournament.DateTimer.HasValue)
            {
                eventDateTime = tournament.Date.Value.Date.Add(tournament.DateTimer.Value);

                // Check if the event start date-time is before the deadline
                if (tournament.Deadline.HasValue && tournament.DeadlineTime.HasValue)
                {
                    deadlineDateTime = tournament.Deadline.Value.Date.Add(tournament.DeadlineTime.Value);

                    // Truncate both the eventDateTime and deadlineDateTime to the minute level
                    var eventTruncated = eventDateTime.AddSeconds(-eventDateTime.Second).AddMilliseconds(-eventDateTime.Millisecond).AddMicroseconds(-eventDateTime.Microsecond);
                    if (eventTruncated < deadlineDateTime)
                    {
                        ModelState.AddModelError("DateOrder", "Началната дата не може да бъде преди крайния срок.");
                    }
                }
            }

            tournament.Deadline = deadlineDateTime;
            tournament.Date = eventDateTime;

            if (ModelState.IsValid)
            {
                await _repository.Add(tournament.GetTournament());
                return Redirect(returnUrl);
            }

            ViewBag.ReturnUrl = returnUrl ?? Url.Action("AllTournaments", "Tournament");
            tournament.Sports = new SelectList(await _sportRepository.GetAll(), "Id", "Name");
            tournament.CountryList = await GetAllCountries();
            return View(tournament);
        }

        [Authorize(Roles = $"{SD.AdminRole},{SD.UserRole}")]
        public async Task<IActionResult> EditTournament(int id, string returnUrl = null)
        {
            var tournament = await _repository.GetById(id);

            var model = new TournamentViewModel()
            {
                Id = tournament.Id,
                OrganizerId = tournament.OrganizerId,
                Date = tournament.Date.Date,
                DateTimer = tournament.Date.TimeOfDay,
                Deadline = tournament.Deadline.Date,
                ImageUrl = tournament.ImageUrl,
                DeadlineTime = tournament.Deadline.TimeOfDay,
                Description = tournament.Description,
                Country = tournament.Country,
                CountryList = await GetAllCountries(),
                Name = tournament.Name,
                SportId = tournament.SportId,
                Sports = new SelectList(await _sportRepository.GetAll(), "Id", "Name", tournament.SportId),
            };

            var currentUser = await _userManager.GetUserAsync(this.User);
            ViewBag.UserId = currentUser.Id;
            ViewBag.ReturnUrl = returnUrl ?? Url.Action("AllTournaments", "Tournament");
            return View(model);
        }

        [Authorize(Roles = $"{SD.AdminRole},{SD.UserRole}")]
        [HttpPost]
        public async Task<IActionResult> EditTournament(TournamentViewModel tournament, string returnUrl = null)
        {
            var user = await _userManager.GetUserAsync(this.User);
            ViewBag.UserId = user.Id;

            if (ViewBag.UserId == tournament.OrganizerId)
            {
                if (string.IsNullOrWhiteSpace(tournament.Name) || tournament.Name.Length < 5 || tournament.Name.Length > 100)
                {
                    ModelState.AddModelError("Name", "Името трябва да е между 5 и 100 символа");
                }

                if (string.IsNullOrWhiteSpace(tournament.Description) || tournament.Description.Length < 5 || tournament.Description.Length > 100)
                {
                    ModelState.AddModelError("Description", "Описанието трябва да е между 5 и 100 символа");
                }

                if ((await _repository.GetAll()).Any(s => s.Name == tournament.Name && s.Id != tournament.Id))
                {
                    ModelState.AddModelError("Name", "Името е вече заето.");
                }

                if ((await _repository.GetAll()).Any(s => s.Description == tournament.Description && s.Id != tournament.Id))
                {
                    ModelState.AddModelError("Description", "Описанието е вече заето.");
                }

                if (tournament.SportId == null)
                {
                    ModelState.AddModelError("SportId", "Спортът е задължителен");
                }

                if (string.IsNullOrWhiteSpace(tournament.ImageUrl))
                {
                    ModelState.AddModelError("ImageUrl", "Задължителна");
                }

                if (string.IsNullOrWhiteSpace(tournament.Country))
                {
                    ModelState.AddModelError("Country", "Задължителна");
                }

                if (!tournament.Date.HasValue)
                {
                    ModelState.AddModelError("Date", "Датата е задължителна");
                }

                if (!tournament.Deadline.HasValue)
                {
                    ModelState.AddModelError("Deadline", "Краен срок е задължителен");
                }

                if (!tournament.DateTimer.HasValue)
                {
                    ModelState.AddModelError("DateTimer", "Задължителен");
                }

                if (!tournament.DeadlineTime.HasValue)
                {
                    ModelState.AddModelError("DeadlineTime", "Задължителен");
                }

                DateTime eventDateTime = tournament.Deadline.Value.Date.Add(tournament.DeadlineTime.Value);
                DateTime deadlineDateTime = tournament.Date.Value.Date.Add(tournament.DateTimer.Value);

                // Combine the Date and Time for Deadline and Event Date
                if (tournament.Deadline.HasValue && tournament.DeadlineTime.HasValue)
                {
                    deadlineDateTime = tournament.Deadline.Value.Date.Add(tournament.DeadlineTime.Value);

                    var deadlineDateTimeTruncated = deadlineDateTime.Date.AddHours(deadlineDateTime.TimeOfDay.Hours).AddMinutes(deadlineDateTime.TimeOfDay.Minutes);
                    var nowTruncated = DateTime.Now.Date.AddHours(DateTime.Now.TimeOfDay.Hours).AddMinutes(DateTime.Now.TimeOfDay.Minutes);
                    if (deadlineDateTimeTruncated < nowTruncated)
                    {
                        ModelState.AddModelError("DeadlineTime", "Не може да е в миналото.");
                    }
                }

                if (tournament.Date.HasValue && tournament.DateTimer.HasValue)
                {
                    eventDateTime = tournament.Date.Value.Date.Add(tournament.DateTimer.Value);

                    // Check if the event start date-time is before the deadline
                    if (tournament.Deadline.HasValue && tournament.DeadlineTime.HasValue)
                    {
                        deadlineDateTime = tournament.Deadline.Value.Date.Add(tournament.DeadlineTime.Value);

                        // Truncate both the eventDateTime and deadlineDateTime to the minute level
                        var eventTruncated = eventDateTime.AddSeconds(-eventDateTime.Second).AddMilliseconds(-eventDateTime.Millisecond).AddMicroseconds(-eventDateTime.Microsecond);
                        if (eventTruncated < deadlineDateTime)
                        {
                            ModelState.AddModelError("DateOrder", "Началната дата не може да бъде преди крайния срок.");
                        }
                    }
                }

                tournament.Deadline = deadlineDateTime;
                tournament.Date = eventDateTime;

                if (ModelState.IsValid)
                {
                    await _repository.Update(tournament.GetTournament());
                    return Redirect(returnUrl);
                }

                ViewBag.ReturnUrl = returnUrl ?? Url.Action("AllTournaments", "Tournament");
                tournament.Sports = new SelectList(await _sportRepository.GetAll(), "Id", "Name");
                tournament.CountryList = await GetAllCountries();
                return View(tournament);
            }
            else
            {
                if (string.IsNullOrWhiteSpace(tournament.Name) || tournament.Name.Length < 5 || tournament.Name.Length > 100)
                {
                    ModelState.AddModelError("Name", "Името трябва да е между 5 и 100 символа");
                }

                if (string.IsNullOrWhiteSpace(tournament.Description) || tournament.Description.Length < 5 || tournament.Description.Length > 100)
                {
                    ModelState.AddModelError("Description", "Описанието трябва да е между 5 и 100 символа");
                }

                if ((await _repository.GetAll()).Any(s => s.Name == tournament.Name && s.Id != tournament.Id))
                {
                    ModelState.AddModelError("Name", "Името е вече заето.");
                }

                if ((await _repository.GetAll()).Any(s => s.Description == tournament.Description && s.Id != tournament.Id))
                {
                    ModelState.AddModelError("Description", "Описанието е вече заето.");
                }

                if (string.IsNullOrWhiteSpace(tournament.ImageUrl))
                {
                    ModelState.AddModelError("ImageUrl", "Задължителна");
                }

                if (ModelState.IsValid)
                {
                    await _repository.Update(tournament.GetTournament());
                    return Redirect(returnUrl);
                }

                ViewBag.ReturnUrl = returnUrl ?? Url.Action("AllTournaments", "Tournament");
                return View(tournament);
            }
        }

        [Authorize(Roles = $"{SD.AdminRole},{SD.UserRole}")]
        public async Task<IActionResult> DeleteTournament(int id, string returnUrl = null)
        {
            var tournament = await _repository.GetById(id);

            var model = new TournamentViewModel()
            {
                Id = tournament.Id,
                Name = tournament.Name,
                ImageUrl = tournament.ImageUrl,
                Description = tournament.Description,
            };

            ViewBag.ReturnUrl = returnUrl ?? Url.Action("AllTournaments", "Tournament");
            return View(model);
        }

        [Authorize(Roles = $"{SD.AdminRole},{SD.UserRole}")]
        [HttpPost]
        public async Task<IActionResult> DeleteTournament(string ConfirmText, TournamentViewModel model, string returnUrl = null)
        {
            var tournament = await _repository.GetById((int)model.Id);

            if (ConfirmText == "ПОТВЪРДИ")
            {
                var range = await _participationsRepository.GetAllBy(x => x.TournamentId == tournament.Id);
                await _participationsRepository.DeleteRange(range);
                await _repository.Delete(tournament);
                return Redirect(returnUrl);
            }

            var model1 = new TournamentViewModel()
            {
                Id = tournament.Id,
                Name = tournament.Name,
                ImageUrl = tournament.ImageUrl,
                Description = tournament.Description,
            };

            ViewBag.ReturnUrl = returnUrl ?? Url.Action("AllTournaments", "Tournament");
            return View(model1);
        }

        [AllowAnonymous]
        public async Task<IActionResult> SportTournaments(int id, TournamentViewModel? filter)
        {
            var tournaments = (await _repository.AllWithIncludes(x => x.Organizer, x => x.Sport)).Where(x => x.SportId == id);

            if (filter == null)
            {
                return View(new TournamentViewModel());
            }

            var query = tournaments.AsQueryable();

            if (filter.Country != null)
            {
                query = query.Where(p => p.Country == filter.Country);
            }

            if (!string.IsNullOrWhiteSpace(filter.Name))
            {
                string filteredname = filter.Name.Trim().ToLower();
                query = query.Where(p => p.Name.ToLower().Contains(filteredname));
            }

            if (!string.IsNullOrWhiteSpace(filter.OrganizerName))
            {
                string filteredname = filter.OrganizerName.Trim().ToLower();
                query = query.Where(p => p.Organizer.FullName.ToLower().Contains(filteredname));
            }

            if (filter.StartDate != null)
            {
                query = query.Where(p => p.Date >= filter.StartDate);
            }

            if (filter.EndDate != null)
            {
                query = query.Where(p => p.Date <= filter.EndDate);
            }

            ViewBag.Countries = await GetAllCountries();

            var model = new TournamentViewModel
            {
                Country = filter.Country,
                Name = filter.Name,
                OrganizerName = filter.OrganizerName,
                StartDate = filter.StartDate,
                EndDate = filter.EndDate,
                FilteredTournaments = query.Include(x => x.Organizer).Include(x => x.Sport).ToList(),
                Tournaments = tournaments.ToList(),
            };

            return View(model);
        }

        [Authorize(Roles = $"{SD.AdminRole},{SD.UserRole}")]
        public async Task<IActionResult> UserTournaments(string id, TournamentViewModel? filter, string returnUrl = null)
        {
            ViewBag.ReturnUrl = returnUrl ?? Url.Action("UserDetails", "User", new { id = id });

            var tournaments = (await _repository.AllWithIncludes(x => x.Organizer, x => x.Sport)).Where(x => x.OrganizerId == id);

            if (filter == null)
            {
                return View(new TournamentViewModel());
            }

            var query = tournaments.AsQueryable();

            if (filter.SportId != null)
            {
                query = query.Where(p => p.SportId == filter.SportId.Value);
            }

            if (filter.Country != null)
            {
                query = query.Where(p => p.Country == filter.Country);
            }

            if (!string.IsNullOrWhiteSpace(filter.Name))
            {
                string filteredname = filter.Name.Trim().ToLower();
                query = query.Where(p => p.Name.ToLower().Contains(filteredname));
            }

            if (filter.StartDate != null)
            {
                query = query.Where(p => p.Date >= filter.StartDate);
            }

            if (filter.EndDate != null)
            {
                query = query.Where(p => p.Date <= filter.EndDate);
            }

            ViewBag.Sports = new SelectList(await _sportRepository.GetAll(), "Id", "Name");
            ViewBag.Countries = await GetAllCountries();

            var model = new TournamentViewModel
            {
                SportId = filter.SportId,
                Country = filter.Country,
                Name = filter.Name,
                StartDate = filter.StartDate,
                EndDate = filter.EndDate,
                FilteredTournaments = query.Include(x => x.Organizer).Include(x => x.Sport).ToList(),
                Tournaments = tournaments.ToList(),
            };

            return View(model);
        }

        [Authorize(Roles = $"{SD.UserRole},{SD.AdminRole}")]
        public async Task<IActionResult> MyTournaments(TournamentViewModel? filter)
        {
            var currentUser = await _userManager.GetUserAsync(this.User);
            var currentUserId = currentUser.Id;

            var tournaments = (await _repository.AllWithIncludes(x => x.Organizer, x => x.Sport)).Where(x => x.OrganizerId == currentUserId);

            if (filter == null)
            {
                return View(new TournamentViewModel());
            }

            var query = tournaments.AsQueryable();

            if (filter.SportId != null)
            {
                query = query.Where(p => p.SportId == filter.SportId.Value);
            }

            if (filter.Country != null)
            {
                query = query.Where(p => p.Country == filter.Country);
            }

            if (!string.IsNullOrWhiteSpace(filter.Name))
            {
                string filteredname = filter.Name.Trim().ToLower();
                query = query.Where(p => p.Name.ToLower().Contains(filteredname));
            }

            if (filter.StartDate != null)
            {
                query = query.Where(p => p.Date >= filter.StartDate);
            }

            if (filter.EndDate != null)
            {
                query = query.Where(p => p.Date <= filter.EndDate);
            }

            ViewBag.Sports = new SelectList(await _sportRepository.GetAll(), "Id", "Name");
            ViewBag.Countries = await GetAllCountries();

            var model = new TournamentViewModel
            {
                SportId = filter.SportId,
                Country = filter.Country,
                Name = filter.Name,
                StartDate = filter.StartDate,
                EndDate = filter.EndDate,
                FilteredTournaments = query.Include(x => x.Organizer).Include(x => x.Sport).ToList(),
                Tournaments = tournaments.ToList(),
            };

            return View(model);
        }

        [AllowAnonymous]
        public async Task<IActionResult> TournamentDetails(int id, string returnUrl = null)
        {
            var range = (await _participationsRepository.AllWithIncludes(x => x.Tournament, x => x.Participant)).Where(x => x.TournamentId == id);
            var tournament = (await _repository.AllWithIncludes(x => x.Organizer, x => x.Sport)).FirstOrDefault(x => x.Id == id);

            var user = (await _userManager.GetUserAsync(this.User));
            ViewBag.UserId = user.Id;

            var model = new TournamentViewModel()
            {
                Id = tournament.Id,
                OrganizerName = tournament.Organizer.FullName,
                Organizer = tournament.Organizer,
                Date = tournament.Date,
                Deadline = tournament.Deadline,
                Description = tournament.Description,
                Country = tournament.Country,
                Name = tournament.Name,
                ImageUrl = tournament.ImageUrl,
                Participations = range,
                SportName = tournament.Sport.Name,
            };

            ViewBag.ReturnUrl = returnUrl ?? Url.Action("TournamentDetails", "Tournament");
            return View(model);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}