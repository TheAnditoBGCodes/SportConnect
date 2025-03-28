using Microsoft.AspNetCore.Identity;

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