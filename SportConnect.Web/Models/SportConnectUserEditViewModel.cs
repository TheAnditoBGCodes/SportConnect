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

        [Required(ErrorMessage = "Моля, въведете имейл.")]
        [EmailAddress(ErrorMessage = "Невалиден имейл")]
        [Display(Name = "Имейл")]
        public string Email { get; set; }

        public string? BothNames { get; set; }

        [Required(ErrorMessage = "Моля, въведете потребителско име.")]
        [StringLength(100, ErrorMessage = "Tрябва да е от {2} до {1} символа", MinimumLength = 5)]
        [Display(Name = "Потребителско име")]
        public string UserName { get; set; }

        [Required(ErrorMessage = "Моля, въведете първото си име.")]
        [StringLength(100, ErrorMessage = "Tрябва да е от {2} до {1} символа", MinimumLength = 2)]
        [Display(Name = "Първо име")]
        public string FirstName { get; set; }

        [Required(ErrorMessage = "Моля, въведете фамилното си име.")]
        [StringLength(100, ErrorMessage = "Tрябва да е от {2} до {1} символа", MinimumLength = 2)]
        [Display(Name = "Фамилия")]
        public string LastName { get; set; }

        [Required(ErrorMessage = "Моля, въведете своята дата на раждане.")]
        [DataType(DataType.Date)]
        [Display(Name = "Дата на раждане")]
        public DateTime DateOfBirth { get; set; }

        [Required(ErrorMessage = "Моля, въведете държава.")]
        [Display(Name = "Държава")]
        public string Country { get; set; }
        [ValidateNever]
        public IEnumerable<SelectListItem> CountryList { get; set; } = new List<SelectListItem>();

        [Required(ErrorMessage = "Моля, въведете телефонен номер.")]
        [Phone(ErrorMessage = "Невалиден телефонен номер.")]
        [Display(Name = "Телефонен номер")]
        public string PhoneNumber { get; set; }

        public string? ProfileImage { get; set; }
        public string? PasswordHash { get; set; }
        public IEnumerable<Participation>? Participations { get; set; } = new List<Participation>();
    }
}
