using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SportConnect.DataAccess.Repository.IRepository;
using SportConnect.Models;
using SportConnect.Services;
using SportConnect.Services.Participation;
using SportConnect.Services.Sport;
using SportConnect.Services.Tournament;
using SportConnect.Services.User;
using SportConnect.Utility;
using SportConnect.Web.Models;
using System.Diagnostics;
using System.Globalization;
using static System.Net.Mime.MediaTypeNames;

namespace SportConnect.Web.Controllers
{
    public class TournamentController : Controller
    {
        public UserManager<SportConnectUser> _userManager;
        public ITournamentService _tournamentService;
        public IUserService _userService;
        public ISportService _sportService;
        public IParticipationService _participationService;
        public CountryService _countryService;

        public TournamentController(UserManager<SportConnectUser> userManager, ITournamentService tournamentService, IUserService userService, ISportService sportService, IParticipationService participationService, CountryService countryService)
        {
            _userManager = userManager;
            _tournamentService = tournamentService;
            _userService = userService;
            _sportService = sportService;
            _participationService = participationService;
            _countryService = countryService;
        }

        [AllowAnonymous]
        public async Task<IActionResult> AllTournaments(TournamentViewModel? filter)
        {
            HttpContext.Session.Remove("ReturnUrl");
            if (filter == null)
            {
                return View(new TournamentViewModel());
            }

            var currentUser = await _userManager.GetUserAsync(this.User);
            if (currentUser != null)
            {
                var currentUserId = currentUser.Id;
                ViewBag.UserId = currentUserId;
            }

            var tournaments = await _tournamentService.AllWithIncludes(x => x.Organizer, x => x.Sport, x => x.Participations);

            var query = tournaments.AsQueryable();

            if (filter.SportId != null)
            {
                query = query.Where(p => p.SportId.ToString() == filter.SportId.ToString());
            }

            if (!string.IsNullOrWhiteSpace(filter.OrganizerName))
            {
                string filteredname = filter.OrganizerName.Trim().ToLower();
                query = query.Where(p => p.Organizer.FullName.ToLower().Contains(filteredname));
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
                query = query.Where(p => DateTime.Parse(p.Date) >= filter.StartDate);
            }

            if (filter.EndDate != null)
            {
                query = query.Where(p => DateTime.Parse(p.Date) <= filter.EndDate);
            }

            ViewBag.Sports = new SelectList(await _sportService.GetAll(), "Id", "Name");
            ViewBag.Countries = _countryService.GetAllCountries();

            var model = new TournamentViewModel
            {
                OrganizerName = filter.OrganizerName,
                SportId = filter.SportId,
                Country = filter.Country,
                Name = filter.Name,
                StartDate = filter.StartDate,
                EndDate = filter.EndDate,
                FilteredTournaments = query.Include(x => x.Organizer).Include(x => x.Sport).Include(x => x.Participations).ToList(),
                Tournaments = tournaments.ToList(),
            };

            return View(model);
        }

        [Authorize(Roles = $"{SD.UserRole},{SD.AdminRole}")]
        public async Task<IActionResult> AddTournament(string returnUrl)
        {
            var currentUser = await _userManager.GetUserAsync(this.User);
            var model = new TournamentViewModel()
            {
                OrganizerId = currentUser.Id,
                CountryList = _countryService.GetAllCountries(),
                Sports = new SelectList(await _sportService.GetAll(), "Id", "Name")
            };
            ViewBag.ReturnUrl = returnUrl;
            return View(model);
        }

        [Authorize(Roles = $"{SD.UserRole},{SD.AdminRole}")]
        [HttpPost]
        public async Task<IActionResult> AddTournament(TournamentViewModel tournament, string returnUrl)
        {
            if (string.IsNullOrWhiteSpace(tournament.Name) || tournament.Name.Length < 5 || tournament.Name.Length > 100)
            {
                ModelState.AddModelError("Name", "Името трябва да е между 5 и 100 символа");
            }

            if (string.IsNullOrWhiteSpace(tournament.Description) || tournament.Description.Length < 5 || tournament.Description.Length > 100)
            {
                ModelState.AddModelError("Description", "Описанието трябва да е между 5 и 100 символа");
            }

            if ((await _tournamentService.GetAll()).Any(s => s.Name == tournament.Name))
            {
                ModelState.AddModelError("Name", "Името е вече заето.");
            }

            if ((await _tournamentService.GetAll()).Any(s => s.Description == tournament.Description))
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

            if (tournament.Deadline != null && tournament.DeadlineTime != null && tournament.Date != null && tournament.DateTimer != null)
            {
                DateTime eventDateTime = tournament.Deadline.Value.Date.Add(tournament.DeadlineTime.Value);
                DateTime deadlineDateTime = tournament.Date.Value.Date.Add(tournament.DateTimer.Value);

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

                    if (tournament.Deadline.HasValue && tournament.DeadlineTime.HasValue)
                    {
                        deadlineDateTime = tournament.Deadline.Value.Date.Add(tournament.DeadlineTime.Value);

                        var eventTruncated = eventDateTime.AddSeconds(-eventDateTime.Second).AddMilliseconds(-eventDateTime.Millisecond).AddMicroseconds(-eventDateTime.Microsecond);
                        if (eventTruncated < deadlineDateTime)
                        {
                            ModelState.AddModelError("DateOrder", "Началната дата не може да бъде преди крайния срок.");
                        }
                    }
                }

                tournament.Deadline = deadlineDateTime;
                tournament.Date = eventDateTime;
            }

