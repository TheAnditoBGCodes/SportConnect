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
            var tournament = new TournamentViewModel
            {
                Sports = _sportRepository.GetAll().Select(x => new SelectListItem
                {
                    Text = x.Name,
                    Value = x.Id.ToString()
                }).ToList()
            };
            return View(tournament);
        }

        [HttpPost]
        public IActionResult AddTournament(TournamentViewModel tournament)
        {
            if (!ModelState.IsValid)
            {
                var user = this.User;
                var currentUser = _userManager.GetUserAsync(user).Result;
                if (currentUser != null)
                {
                    var tour = tournament.ToTournament();
                    tour.Organizer = currentUser;
                    tour.OrganizerId = currentUser.Id;
                    _repository.Add(tour);
                    return RedirectToAction("AllTournaments");
                }
                ModelState.AddModelError("", "Could not determine the current user.");
            }
            else
            {
                tournament = new TournamentViewModel
                {
                    Sports = _sportRepository.GetAll().Select(x => new SelectListItem
                    {
                        Text = x.Name,
                        Value = x.Id.ToString()
                    }).ToList()
                };
            }
            return View(tournament);
        }

        public IActionResult EditTournament(int id)
        {
            var entity = _repository.GetById(id);
            var entitymodel = new TournamentViewModel
            {
                Name = entity.Name,
                Description = entity.Description,
                Date = entity.Date,
                Deadline = entity.Deadline,
                Location = entity.Location,
                Sports = _sportRepository.GetAll().Select(x => new SelectListItem
                {
                    Text = x.Name,
                    Value = x.Id.ToString()
                }).ToList(),
                SportName = entity.Sport.Name,
                OrganizerId = entity.OrganizerId,
            };
            return View(entitymodel);
        }

        [HttpPost]
        public IActionResult EditTournament(int id, TournamentViewModel tournament)
        {
            if (!ModelState.IsValid)
            {
                //organizer shit id stuff
                Tournament tour = tournament.ToTournament(id);
                _repository.Update(tour);
                return RedirectToAction("AllTournaments");
            }
            else
            {
                tournament = new TournamentViewModel
                {
                    Sports = _sportRepository.GetAll().Select(x => new SelectListItem
                    {
                        Text = x.Name,
                        Value = x.Id.ToString()
                    }).ToList()
                };
            }
            return View(tournament);
        }

        public IActionResult AllTournaments()
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
                    SportName = x.Sport.Name
                });
            return View(model.ToList());
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
