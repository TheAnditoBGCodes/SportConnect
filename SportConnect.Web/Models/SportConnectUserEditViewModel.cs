using SportConnect.Models;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Drawing;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace SportConnect.Web.Models
{
    public class SportConnectUserEditViewModel
    {
        public string? Id { get; set; }

        [Required(ErrorMessage = "Моля, въведете потребителско име.")]
        [StringLength(100, ErrorMessage = "Потребителското име трябва да е от {2} до {1} символа", MinimumLength = 5)]
        public string UserName { get; set; }

        [Required(ErrorMessage = "Моля, въведете фамилното си име.")]
        [StringLength(100, ErrorMessage = "Фамилията трябва да е от {2} до {1} символа", MinimumLength = 2)]
        public string LastName { get; set; }

        [Required(ErrorMessage = "Моля, въведете първото си име.")]
        [StringLength(100, ErrorMessage = "Името трябва да е от {2} до {1} символа", MinimumLength = 2)]
        public string FirstName { get; set; }

        public string? BothNames { get; set; }
        public string? Email { get; set; }

        [Required(ErrorMessage = "Моля, въведете държава.")]
        [Display(Name = "Държава")]
        public string Country { get; set; }
        [ValidateNever]
        public IEnumerable<SelectListItem> CountryList { get; set; } = new List<SelectListItem>();

        [Required(ErrorMessage = "Моля, въведете телефонен номер.")]
        [Phone(ErrorMessage = "Невалиден телефонен номер.")]
        [Display(Name = "Телефонен номер")]
        public string PhoneNumber { get; set; }

        [Required(ErrorMessage = "Моля, въведете своята дата на раждане.")]
        [DataType(DataType.Date)]
        [Display(Name = "Дата на раждане")]
        public DateTime? DateOfBirth { get; set; }

        public string? ProfileImage { get; set; }

        public string? PasswordHash { get; set; }
        public IEnumerable<Participation>? Participations { get; set; } = new List<Participation>();
    }
}
