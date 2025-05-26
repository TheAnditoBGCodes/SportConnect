using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SportConnect.DataAccess.Repository.IRepository;
using SportConnect.Models;
using SportConnect.Services.Participation;
using SportConnect.Services.Sport;
using SportConnect.Services.Tournament;
using SportConnect.Services.User;
using SportConnect.Utility;
using SportConnect.Web.Models;
using System.Diagnostics;

namespace SportConnect.Web.Controllers
{
    public class SportController : Controller
    {
        public ITournamentService _tournamentService;
        public ISportService _sportService;
        public IParticipationService _participationService;

        public SportController(ITournamentService tournamentService, ISportService sportService, IParticipationService participationService)
        {
            _tournamentService = tournamentService;
            _sportService = sportService;
            _participationService = participationService;
        }

        [AllowAnonymous]
        public async Task<IActionResult> AllSports(SportViewModel? filter)
        {
            var allSports = await _sportService.GetAll(); if (filter == null)
            {
                return View(new SportViewModel
                {
                    Sports = allSports.ToList(),
                    FilteredSports = allSports.ToList()
                });
            }

            var query = allSports.AsQueryable();
            if (!string.IsNullOrEmpty(filter.Name))
            {
                string trimmedFilter = filter.Name.Trim().ToLower();
                query = query.Where(p => p.Name.Trim().ToLower().Contains(trimmedFilter));
            }

            var model = new SportViewModel
            {
                Name = filter.Name,
                Sports = allSports.ToList(),
                FilteredSports = query.ToList(),
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

            if ((await _sportService.GetAll()).Any(s => s.Name == model.Name))
            {
                ModelState.AddModelError("Name", "Името е използвано от друг спорт.");
                ModelState.AddModelError("ImageUrl", "Снимката е задължителна.");
            }

            if ((await _sportService.GetAll()).Any(s => s.Description == model.Description))
            {
                ModelState.AddModelError("Description", "Описанието е използвано от друг спорт.");
                ModelState.AddModelError("ImageUrl", "Снимката е задължителна.");
            }

            if (ModelState.IsValid)
            {
                await _sportService.Add(await model.ToSport());
                return RedirectToAction("AllSports");
            }

            return View(model);
        }

        [Authorize(Roles = $"{SD.AdminRole}")]
        public async Task<IActionResult> EditSport(string id)
        {
            var sport = await _sportService.GetById(id);
            if (sport == null)
            {
                return View("~/Views/Shared/NotFound.cshtml");
            }

            var entity = await _sportService.GetById(id);
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

            if ((await _sportService.GetAll()).Any(s => s.Description == sport.Description && s.Id != sport.Id))
            {
                ModelState.AddModelError("Description", "Описанието е използвано от друг спорт.");
            }

            if (((await _sportService.GetAll()).Any(s => s.Name == sport.Name && s.Id != sport.Id)))
            {
                ModelState.AddModelError("Name", "Името е използвано от друг спорт.");
            }

            if (ModelState.IsValid)
            {
                var dbSport = (await _sportService.GetById(sport.Id));

                dbSport.Id = sport.Id;
                dbSport.Name = sport.Name;
                dbSport.Description = sport.Description;
                dbSport.ImageUrl = sport.ImageUrl;

                await _sportService.Update(dbSport);
                return RedirectToAction("AllSports");
            }
            return View(sport);
        }

        [Authorize(Roles = $"{SD.AdminRole}")]
        public async Task<IActionResult> DeleteSport(string id)
        {
            var sport = await _sportService.GetById(id);
            if (sport == null)
            {
                return View("~/Views/Shared/NotFound.cshtml");
            }

            var model = new SportViewModel()
            {
                Name = sport.Name,
                Description = sport.Description,
                ImageUrl = sport.ImageUrl,
            };
            return View(model);
        }

        [HttpPost]
        [Authorize(Roles = $"{SD.AdminRole}")]
        public async Task<IActionResult> DeleteSport(string id, string ConfirmText, SportViewModel model)
        {
            var sport = await _sportService.GetById(id);
            if (sport == null)
            {
                return View("~/Views/Shared/NotFound.cshtml");
            }

            if (ConfirmText == "ПОТВЪРДИ")
            {
                var tournaments = (await _tournamentService.GetAllBy(t => t.SportId == id)).ToList();

                foreach (var tournament in tournaments)
                {
                    var participations = (await _participationService.GetAllBy(p => p.TournamentId == tournament.Id)).ToList();
                    await _participationService.DeleteRange(participations);
                }

                await _tournamentService.DeleteRange(tournaments);
                await _sportService.Delete(sport);
                return RedirectToAction("AllSports");
            }
            var sport1 = await _sportService.GetById(id);
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
