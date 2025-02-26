using SportConnect.Models;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Drawing;

namespace SportConnect.Web.Models
{
    public class SportConnectUserEditViewModel
    {
        public string? Id { get; set; }
        [Required(ErrorMessage = "��������������� ��� � ������������")]
        public string UserName { get; set; }
        [Required(ErrorMessage = "��������� � ������������")]
        public string LastName { get; set; }
        [Required(ErrorMessage = "����������� ��� � ������������")]
        public string FirstName { get; set; }
        public string? BothNames { get; set; }
        [Required(ErrorMessage = "��������� � ������������")]
        public int Age { get; set; }
        [Required(ErrorMessage = "��������� � ������������")]
        public string Location { get; set; }
        [Required(ErrorMessage = "������� � ������������")]
        public string Email { get; set; }
        [Required(ErrorMessage = "����������� ����� � ������������")]
        public string PhoneNumber { get; set; }
        public string? PasswordHash { get; set; }
        public IEnumerable<Participation>? Participations { get; set; } = new List<Participation>();

        public SportConnectUser ToUser(string id)
        {
            return new SportConnectUser
            {
                Id = id,
                UserName = UserName,
                FullName = $"{FirstName} {LastName}",
                Age = Age,
                Location = Location,
                Email = Email,
                PhoneNumber = PhoneNumber,
                PasswordHash = PasswordHash
            };
        }
    }
}