            if (ModelState.IsValid)
            {
                await _tournamentService.Add(await tournament.ToTournament());
                return Redirect(returnUrl);
            }

            ViewBag.ReturnUrl = returnUrl;
            tournament.Sports = new SelectList(await _sportService.GetAll(), "Id", "Name");
            tournament.CountryList = _countryService.GetAllCountries();
            return View(tournament);
        }

        [Authorize(Roles = $"{SD.UserRole},{SD.AdminRole}")]
        public async Task<IActionResult> EditTournament(string id, string returnUrl)
        {
            var tournament = await _tournamentService.GetById(id);
            if (tournament == null)
            {
                return View("~/Views/Shared/NotFound.cshtml");
            }

            var model = new TournamentViewModel()
            {
                Id = tournament.Id,
                OrganizerId = tournament.OrganizerId,
                Date = DateTime.Parse(tournament.Date).Date,
                DateTimer = DateTime.Parse(tournament.Date).TimeOfDay,
                Deadline = DateTime.Parse(tournament.Deadline).Date,
                ImageUrl = tournament.ImageUrl,
                DeadlineTime = DateTime.Parse(tournament.Deadline).TimeOfDay,
                Description = tournament.Description,
                Country = tournament.Country,
                CountryList = _countryService.GetAllCountries(),
                Name = tournament.Name,
                SportId = tournament.SportId,
                Sports = new SelectList(await _sportService.GetAll(), "Id", "Name", tournament.SportId),
            };

            var currentUser = await _userManager.GetUserAsync(this.User);
            ViewBag.UserId = currentUser.Id;

            ViewBag.ReturnUrl = returnUrl;
            return View(model);
        }

        [Authorize(Roles = $"{SD.UserRole},{SD.AdminRole}")]
        [HttpPost]
        public async Task<IActionResult> EditTournament(TournamentViewModel tournament, string returnUrl)
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

                if ((await _tournamentService.GetAll()).Any(s => s.Name == tournament.Name && s.Id != tournament.Id))
                {
                    ModelState.AddModelError("Name", "Името е вече заето.");
                }

                if ((await _tournamentService.GetAll()).Any(s => s.Description == tournament.Description && s.Id != tournament.Id))
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

                if (tournament.Deadline != null && tournament.DeadlineTime != null && tournament.Date != null && tournament.DateTimer != null)
                {
                    DateTime eventDateTime = tournament.Deadline.Value.Date.Add(tournament.DeadlineTime.Value);
                    DateTime deadlineDateTime = tournament.Date.Value.Date.Add(tournament.DateTimer.Value);

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

                        if (tournament.Deadline.HasValue && tournament.DeadlineTime.HasValue)
                        {
                            deadlineDateTime = tournament.Deadline.Value.Date.Add(tournament.DeadlineTime.Value);

                            var eventTruncated = eventDateTime.AddSeconds(-eventDateTime.Second).AddMilliseconds(-eventDateTime.Millisecond).AddMicroseconds(-eventDateTime.Microsecond);
                            if (eventTruncated < deadlineDateTime)
                            {
                                ModelState.AddModelError("DateOrder", "Началната дата не може да бъде преди крайния срок.");
                            }
                        }
                    }

