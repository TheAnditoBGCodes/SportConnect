using SportConnect.Models;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Drawing;

namespace SportConnect.Web.Models
{
    public class SportConnectUserViewModel
    {
        public string Id { get; set; }
        public string UserName { get; set; }
        public string LastName { get; set; }
        public string FirstName { get; set; }
        public string BothNames { get; set; }
        public DateTime DateOfBirth { get; set; }
        public string Country { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public string? PasswordHash { get; set; }
        public IEnumerable<Participation> Participations { get; set; } = new List<Participation>();

        public SportConnectUser ToUser(string id)
        {
            return new SportConnectUser
            {
                Id = id,
                UserName = UserName,
                FullName = $"{FirstName} {LastName}",
                DateOfBirth = DateOfBirth,
                Country = Country,
                Email = Email,
                PhoneNumber = PhoneNumber,
                PasswordHash = PasswordHash
            };
        }
    }
}
