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
            var tournament = _tournamentsRepository.GetAll().FirstOrDefault(x  => x.Id == id);
            var user = this.User;
            var currentUser = _userManager.GetUserAsync(user).Result;
            var participation = new Participation();
            participation.Participant = currentUser;
            participation.ParticipantId = currentUser.Id;
            participation.RegistrationDate = DateTime.Now;
            participation.Tournament = tournament;
            participation.TournamentId = id;
            _participationsRepository.Add(participation);
            return RedirectToAction("AllTournaments", "Tournament");
        }

        public IActionResult AllParticipations()
        {
            var model = _participationsRepository.AllWithIncludes(x => x.Participant, x => x.Tournament)
                .Select(x => new ParticipationViewModel
                {
                    Id = x.Id,
                    TournamentName = x.Tournament.Name,
                    RegistrationDate = x.RegistrationDate,
                    ParticipantName = x.Participant.FullName,
                    TournamentDate = x.Tournament.Date
                });
            return View(model.ToList());
        }

        public IActionResult DeleteParticipation(int id)
        {
            var participation = _participationsRepository.GetAll().FirstOrDefault(x => x.Id == id);
            _participationsRepository.Delete(participation);
            return RedirectToAction("AllParticipations");
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
