using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using SportConnect.DataAccess.Repository.IRepository;
using SportConnect.Models;
using SportConnect.Web.Models;
using System.Diagnostics;

namespace SportConnect.Web.Controllers
{
    public class SportController : Controller
    {
        private readonly ILogger<SportController> _logger;
        public IRepository<Sport> _repository { get; set; }

        public SportController(ILogger<SportController> logger, IRepository<Sport> repository)
        {
            _logger = logger;
            this._repository = repository;
        }

        public IActionResult AddSport()
        {
            ViewBag.Sports = new SelectList(_repository.GetAll(), "Id", "Name");
            return View();
        }

        [HttpPost]
        public IActionResult AddSport(Sport sport)
        {
            _repository.Add(sport);
            return RedirectToAction("AllSports");
        }

        public IActionResult EditSport(int id)
        {
            ViewBag.Sports = new SelectList(_repository.GetAll(), "Id", "Name");
            var entity = _repository.GetById(id);
            return View(entity);
        }

        [HttpPost]
        public IActionResult EditSport(Sport sport)
        {
            _repository.Update(sport);
            return RedirectToAction("AllSports");
        }

        public IActionResult AllSports()
        {
            return View(_repository.GetAll().ToList());
        }

        public IActionResult DeleteSport(int id)
        {
            var entity = _repository.GetById(id);
            _repository.Delete(entity);
            return RedirectToAction("AllSports");
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
