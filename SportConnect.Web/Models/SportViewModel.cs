using SportConnect.Models;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Drawing;

namespace SportConnect.Web.Models
{
    public class SportViewModel
    {
        public List<Sport>? Sports { get; set; }
        public List<Sport>? FilteredSports { get; set; }
        public List<Tournament>? Tournaments { get; set; }
        public int? Id { get; set; }
        public string? Name { get; set; }

        public string? Description { get; set; }

        public string? ImageUrl { get; set; }

        public Sport ToSport()
        {
            return new Sport
            {
                Id = (int)Id,
                Name = Name,
                Description = Description,
                ImageUrl = ImageUrl
            };
        }

        public Sport ToSportAdd()
        {
            return new Sport
            {
                Name = Name,
                Description = Description,
                ImageUrl = ImageUrl
            };
        }
    }
}
