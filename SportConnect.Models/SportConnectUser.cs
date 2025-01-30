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

        [Required(ErrorMessage = "Моля, въведете възрастта си.")]
        [Range(13, 120, ErrorMessage = "Възрастта трябва да бъде между 13 и 120 години.")]
        [Display(Name = "Възраст")]
        public int Age { get; set; }

        [Required(ErrorMessage = "Моля, въведете местоположението си.")]
        [Display(Name = "Местоположение")]
        public string Location { get; set; }

        public ICollection<Participation> Participations { get; set; } = new List<Participation>();
        public ICollection<Tournament> OrganizedTournaments { get; set; } = new List<Tournament>();
    }
}