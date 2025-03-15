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

        [Required(ErrorMessage = "Името е задължително")]
        [StringLength(100, MinimumLength = 5, ErrorMessage = "Името трябва да е между 5 и 100 символа")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Описанието е задължително")]
        [StringLength(100, MinimumLength = 5, ErrorMessage = "Описанието трябва да е между 5 и 100 символа")]
        public string Description { get; set; }

        [Required]
        [ForeignKey(nameof(Organizer))]
        public string OrganizerId { get; set; }
        public SportConnectUser Organizer { get; set; }

        [Required(ErrorMessage = "Датата на прожеждане е задължителна")]
        public DateTime Date { get; set; }

        [Required(ErrorMessage = "Крайният срок е задължителен")]
        [DeadlineBeforeTournament("Date")]
        public DateTime Deadline { get; set; }

        public string? Country { get; set; }
        public string? ImageUrl { get; set; }

        [Required(ErrorMessage = "Спортът е задължителен")]
        [ForeignKey(nameof(Sport))]
        public int SportId { get; set; }
        public Sport Sport { get; set; }

        public IEnumerable<Participation> Participations { get; set; } = new List<Participation>();
    }
}