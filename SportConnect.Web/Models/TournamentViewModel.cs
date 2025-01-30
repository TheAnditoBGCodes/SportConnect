using SportConnect.Models;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Drawing;

namespace SportConnect.Web.Models
{
    public class TournamentViewModel
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        public string OrganizerId { get; set; }
        public string OrganizerName { get; set; }

        public DateTime Date { get; set; }

        public DateTime Deadline { get; set; }

        public string Location { get; set; }

        [Required]
        public int SportId { get; set; }
        public ICollection<SelectListItem> Sports { get; set; }
        public string SportName { get; set; }

        public Tournament ToTournament(int id)
        {
            return new Tournament
            {
                Id = id,
                Name = Name,
                Description = Description,
                Deadline = Deadline,
                Date = Date,
                Location = Location,
                SportId = SportId,
                OrganizerId = OrganizerId
            };
        }
        public Tournament ToTournament()
        {
            return new Tournament
            {
                Name = Name,
                Description = Description,
                Deadline = Deadline,
                Date = Date,
                Location = Location,
                SportId = SportId,
                OrganizerId = OrganizerId
            };
        }
    }
}