using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SportConnect.Models
{
    public class Tournament
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Name { get; set; }

        [Required]
        public string Description { get; set; }

        [ForeignKey(nameof(Organizer))]
        public string OrganizerId { get; set; }
        public SportConnectUser Organizer { get; set; }

        [Required]
        public DateTime Date { get; set; }

        [Required]
        public DateTime Deadline { get; set; }

        [Required]
        public string Location { get; set; }

        [Required]
        public bool IsActive { get; set; }

        [ForeignKey(nameof(Sport))]
        public int SportId { get; set; }
        public Sport Sport { get; set; }

        public IEnumerable<Participation> Participations { get; set; } = new List<Participation>();
    }
}