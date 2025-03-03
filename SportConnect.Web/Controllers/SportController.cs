
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

namespace SportConnect.Web.Controllers
{
    public class SportController : Controller
    {
        private readonly ILogger<SportController> _logger;
        public IRepository<Sport> _repository { get; set; }
        public IRepository<Tournament> _tournamentRepository { get; set; }
        public IRepository<Participation> _participationsRepository { get; set; }
        private readonly CloudinaryService _cloudinaryService;

        public SportController(ILogger<SportController> logger, IRepository<Sport> repository, IRepository<Tournament> tournamentRepository, IRepository<Participation> participationsRepository, CloudinaryService cloudinaryService)
        {
            _logger = logger;
            _repository = repository;
            _tournamentRepository = tournamentRepository;
            _participationsRepository = participationsRepository;
            _cloudinaryService = cloudinaryService;
        }

        [HttpGet]
        [Authorize(Roles = $"{SD.AdminRole}")]
        public IActionResult AddSport()
        {
            return View();
        }
        [Authorize(Roles = $"{SD.AdminRole}")]
        [HttpPost]
        public async Task<IActionResult> AddSport(Sport sport, IFormFile file)
        {
            if (_repository.GetAll().Any(s => s.Name == sport.Name))
            {
                ModelState.AddModelError("Name", "��� ����� �����.");
            }
            if (_repository.GetAll().Any(s => s.Description == sport.Description))
            {
                ModelState.AddModelError("Description", "���� �������� � ���������� �� ���� �����.");
            }

            if (file == null || file.Length == 0)
            {
                ModelState.AddModelError("ImageUrl", "�������� � ������������.");
            }
            else
            {
                var imageUrl = await _cloudinaryService.UploadImageAsync(file);

                if (string.IsNullOrEmpty(imageUrl))
                {
                    ModelState.AddModelError("ImageUrl", "������ ��� ���������.");
                    return View(sport);
                }

                sport.ImageUrl = imageUrl;
            }

            if (ModelState.IsValid)
            {
                _repository.Add(sport);
                return RedirectToAction("AllSports");
            }

            return View(sport);
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
            if (sport.Name == null)
            {
                ModelState.AddModelError("Name", "����� � ������������.");
            }

            if (sport.Description == null)
            {
                ModelState.AddModelError("Description", "����������  � ������������.");
            }

            if (!_repository.IsPropertyUnique(s => s.Name == sport.Name && s.Id != sport.Id))
            {
                ModelState.AddModelError("Name", "��� ����� �����.");
            }

            if (!_repository.IsPropertyUnique(s => s.Description == sport.Description && s.Id != sport.Id))
            {
                ModelState.AddModelError("Description", "���� �������� � ���������� �� ���� �����.");
            }

            // Check if a new image is uploaded
            if (file != null && file.Length > 0)
            {
                var imageUrl = await _cloudinaryService.UploadImageAsync(file);

                if (string.IsNullOrEmpty(imageUrl))
                {
                    ModelState.AddModelError("ImageUrl", "������ ��� ���������.");
                    return View(sport);
                }

                sport.ImageUrl = imageUrl;  // Set the new image URL if a new image is uploaded
            }
            else
            {
                // If no new image is uploaded, keep the existing image URL
                var existingSport = _repository.GetById(sport.Id);
                if (existingSport != null)
                {
                    // Keep the existing image URL if available
                    sport.ImageUrl = existingSport.ImageUrl;
                }
            }

            if (ModelState.IsValid)
            {
                // Reload the entity from the database to avoid tracking multiple instances
                var dbSport = _repository.GetById(sport.Id);
                if (dbSport != null)
                {
                    // Copy the values from the form submission (this avoids tracking conflicts)
                    dbSport.Id = sport.Id;
                    dbSport.Name = sport.Name;
                    dbSport.Description = sport.Description;
                    dbSport.ImageUrl = sport.ImageUrl;

                    _repository.Update(dbSport);
                    return RedirectToAction("AllSports");
                }
            }
            return View(sport);
        }

        [AllowAnonymous]
        public IActionResult AllSports()
        {
            return View(_repository.GetAll().ToList());
        }

        [Authorize(Roles = $"{SD.AdminRole}")]
        public IActionResult DeleteSport(int id)
        {
            var sport = _repository.GetById(id);
            var model = new SportDeletionViewModel()
            {
                Name = sport.Name,
                Description = sport.Description,
                ImageUrl= sport.ImageUrl,
            };
            return View(model);
        }

        [HttpPost]
        [Authorize(Roles = $"{SD.AdminRole}")]
        public IActionResult DeleteSport(int id, string ConfirmText, SportDeletionViewModel model)
        {
            if (ConfirmText == "��������")
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
            var model1 = new SportDeletionViewModel()
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
