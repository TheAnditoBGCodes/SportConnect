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
using System.Text.Json;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace SportConnect.Web.Controllers
{
    public class ParticipationController : Controller
    {
        private readonly ILogger<TournamentController> _logger;
        private readonly UserManager<SportConnectUser> _userManager;
        public IRepository<Tournament> _tournamentsRepository { get; set; }
        public IRepository<Sport> _sportRepository { get; set; }
        public IRepository<Participation> _participationsRepository { get; set; }
        public IRepository<SportConnectUser> _usersRepository { get; set; }
        private readonly HttpClient _httpClient;
        private readonly Cloudinary _cloudinary;
        private readonly CloudinaryService _cloudinaryService;

        public ParticipationController(ILogger<TournamentController> logger, UserManager<SportConnectUser> userManager, IRepository<Tournament> tournamentsRepository, IRepository<Sport> sportRepository, IRepository<Participation> participationsRepository, IRepository<SportConnectUser> usersRepository, HttpClient httpClient, Cloudinary cloudinary, CloudinaryService cloudinaryService)
        {
            _logger = logger;
            _userManager = userManager;
            _tournamentsRepository = tournamentsRepository;
            _sportRepository = sportRepository;
            _participationsRepository = participationsRepository;
            _usersRepository = usersRepository;
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

        [Authorize(Roles = $"{SD.AdminRole},{SD.UserRole}")]
        public async Task<IActionResult> MyParticipations(TournamentViewModel? filter, string returnUrl = null)
        {
            if (filter == null)
            {
                return View(new TournamentViewModel());
            }

            var currentUser = _userManager.GetUserAsync(this.User).Result.Id;
            ViewBag.UserId = currentUser;
            // Start querying directly from the database
            var query = _tournamentsRepository.AllWithIncludes(t => t.Participations, t => t.Organizer, t => t.Sport)
                .Where(t => t.Participations.Any(p => p.ParticipantId == currentUser));

            // Apply approval status filter
            if (filter.Approved.HasValue)
            {
                bool isApproved = filter.Approved.Value;
                query = query.Where(t => t.Participations.Any(p => p.ParticipantId == currentUser && p.Approved == isApproved));
            }

            // Apply other filters
            if (filter.SportId != null)
            {
                query = query.Where(p => p.SportId == filter.SportId.Value);
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

            // Execute query before ViewModel assignment
            var filteredTournaments = query.ToList();

            ViewBag.Sports = new SelectList(_sportRepository.GetAll(), "Id", "Name");
            ViewBag.Countries = await GetAllCountries();

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
                Tournaments = _tournamentsRepository.AllWithIncludes(t => t.Participations, t => t.Organizer, t => t.Sport)
                .Where(t => t.Participations.Any(p => p.ParticipantId == currentUser)).ToList(),
            };

            ViewBag.ReturnUrl = returnUrl ?? Url.Action("MyParticipations", "Participation");
            return View(model);
        }
        
        [Authorize(Roles = $"{SD.AdminRole},{SD.UserRole}")]
        public async Task<IActionResult> TournamentParticipations(int id, UserViewModel? filter, string returnUrl = null)
        {
            if (filter == null)
            {
                return View(new UserViewModel());
            }

            var currentUser = _userManager.GetUserAsync(this.User).Result.Id;
            ViewBag.UserId = currentUser;

            var query = _usersRepository.AllWithIncludes(t => t.Participations).Where(t => t.Participations.Any(p => p.TournamentId == id));

            // Apply approval status filter
            if (filter.Approved.HasValue)
            {
                bool isApproved = filter.Approved.Value;
                query = query.Where(t => t.Participations.Any(p => p.ParticipantId == currentUser && p.Approved == isApproved));
            }

            // Filter by country if selected
            if (!string.IsNullOrEmpty(filter.Country))
            {
                query = query.Where(p => p.Country == filter.Country);
            }

            // Filter by UserName or FullName if either contains the filter value, case-insensitive and trimming spaces
            if (!string.IsNullOrEmpty(filter.UserName))
            {
                string trimmedFilter = filter.UserName.Trim().ToLower();

                query = query.Where(p => p.UserName.Trim().ToLower().Contains(trimmedFilter) || p.FullName.Trim().ToLower().Contains(trimmedFilter));
            }

            // Filter by UserName or FullName if either contains the filter value, case-insensitive and trimming spaces
            if (!string.IsNullOrEmpty(filter.Email))
            {
                string trimmedFilter = filter.Email.Trim().ToLower();

                query = query.Where(p => p.Email.Trim().ToLower().Contains(trimmedFilter));
            }

            // Filter users by the specified birth year
            if (filter.BirthYear.HasValue)
            {
                // Filter users born in the selected year
                query = query.Where(p => p.DateOfBirth.Year == filter.BirthYear);
            }

            var filteredTournaments = query.ToList();

            ViewBag.Sports = new SelectList(_sportRepository.GetAll(), "Id", "Name");
            ViewBag.Countries = await GetAllCountries();
            ViewBag.Tournament = _tournamentsRepository.GetById(id);

            var model = new UserViewModel
            {
                UserName = filter.UserName,
                BirthYear = filter.BirthYear,
                Country = filter.Country,
                Email = filter.Email,
                FilteredUsers = filteredTournaments,
                Approved = filter.Approved,
                Users = _usersRepository.GetAll().Where(t => t.Participations.Any(p => p.TournamentId == id)).ToList(),
            };

            ViewBag.ReturnUrl = returnUrl ?? Url.Action("TournamentDetails", "Tournament", new { id = id });
            return View(model);
        }

        [Authorize(Roles = $"{SD.AdminRole},{SD.UserRole}")]
        public async Task<IActionResult> UserParticipations(string id, TournamentViewModel? filter, string returnUrl = null)
        {
            if (filter == null)
            {
                return View(new TournamentViewModel());
            }

            var currentUser = _usersRepository.GetUserById(id).Id;
            ViewBag.UserId = currentUser;

            var query = _tournamentsRepository.AllWithIncludes(t => t.Participations, t => t.Organizer, t => t.Sport)
                .Where(t => t.Participations.Any(p => p.ParticipantId == currentUser));

            // Apply approval status filter
            if (filter.Approved.HasValue)
            {
                bool isApproved = filter.Approved.Value;
                query = query.Where(t => t.Participations.Any(p => p.ParticipantId == currentUser && p.Approved == isApproved));
            }

            // Apply other filters
            if (filter.SportId != null)
            {
                query = query.Where(p => p.SportId == filter.SportId.Value);
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

            // Execute query before ViewModel assignment
            var filteredTournaments = query.ToList();

            ViewBag.Sports = new SelectList(_sportRepository.GetAll(), "Id", "Name");
            ViewBag.Countries = await GetAllCountries();

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
                Tournaments = _tournamentsRepository.AllWithIncludes(t => t.Participations, t => t.Organizer, t => t.Sport)
                .Where(t => t.Participations.Any(p => p.ParticipantId == currentUser)).ToList(),
            };

            ViewBag.ReturnUrl = returnUrl ?? Url.Action("UserDetails", "User", new { id = id });
            return View(model);
        }

        [Authorize(Roles = $"{SD.AdminRole},{SD.UserRole}")]
        public async Task<IActionResult> AddParticipation(int id, string returnUrl = null)
        {
            var participation = new Participation
            {
                ParticipantId = _userManager.GetUserAsync(this.User).Result.Id,
                TournamentId = id,
                Approved = false,
            };
            _participationsRepository.Add(participation);
            return Redirect(returnUrl);
        }

        [Authorize(Roles = $"{SD.AdminRole},{SD.UserRole}")]
        public async Task<IActionResult> DeleteParticipation(int id, string returnUrl = null)
        {
            var currentUser = _userManager.GetUserAsync(this.User).Result.Id;
            ViewBag.UserId = currentUser;
            var participation = _participationsRepository.GetAll().FirstOrDefault(x => x.ParticipantId == currentUser && x.TournamentId == id);
            var tournament = _tournamentsRepository.GetById(id);
            tournament.Participations.ToList().Remove(participation);
            _participationsRepository.Delete(participation);
            return Redirect(returnUrl);
        }

        [Authorize(Roles = $"{SD.AdminRole}")]
        public async Task<IActionResult> ApproveParticipation(int tournamentId, string userId, string returnUrl = null)
        {
            var participation = _participationsRepository.GetAll().FirstOrDefault(x => x.ParticipantId == userId && x.TournamentId == tournamentId);
            participation.Approved = true;
            _participationsRepository.Update(participation);
            return Redirect(returnUrl);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
