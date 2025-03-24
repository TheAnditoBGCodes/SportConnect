using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SportConnect.Utility;

namespace SportConnect.Models
{
    public class Tournament
    {
        [Key]
        public int Id { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        [ForeignKey(nameof(Organizer))]
        public string OrganizerId { get; set; }
        public SportConnectUser Organizer { get; set; }

        public DateTime Date { get; set; }

        public DateTime Deadline { get; set; }

        public string Country { get; set; }
        public string ImageUrl { get; set; }

        [ForeignKey(nameof(Sport))]
        public int SportId { get; set; }
        public Sport Sport { get; set; }

        public IEnumerable<Participation> Participations { get; set; } = new List<Participation>();
    }
}