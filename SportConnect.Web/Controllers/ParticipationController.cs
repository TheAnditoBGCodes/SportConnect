using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using SportConnect.DataAccess.Repository.IRepository;
using SportConnect.Models;
using SportConnect.Services;
using SportConnect.Utility;
using SportConnect.Web.Models;
using System.Diagnostics;

namespace SportConnect.Web.Controllers
{
    public class ParticipationController : Controller
    {
        public UserManager<SportConnectUser> _userManager;
        public IRepository<Tournament> _tournamentRepository { get; set; }
        public IRepository<Sport> _sportRepository { get; set; }
        public IRepository<Participation> _participationRepository { get; set; }
        public IRepository<SportConnectUser> _userRepository { get; set; }
        public CountryService _countryService { get; set; }

        public ParticipationController(UserManager<SportConnectUser> userManager, IRepository<Tournament> tournamentRepository, IRepository<Sport> sportRepository, IRepository<Participation> participationRepository, IRepository<SportConnectUser> userRepository, CountryService countryService)
        {
            _userManager = userManager;
            _tournamentRepository = tournamentRepository;
            _sportRepository = sportRepository;
            _participationRepository = participationRepository;
            _userRepository = userRepository;
            _countryService = countryService;
        }

        [Authorize(Roles = $"{SD.UserRole},{SD.AdminRole}")]
        public async Task<IActionResult> MyParticipations(TournamentViewModel filter = null)
        {
            HttpContext.Session.Remove("ReturnUrl");

            var allSports = await _tournamentRepository.GetAll(); if (filter == null)
            {
                return View(new TournamentViewModel
                {
                    Tournaments = allSports.ToList(),
                    FilteredTournaments = allSports.ToList()
                });
            }

            var currentUser = (await _userManager.GetUserAsync(this.User)).Id;
            ViewBag.UserId = currentUser;

            var query = (await _tournamentRepository.AllWithIncludes(t => t.Participations, t => t.Organizer, t => t.Sport))
                .Where(t => t.Participations.Any(p => p.ParticipantId == currentUser));

            if (filter.Approved.HasValue)
            {
                bool isApproved = filter.Approved.Value;
                query = query.Where(t => t.Participations.Any(p => p.ParticipantId == currentUser && p.Approved == isApproved));
            }

            if (filter.SportId != null)
            {
                query = query.Where(p => p.SportId.ToString() == filter.SportId.ToString());
            }

            if (!string.IsNullOrWhiteSpace(filter.Country))
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
                query = query.Where(p => p.Organizer.FullName.ToLower().Contains(filteredname) || p.Organizer.UserName.ToLower().Contains(filteredname));
            }

            if (filter.StartDate != null)
            {
                query = query.Where(p => p.Date >= filter.StartDate);
            }

            if (filter.EndDate != null)
            {
                query = query.Where(p => p.Date <= filter.EndDate);
            }

            var filteredTournaments = query.ToList();

            ViewBag.Sports = new SelectList(await _sportRepository.GetAll(), "Id", "Name");
            ViewBag.Countries = _countryService.GetAllCountries();

            var model = new TournamentViewModel
            {
                SportId = filter.SportId,
                Country = filter.Country,
                Name = filter.Name,
                OrganizerName = filter.OrganizerName,
                StartDate = filter.StartDate,
                EndDate = filter.EndDate,
                Approved = filter.Approved,
                FilteredTournaments = filteredTournaments,
                Tournaments = (await _tournamentRepository.AllWithIncludes(t => t.Participations, t => t.Organizer, t => t.Sport))
                .Where(t => t.Participations.Any(p => p.ParticipantId == currentUser)).ToList(),
            };

            return View(model);
        }

        [AllowAnonymous]
        public async Task<IActionResult> TournamentParticipations(string id, string returnUrl, UserViewModel? filter)
        {
            var tournament = await _tournamentRepository.GetById(id);
            if (tournament == null)
            {
                return View("~/Views/Shared/NotFound.cshtml");
            }

            var allSports = await _userRepository.GetAll(); if (filter == null)
            {
                return View(new UserViewModel
                {
                    Users = allSports.ToList(),
                    FilteredUsers = allSports.ToList()
                });
            }

            var currentUser = await _userManager.GetUserAsync(this.User);
            if (currentUser != null)
            {
                ViewBag.UserId = currentUser.Id;
            }

            var query = (await _userRepository.AllWithIncludes(t => t.Participations)).Where(t => t.Participations.Any(p => p.TournamentId.ToString() == id.ToString()));

            if (filter.Approved.HasValue)
            {
                bool isApproved = filter.Approved.Value;
                query = query.Where(t => t.Participations.Any(p => p.Approved == isApproved));
            }

            if (!string.IsNullOrEmpty(filter.Country))
            {
                query = query.Where(p => p.Country == filter.Country);
            }

            if (!string.IsNullOrEmpty(filter.UserName))
            {
                string trimmedFilter = filter.UserName.Trim().ToLower();

                query = query.Where(p => p.UserName.Trim().ToLower().Contains(trimmedFilter) || p.FullName.Trim().ToLower().Contains(trimmedFilter));
            }

            if (!string.IsNullOrEmpty(filter.Email))
            {
                string trimmedFilter = filter.Email.Trim().ToLower();

                query = query.Where(p => p.Email.Trim().ToLower().Contains(trimmedFilter));
            }

            if (filter.BirthYear.HasValue)
            {
                query = query.Where(p => int.Parse(p.DateOfBirth.Split('-')[0]) == filter.BirthYear);
            }

            var filteredTournaments = query.ToList();

            ViewBag.Sports = new SelectList(await _sportRepository.GetAll(), "Id", "Name");
            ViewBag.Countries = _countryService.GetAllCountries();
            ViewBag.Tournament = (await _tournamentRepository.GetById(id));

            var model = new UserViewModel
            {
                UserName = filter.UserName,
                BirthYear = filter.BirthYear,
                Country = filter.Country,
                Email = filter.Email,
                FilteredUsers = filteredTournaments,
                Approved = filter.Approved,
                Users = (await _userRepository.AllWithIncludes(t => t.Participations)).Where(t => t.Participations.Any(p => p.TournamentId.ToString() == id.ToString())).ToList(),
            };

            var storedReturnUrl = HttpContext.Session.GetString("ReturnUrl");
            if (!string.IsNullOrEmpty(returnUrl) && string.IsNullOrEmpty(storedReturnUrl))
            {
                HttpContext.Session.SetString("ReturnUrl", returnUrl);
            }

            ViewBag.ReturnUrl = HttpContext.Session.GetString("ReturnUrl") ?? Url.Action("AllTournaments", "Tournament");
            return View(model);
        }

