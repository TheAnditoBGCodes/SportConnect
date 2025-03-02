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

        [Required(ErrorMessage = "Потребителското име е задължително")]
        [StringLength(100, MinimumLength = 5, ErrorMessage = "Потребителското име трябва да е между 5 и 100 символа")]
        public string UserName { get; set; }

        [Required(ErrorMessage = "Фамилията е задължителна")]
        [StringLength(100, MinimumLength = 5, ErrorMessage = "Фамилията трябва да е между 5 и 100 символа")]
        public string LastName { get; set; }

        [Required(ErrorMessage = "Собственото име е задължително")]
        [StringLength(100, MinimumLength = 5, ErrorMessage = "Собственото име трябва да е между 5 и 100 символа")]
        public string FirstName { get; set; }

        public string? BothNames { get; set; }

        [Required(ErrorMessage = "Локацията е задължителна")]
        [StringLength(100, MinimumLength = 5, ErrorMessage = "Локацията трябва да е между 5 и 100 символа")]
        public string Location { get; set; }

        [Required(ErrorMessage = "Имейлът е задължителен")]
        [DataType(DataType.EmailAddress)]
        public string Email { get; set; }

        [Required(ErrorMessage = "Телефонният номер е задължителен")]
        [DataType(DataType.PhoneNumber)]
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
                Location = Location,
                Email = Email,
                PhoneNumber = PhoneNumber,
                PasswordHash = PasswordHash
            };
        }
    }
}
