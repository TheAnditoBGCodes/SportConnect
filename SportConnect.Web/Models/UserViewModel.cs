using SportConnect.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace SportConnect.Web.Models
{
    public class UserViewModel
    {
        public List<SportConnectUser>? Users { get; set; }
        public List<SportConnectUser>? FilteredUsers { get; set; }
        public string? Id { get; set; }
        public int? TournamentId { get; set; }
        public string? Email { get; set; }
        public string? BothNames { get; set; }
        public string? UserName { get; set; }
        public int? BirthYear { get; set; }
        public string? FirstName { get; set; }
        public bool? Approved { get; set; }
        public string? LastName { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string? Country { get; set; }
        public IEnumerable<SelectListItem>? CountryList { get; set; } = new List<SelectListItem>();
        public string? PhoneNumber { get; set; }
        public string? ProfileImage { get; set; }
        public string? PasswordHash { get; set; }
        public IEnumerable<Participation>? Participations { get; set; } = new List<Participation>();
    }
}
