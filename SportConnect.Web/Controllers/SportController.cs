using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
            return View();
        }

        [HttpPost]
        public IActionResult AddSport(Sport category)
        {
            _repository.Add(category);
            return RedirectToAction("AllSports");
        }

        public IActionResult EditSport(int id)
        {
            var entity = _repository.GetById(id);
            return View(entity);
        }

        [HttpPost]
        public IActionResult EditSport(Sport category)
        {
            _repository.Update(category);
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
