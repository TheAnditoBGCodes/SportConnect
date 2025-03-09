using Microsoft.AspNetCore.Identity;
using SportConnect.Models;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SportConnect.Models
{
    public class SportConnectUser : IdentityUser
    {
        [Required(ErrorMessage = "Моля, въведете пълното си име.")]
        [Display(Name = "Пълно име")]
        public string FullName { get; set; }

        [Required(ErrorMessage = "Моля, въведете своята дата на раждане.")]
        [DataType(DataType.Date)]
        [Display(Name = "Дата на раждане")]
        public DateTime DateOfBirth { get; set; }

        [Required(ErrorMessage = "Моля, въведете своята държава.")]
        [Display(Name = "Държава")]
        public string? Country { get; set; }

        public string? ImageUrl { get; set; }

        public ICollection<Participation> Participations { get; set; } = new List<Participation>();
        public ICollection<Tournament> OrganizedTournaments { get; set; } = new List<Tournament>();
    }
}