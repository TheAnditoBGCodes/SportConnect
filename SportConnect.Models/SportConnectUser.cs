using Microsoft.AspNetCore.Identity;
using SportConnect.Models;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SportConnect.Models
{
    public class SportConnectUser : IdentityUser
    {
        public string FullName { get; set; }

        public DateTime DateOfBirth { get; set; }

        public string Country { get; set; }

        public string ImageUrl { get; set; }

        public ICollection<Participation> Participations { get; set; } = new List<Participation>();
        public ICollection<Tournament> OrganizedTournaments { get; set; } = new List<Tournament>();
    }
}