        [AllowAnonymous]
        public async Task<IActionResult> UserParticipations(string id, string? tournamentId, TournamentViewModel? filter, string returnUrl, string tournamentUrl = null)
        {
            if (tournamentId != null)
            {
                var tournament = await _tournamentRepository.GetById(tournamentId);
                if (tournament == null)
                {
                    return View("~/Views/Shared/NotFound.cshtml");
                }
            }

            var user = await _userRepository.GetById(id);
            if (user == null)
            {
                return View("~/Views/Shared/NotFound.cshtml");
            }

            var allSports = await _tournamentRepository.GetAll(); if (filter == null)
            {
                return View(new TournamentViewModel
                {
                    Tournaments = allSports.ToList(),
                    FilteredTournaments = allSports.ToList()
                });
            }

            var currentUser = await _userRepository.GetById(id);
            ViewBag.UserId = currentUser.Id;

            var query = (await _tournamentRepository.AllWithIncludes(t => t.Participations, t => t.Organizer, t => t.Sport))
                .Where(t => t.Participations.Any(p => p.ParticipantId == currentUser.Id));

            if (filter.Approved.HasValue)
            {
                bool isApproved = filter.Approved.Value;
                query = query.Where(t => t.Participations.Any(p => p.ParticipantId == currentUser.Id && p.Approved == isApproved));
            }

            if (filter.SportId != null)
            {
                query = query.Where(p => p.SportId.ToString() == filter.SportId.ToString());
            }

            if (!string.IsNullOrWhiteSpace(filter.Country))
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
                query = query.Where(p => p.Organizer.FullName.ToLower().Contains(filteredname) || p.Organizer.UserName.ToLower().Contains(filteredname));
            }

            if (filter.StartDate != null)
            {
                query = query.Where(p => p.Date >= filter.StartDate);
            }

            if (filter.EndDate != null)
            {
                query = query.Where(p => p.Date <= filter.EndDate);
            }

            var filteredTournaments = query.ToList();

            ViewBag.Sports = new SelectList(await _sportRepository.GetAll(), "Id", "Name");
            ViewBag.Countries = _countryService.GetAllCountries();
            ViewBag.CheckedUser = currentUser.UserName;

            var model = new TournamentViewModel
            {
                PreviousId = tournamentId,
                SportId = filter.SportId,
                Country = filter.Country,
                Name = filter.Name,
                OrganizerName = filter.OrganizerName,
                StartDate = filter.StartDate,
                EndDate = filter.EndDate,
                Approved = filter.Approved,
                FilteredTournaments = filteredTournaments,
                Tournaments = (await _tournamentRepository.AllWithIncludes(t => t.Participations, t => t.Organizer, t => t.Sport))
                .Where(t => t.Participations.Any(p => p.ParticipantId == currentUser.Id)).ToList(),
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
        public async Task<IActionResult> AddParticipation(string id, string returnUrl)
        {
            var tournament = await _tournamentRepository.GetById(id);
            if (tournament == null)
            {
                return View("~/Views/Shared/NotFound.cshtml");
            }

            var participation = new Participation
            {
                ParticipantId = (await _userManager.GetUserAsync(this.User)).Id,
                TournamentId = id,
                Approved = false,
            };
            await _participationRepository.Add(participation);
            return Redirect(returnUrl);
        }

        [Authorize(Roles = $"{SD.AdminRole},{SD.UserRole}")]
        public async Task<IActionResult> DeleteParticipation(string tournamentId, string userId, string returnUrl = null)
        {
            var tournament = await _tournamentRepository.GetById(tournamentId);
            var user = await _userRepository.GetById(userId);
            if (tournament == null || user == null)
            {
                return View("~/Views/Shared/NotFound.cshtml");
            }

            var participation = (await _participationRepository.GetAll()).FirstOrDefault(x => x.ParticipantId == userId && x.TournamentId.ToString() == tournamentId.ToString());
            tournament.Participations.ToList().Remove(participation);
            await _participationRepository.Delete(participation);
            return Redirect(returnUrl);
        }

        [Authorize(Roles = $"{SD.AdminRole}")]
        public async Task<IActionResult> ApproveParticipation(string tournamentId, string userId, string returnUrl = null)
        {
            var tournament = await _tournamentRepository.GetById(tournamentId);
            var user = await _userRepository.GetById(userId);
            if (tournament == null || user == null)
            {
                return View("~/Views/Shared/NotFound.cshtml");
            }
            var participation = (await _participationRepository.GetAll()).FirstOrDefault(x => x.ParticipantId == userId && x.TournamentId.ToString() == tournamentId.ToString());
            participation.Approved = true;
            await _participationRepository.Update(participation);
            return Redirect(returnUrl);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}