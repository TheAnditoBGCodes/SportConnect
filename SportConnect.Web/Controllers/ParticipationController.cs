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
    public class ParticipationController : Controller
    {
        private readonly ILogger<ParticipationController> _logger;
        private readonly UserManager<SportConnectUser> _userManager;
        public IRepository<Participation> _participationsRepository { get; set; }
        public IRepository<Tournament> _tournamentsRepository { get; set; }

        public ParticipationController(ILogger<ParticipationController> logger, UserManager<SportConnectUser> userManager, IRepository<Participation> participationsRepository, IRepository<Tournament> tournamentsRepository)
        {
            _logger = logger;
            _userManager = userManager;
            _participationsRepository = participationsRepository;
            _tournamentsRepository = tournamentsRepository;
        }

        public IActionResult AddParticipation(int id)
        {
            var currentTournament = _tournamentsRepository.GetAll().FirstOrDefault(x => x.Id == id);
            var currentUser = _userManager.GetUserAsync(this.User).Result;

            if (currentTournament == null || currentUser == null)
            {
                return NotFound("Invalid tournament or user.");
            }

            var currentUserId = currentUser.Id;

            var existingParticipation = _participationsRepository
                .GetAllBy(p => p.TournamentId == id && p.ParticipantId == currentUserId)
                .FirstOrDefault();

            if (existingParticipation != null)
            {
                return RedirectToAction("AllTournaments", "Tournament");
            }

            var participation = new Participation
            {
                Participant = currentUser,
                ParticipantId = currentUser.Id,
                RegistrationDate = DateTime.Now,
                Tournament = currentTournament,
                TournamentId = id
            };

            _participationsRepository.Add(participation);
            return RedirectToAction("AllTournaments", "Tournament");
        }

        public IActionResult AllParticipations()
        {
            var model = _participationsRepository.AllWithIncludes(x => x.Participant, x => x.Tournament, p => p.Tournament.Sport)
                .Select(x => new ParticipationViewModel
                {
                    Id = x.Id,
                    TournamentName = x.Tournament.Name,
                    RegistrationDate = x.RegistrationDate,
                    ParticipantName = x.Participant.FullName,
                    TournamentDate = x.Tournament.Date,
                    TournamentSport = x.Tournament.Sport.Name,
                    TournamentDeadLine = x.Tournament.Deadline
                });
            return View(model.ToList());
        }

        public IActionResult ThisUserParticipations()
        {
            string userId = _userManager.GetUserAsync(this.User).Result.Id;
            var participations = _participationsRepository.AllWithIncludes(p => p.Tournament, p => p.Tournament.Sport).Where(p => p.ParticipantId == userId).ToList();
            var model = participations.Select(x => new ParticipationViewModel
            {
                Id = x.Id,
                TournamentName = x.Tournament.Name,
                RegistrationDate = x.RegistrationDate,
                ParticipantName = x.Participant.FullName,
                TournamentDate = x.Tournament.Date,
                TournamentSport = x.Tournament.Sport.Name,
                TournamentDeadLine = x.Tournament.Deadline
            });
            return View(model.ToList());
        }

        public IActionResult DeleteParticipationUser(int id)
        {
            var participation = _participationsRepository.GetById(id);
            _participationsRepository.Delete(participation);
            return RedirectToAction("ThisUserParticipations");
        }
        public IActionResult DeleteParticipationTournamentDetails(int id)
        {
            var participation = _participationsRepository.GetById(id);
            if (participation == null)
            {
                return NotFound();
            }

            _participationsRepository.Delete(participation);

            return RedirectToAction("TournamentDetails", "Tournament", new { id = participation.TournamentId });
        }

        public IActionResult DeleteParticipationAll(int id)
        {
            var participation = _participationsRepository.GetById(id);
            _participationsRepository.Delete(participation);
            return RedirectToAction("AllParticipations");
        }

        public IActionResult DeleteParticipationTournament(int id)
        {
            var participation = _participationsRepository.GetAll().FirstOrDefault(x => x.TournamentId == id);
            _participationsRepository.Delete(participation);
            return RedirectToAction("AllTournaments", "Tournament");
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