                    tournament.Deadline = deadlineDateTime;
                    tournament.Date = eventDateTime;
                }

                var edited = (await _tournamentService.GetById(tournament.Id));

                edited.Name = tournament.Name;
                edited.Description = tournament.Description;
                edited.ImageUrl = tournament.ImageUrl; 
                edited.Deadline = ((DateTime)tournament.Deadline).ToString("yyyy-MM-ddTHH:mm:ss");
                edited.Date = ((DateTime)tournament.Date).ToString("yyyy-MM-ddTHH:mm:ss");
                edited.Country = tournament.Country;
                edited.SportId = tournament.SportId;

                if (ModelState.IsValid)
                {
                    await _tournamentService.Update(edited);
                    return Redirect(returnUrl);
                }

                ViewBag.ReturnUrl = returnUrl;
                tournament.Sports = new SelectList(await _sportService.GetAll(), "Id", "Name");
                tournament.CountryList = _countryService.GetAllCountries();
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

                if ((await _tournamentService.GetAll()).Any(s => s.Name == tournament.Name && s.Id != tournament.Id))
                {
                    ModelState.AddModelError("Name", "Името е вече заето.");
                }

                if ((await _tournamentService.GetAll()).Any(s => s.Description == tournament.Description && s.Id != tournament.Id))
                {
                    ModelState.AddModelError("Description", "Описанието е вече заето.");
                }

                if (string.IsNullOrWhiteSpace(tournament.ImageUrl))
                {
                    ModelState.AddModelError("ImageUrl", "Задължителна");
                }

                var edited = (await _tournamentService.GetById(tournament.Id));

                edited.Name = tournament.Name;
                edited.Description = tournament.Description;
                edited.ImageUrl = tournament.ImageUrl;

                if (ModelState.IsValid)
                {
                    await _tournamentService.Update(edited);
                    return Redirect(returnUrl);
                }

                ViewBag.ReturnUrl = returnUrl;
                return View(tournament);
            }
        }

        [Authorize(Roles = $"{SD.UserRole},{SD.AdminRole}")]
        public async Task<IActionResult> DeleteTournament(string id, string returnUrl)
        {
            var tournament = (await _tournamentService.AllWithIncludes(x => x.Sport)).FirstOrDefault(x => x.Id == id);
            if (tournament == null)
            {
                return View("~/Views/Shared/NotFound.cshtml");
            }

            var model = new TournamentViewModel()
            {
                SportId = tournament.Sport.Id,
                Id = tournament.Id,
                Name = tournament.Name,
                ImageUrl = tournament.ImageUrl,
                Description = tournament.Description,
            };

            ViewBag.ReturnUrl = returnUrl;
            return View(model);
        }

        [Authorize(Roles = $"{SD.UserRole},{SD.AdminRole}")]
        [HttpPost]
        public async Task<IActionResult> DeleteTournament(string ConfirmText, TournamentViewModel model, string returnUrl)
        {
            var tournament = await _tournamentService.GetById(model.Id);
            ViewBag.ReturnUrl = returnUrl;

            if (ConfirmText == "ПОТВЪРДИ")
            {
                var range = await _participationService.GetAllBy(x => x.TournamentId == tournament.Id);
                await _participationService.DeleteRange(range);
                await _tournamentService.Delete(tournament);
                return Redirect(returnUrl);
            }

            var model1 = new TournamentViewModel()
            {
                Id = tournament.Id,
                SportId = tournament.SportId,
                Name = tournament.Name,
                ImageUrl = tournament.ImageUrl,
                Description = tournament.Description,
            };

            return View(model1);
        }

        [AllowAnonymous]
        public async Task<IActionResult> SportTournaments(string id, TournamentViewModel? filter)
        {
            HttpContext.Session.Remove("ReturnUrl");
            var sport = await _sportService.GetById(id);
            if (sport == null)
            {
                return View("~/Views/Shared/NotFound.cshtml");
            }
            var tournaments = await _tournamentService.TournamentsOfSport(id);

            if (filter == null)
            {
                return View(new TournamentViewModel());
            }

            var currentUser = await _userManager.GetUserAsync(this.User);
            if (currentUser != null)
            {
                var currentUserId = currentUser.Id;
                ViewBag.UserId = currentUserId;
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
                query = query.Where(p => DateTime.Parse(p.Date) >= filter.StartDate);
            }

            if (filter.EndDate != null)
            {
                query = query.Where(p => DateTime.Parse(p.Date) <= filter.EndDate);
            }

            ViewBag.Countries = _countryService.GetAllCountries();
            ViewBag.SportName = (await _sportService.GetById(id)).Name;

            var model = new TournamentViewModel
            {
                OrganizerName = filter.OrganizerName,
                SportId = id,
                Country = filter.Country,
                Name = filter.Name,
                StartDate = filter.StartDate,
                EndDate = filter.EndDate,
                FilteredTournaments = (await _tournamentService.TournamentsOfSport(id)).ToList(),
                Tournaments = tournaments.ToList(),
            };

            return View(model);
        }

        [AllowAnonymous]
        public async Task<IActionResult> UserTournaments(string id, string? tournamentId, TournamentViewModel? filter, string returnUrl, string tournamentUrl = null)
        {
            if (tournamentId != null)
            {
                var tournament = await _tournamentService.GetById(tournamentId);
                if (tournament == null)
                {
                    return View("~/Views/Shared/NotFound.cshtml");
                }
            }

            var user = await _userService.GetById(id);
            if (user == null)
            {
                return View("~/Views/Shared/NotFound.cshtml");
            }

            var tournaments = await _tournamentService.TournamentsOfUser(id);
            ViewBag.OtherUserId = id;
            ViewBag.CheckedUser = (await _userService.GetById(id)).UserName;

            if (filter == null)
            {
                return View(new TournamentViewModel());
            }

            var query = tournaments.AsQueryable();

            if (filter.SportId != null)
            {
                query = query.Where(p => p.SportId.ToString() == filter.SportId.ToString());
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
                query = query.Where(p => DateTime.Parse(p.Date) >= filter.StartDate);
            }

            if (filter.EndDate != null)
            {
                query = query.Where(p => DateTime.Parse(p.Date) <= filter.EndDate);
            }

            ViewBag.Sports = new SelectList(await _sportService.GetAll(), "Id", "Name");
            ViewBag.Countries = _countryService.GetAllCountries();

            var currentUser = await _userManager.GetUserAsync(this.User);
            if (currentUser != null)
            {
                ViewBag.UserId = currentUser.Id;
            }

            var model = new TournamentViewModel
            {
                PreviousId = tournamentId,
                SportId = filter.SportId,
                Country = filter.Country,
                Name = filter.Name,
                StartDate = filter.StartDate,
                EndDate = filter.EndDate,
                FilteredTournaments = query.Include(x => x.Organizer).Include(X => X.Participations).Include(x => x.Sport).ToList(),
                Tournaments = tournaments.ToList(),
            };

            var storedRootReturnUrl = HttpContext.Session.GetString("RootReturnUrl");
            var storedCurrentReturnUrl = HttpContext.Session.GetString("CurrentReturnUrl");

            if (!string.IsNullOrEmpty(returnUrl))
            {
                if (string.IsNullOrEmpty(storedRootReturnUrl))
                {
                    HttpContext.Session.SetString("RootReturnUrl", returnUrl);
                }

                HttpContext.Session.SetString("CurrentReturnUrl", returnUrl);
            }

            ViewBag.ReturnUrl = HttpContext.Session.GetString("CurrentReturnUrl") ?? Url.Action("AllTournaments", "Tournament");
            ViewBag.RootReturnUrl = HttpContext.Session.GetString("RootReturnUrl") ?? Url.Action("TournamentParticipations", "Participation", new { id = tournamentId });
            if (tournamentUrl != null)
            {
                ViewBag.RootReturnUrl = tournamentUrl;
            }
            return View(model);
        }

        [Authorize(Roles = $"{SD.UserRole},{SD.AdminRole}")]
        public async Task<IActionResult> MyTournaments(TournamentViewModel? filter)
        {
            HttpContext.Session.Remove("ReturnUrl");

            var currentUser = await _userManager.GetUserAsync(this.User);
            var currentUserId = currentUser.Id;
            ViewBag.UserId = currentUserId;

            var tournaments = await _tournamentService.TournamentsOfUser(currentUserId);

            if (filter == null)
            {
                return View(new TournamentViewModel());
            }

            var query = tournaments.AsQueryable();

            if (filter.SportId != null)
            {
                query = query.Where(p => p.SportId.ToString() == filter.SportId.ToString());
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
                query = query.Where(p => DateTime.Parse(p.Date) >= filter.StartDate);
            }

            if (filter.EndDate != null)
            {
                query = query.Where(p => DateTime.Parse(p.Date) <= filter.EndDate);
            }

            ViewBag.Sports = new SelectList(await _sportService.GetAll(), "Id", "Name");
            ViewBag.Countries = _countryService.GetAllCountries();

            var model = new TournamentViewModel
            {
                SportId = filter.SportId,
                Country = filter.Country,
                Name = filter.Name,
                StartDate = filter.StartDate,
                EndDate = filter.EndDate,
                FilteredTournaments = query.Include(x => x.Organizer).Include(x => x.Sport).Include(x => x.Participations).ToList(),
                Tournaments = tournaments.ToList(),
            };

            return View(model);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}