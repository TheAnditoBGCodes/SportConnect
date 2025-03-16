using CloudinaryDotNet.Actions;
using CloudinaryDotNet;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Scaffolding;
using SportConnect.DataAccess.Repository.IRepository;
using SportConnect.Models;
using SportConnect.Services;
using SportConnect.Utility;
using SportConnect.Web.Models;
using System.Diagnostics;
using System.Linq;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace SportConnect.Web.Controllers
{
    public class SportController : Controller
    {
        private readonly ILogger<SportController> _logger;
        public IRepository<Sport> _repository { get; set; }
        public IRepository<Tournament> _tournamentRepository { get; set; }
        public IRepository<Participation> _participationsRepository { get; set; }
        private readonly CloudinaryService _cloudinaryService;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public SportController(ILogger<SportController> logger, IRepository<Sport> repository, IRepository<Tournament> tournamentRepository, IRepository<Participation> participationsRepository, CloudinaryService cloudinaryService, IWebHostEnvironment webHostEnvironment)
        {
            _logger = logger;
            _repository = repository;
            _tournamentRepository = tournamentRepository;
            _participationsRepository = participationsRepository;
            _cloudinaryService = cloudinaryService;
            _webHostEnvironment = webHostEnvironment;
        }

        [AllowAnonymous]
        public async Task<IActionResult> AllSports(SportViewModel? filter)
        {
            if (filter == null)
            {
                return View(new SportViewModel());
            }

            var query = _repository.GetAll().AsQueryable();
            if (!string.IsNullOrEmpty(filter.Name))
            {
                string trimmedFilter = filter.Name.Trim().ToLower();
                query = query.Where(p => p.Name.Trim().ToLower().Contains(trimmedFilter));
            }

            var model = new SportViewModel
            {
                Name = filter.Name,
                Sports = _repository.GetAll().ToList(),
                FilteredSports = query.ToList(),
            };

            return View(model);
        }

        [Authorize(Roles = $"{SD.AdminRole}")]
        public IActionResult AddSport()
        {
            return View(new Sport());
        }

        [Authorize(Roles = $"{SD.AdminRole}")]
        [HttpPost]
        public async Task<IActionResult> AddSport(Sport model)
        {
            if (string.IsNullOrEmpty(model.Name))
            {
                ModelState.AddModelError("ImageUrl", "Снимката е задължителна.");
            }

            if (string.IsNullOrEmpty(model.Description))
            {
                ModelState.AddModelError("ImageUrl", "Снимката е задължителна.");
            }

            if (_repository.GetAll().Any(s => s.Name == model.Name))
            {
                ModelState.AddModelError("Name", "Името е използвано от друг спорт.");
                ModelState.AddModelError("ImageUrl", "Снимката е задължителна.");
            }

            if (_repository.GetAll().Any(s => s.Description == model.Description))
            {
                ModelState.AddModelError("Description", "Описанието е използвано от друг спорт.");
                ModelState.AddModelError("ImageUrl", "Снимката е задължителна.");
            }

            if (ModelState.IsValid)
            {
                _repository.Add(model);
                return RedirectToAction("AllSports");
            }

            return View(model);
        }

        [Authorize(Roles = $"{SD.AdminRole}")]
        public IActionResult EditSport(int id)
        {
            var entity = _repository.GetById(id);
            var model = new SportViewModel
            {
                Id = id,
                Name = entity.Name,
                Description = entity.Description,
                ImageUrl = entity.ImageUrl,
            };
            return View(model);
        }

        [Authorize(Roles = $"{SD.AdminRole}")]
        [HttpPost]
        public async Task<IActionResult> EditSport(SportViewModel sport, IFormFile? file)
        {
            if (string.IsNullOrEmpty(sport.Name))
            {
                ModelState.AddModelError("Name", "Името е задължително.");
            }

            if (string.IsNullOrEmpty(sport.Description))
            {
                ModelState.AddModelError("Description", "Описанието е задължително.");
            }

            if (_repository.GetAll().Any(s => s.Description == sport.Description && s.Id != sport.Id))
            {
                ModelState.AddModelError("Name", "Името е използвано от друг спорт.");
            }

            if ((_repository.GetAll().Any(s => s.Name == sport.Name && s.Id != sport.Id)))
            {
                ModelState.AddModelError("Description", "Описанието е използвано от друг спорт.");
            }

            if (ModelState.IsValid)
            {
                // Reload the entity from the database to avoid tracking multiple instances
                var dbSport = _repository.GetById((int)sport.Id);
                if (dbSport != null)
                {
                    // Copy the values from the form submission (this avoids tracking conflicts)
                    dbSport.Id = (int)sport.Id;
                    dbSport.Name = sport.Name;
                    dbSport.Description = sport.Description;
                    dbSport.ImageUrl = sport.ImageUrl;

                    _repository.Update(dbSport);
                    return RedirectToAction("AllSports");
                }
                return View(sport);
            }

            return View(sport);
        }

        [Authorize(Roles = $"{SD.AdminRole}")]
        public IActionResult DeleteSport(int id)
        {
            var sport = _repository.GetById(id);
            var model = new SportViewModel()
            {
                Name = sport.Name,
                Description = sport.Description,
                ImageUrl= sport.ImageUrl,
            };
            return View(model);
        }

        [HttpPost]
        [Authorize(Roles = $"{SD.AdminRole}")]
        public IActionResult DeleteSport(int id, string ConfirmText, SportViewModel model)
        {
            if (ConfirmText == "ПОТВЪРДИ")
            {
                var tournaments = _tournamentRepository.GetAllBy(t => t.SportId == id).ToList();

                foreach (var tournament in tournaments)
                {
                    var participations = _participationsRepository.GetAllBy(p => p.TournamentId == tournament.Id).ToList();
                    _participationsRepository.DeleteRange(participations);
                }

                _tournamentRepository.DeleteRange(tournaments);

                var sport = _repository.GetById(id);
                if (sport != null)
                {
                    _repository.Delete(sport);
                }

                return RedirectToAction("AllSports");
            }
            var sport1 = _repository.GetById(id);
            var model1 = new SportViewModel()
            {
                Name = sport1.Name,
                Description = sport1.Description,
                ImageUrl = sport1.ImageUrl,
            };
            return View(model1);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
