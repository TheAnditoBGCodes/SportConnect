using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SportConnect.DataAccess.Repository.IRepository;
using SportConnect.Models;
using SportConnect.Utility;
using SportConnect.Web.Models;
using System.Diagnostics;

namespace SportConnect.Web.Controllers
{
    public class SportController : Controller
    {
        public IRepository<Sport> _sportRepository { get; set; }
        public IRepository<Tournament> _tournamentRepository { get; set; }
        public IRepository<Participation> _participationRepository { get; set; }

        public SportController(IRepository<Sport> sportRepository, IRepository<Tournament> tournamentRepository, IRepository<Participation> participationRepository)
        {
            _sportRepository = sportRepository;
            _tournamentRepository = tournamentRepository;
            _participationRepository = participationRepository;
        }

        [AllowAnonymous]
        public async Task<IActionResult> AllSports(SportViewModel? filter)
        {
            if (filter == null)
            {
                return View(new SportViewModel());
            }

            var allSports = await _sportRepository.GetAll(); // Fetch all sports asynchronously
            var query = allSports.AsQueryable(); // Work with an IQueryable for filtering

            if (!string.IsNullOrEmpty(filter.Name))
            {
                string trimmedFilter = filter.Name.Trim().ToLower();
                query = query.Where(p => p.Name.Trim().ToLower().Contains(trimmedFilter)); // Apply filtering
            }

            var model = new SportViewModel
            {
                Name = filter.Name,
                Sports = allSports.ToList(), // All sports, unfiltered
                FilteredSports = query.ToList(), // Apply filters and return results asynchronously
            };

            return View(model);
        }

        [Authorize(Roles = $"{SD.AdminRole}")]
        public async Task<IActionResult> AddSport()
        {
            return View(new SportViewModel());
        }

        [Authorize(Roles = $"{SD.AdminRole}")]
        [HttpPost]
        public async Task<IActionResult> AddSport(SportViewModel model)
        {
            if (string.IsNullOrEmpty(model.Name))
            {
                ModelState.AddModelError("Name", "Името е задължително.");
                ModelState.AddModelError("ImageUrl", "Снимката е задължителна.");
            }
            else if (model.Name.Length < 5 || model.Name.Length > 100)
            {
                ModelState.AddModelError("Name", "Tрябва да е от 5 до 100 символа.");
                ModelState.AddModelError("ImageUrl", "Снимката е задължителна.");
            }


            if (string.IsNullOrEmpty(model.Description))
            {
                ModelState.AddModelError("Description", "Описанието е задължително.");
                ModelState.AddModelError("ImageUrl", "Снимката е задължителна.");
            }
            else if (model.Description.Length < 5 || model.Description.Length > 100)
            {
                ModelState.AddModelError("Description", "Tрябва да е от 5 до 100 символа.");
                ModelState.AddModelError("ImageUrl", "Снимката е задължителна.");
            }

            if ((await _sportRepository.GetAll()).Any(s => s.Name == model.Name))
            {
                ModelState.AddModelError("Name", "Името е използвано от друг спорт.");
                ModelState.AddModelError("ImageUrl", "Снимката е задължителна.");
            }

            if ((await _sportRepository.GetAll()).Any(s => s.Description == model.Description))
            {
                ModelState.AddModelError("Description", "Описанието е използвано от друг спорт.");
                ModelState.AddModelError("ImageUrl", "Снимката е задължителна.");
            }

            if (ModelState.IsValid)
            {
                await _sportRepository.Add(await model.ToSport());
                return RedirectToAction("AllSports");
            }

            return View(model);
        }

        [Authorize(Roles = $"{SD.AdminRole}")]
        public async Task<IActionResult> EditSport(string id)
        {
            var entity = await _sportRepository.GetById(id);
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
            else if (sport.Name.Length < 5 || sport.Name.Length > 100)
            {
                ModelState.AddModelError("Name", "Tрябва да е от 5 до 100 символа.");
            }


            if (string.IsNullOrEmpty(sport.Description))
            {
                ModelState.AddModelError("Description", "Описанието е задължително.");
            }
            else if (sport.Description.Length < 5 || sport.Description.Length > 100)
            {
                ModelState.AddModelError("Description", "Tрябва да е от 5 до 100 символа.");
            }

            if ((await _sportRepository.GetAll()).Any(s => s.Description == sport.Description && s.Id != sport.Id))
            {
                ModelState.AddModelError("Name", "Името е използвано от друг спорт.");
            }

            if (((await _sportRepository.GetAll()).Any(s => s.Name == sport.Name && s.Id != sport.Id)))
            {
                ModelState.AddModelError("Description", "Описанието е използвано от друг спорт.");
            }

            if (ModelState.IsValid)
            {
                // Reload the entity from the database to avoid tracking multiple instances
                var dbSport = (await _sportRepository.GetById(sport.Id));

                // Copy the values from the form submission (this avoids tracking conflicts)
                dbSport.Id = sport.Id;
                dbSport.Name = sport.Name;
                dbSport.Description = sport.Description;
                dbSport.ImageUrl = sport.ImageUrl;

                await _sportRepository.Update(dbSport);
                return RedirectToAction("AllSports");
            }
            return View(sport);
        }

        [Authorize(Roles = $"{SD.AdminRole}")]
        public async Task<IActionResult> DeleteSport(string id)
        {
            var sport = await _sportRepository.GetById(id);
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
        public async Task<IActionResult> DeleteSport(string id, string ConfirmText, SportViewModel model)
        {
            if (ConfirmText == "ПОТВЪРДИ")
            {
                var tournaments = (await _tournamentRepository.GetAllBy(t => t.SportId == id)).ToList();

                foreach (var tournament in tournaments)
                {
                    var participations = (await _participationRepository.GetAllBy(p => p.TournamentId == tournament.Id)).ToList();
                    await _participationRepository.DeleteRange(participations);
                }

                await _tournamentRepository.DeleteRange(tournaments);

                var sport = await _sportRepository.GetById(id);
                await _sportRepository.Delete(sport);
                return RedirectToAction("AllSports");
            }
            var sport1 = await _sportRepository.GetById(id);
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
