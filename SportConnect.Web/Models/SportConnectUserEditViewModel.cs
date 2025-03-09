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

        [Required(ErrorMessage = "����, �������� ������������� ���.")]
        [StringLength(100, ErrorMessage = "��������������� ��� ������ �� � �� {2} �� {1} �������", MinimumLength = 5)]
        public string UserName { get; set; }

        [Required(ErrorMessage = "����, �������� ��������� �� ���.")]
        [StringLength(100, ErrorMessage = "��������� ������ �� � �� {2} �� {1} �������", MinimumLength = 2)]
        public string LastName { get; set; }

        [Required(ErrorMessage = "����, �������� ������� �� ���.")]
        [StringLength(100, ErrorMessage = "����� ������ �� � �� {2} �� {1} �������", MinimumLength = 2)]
        public string FirstName { get; set; }

        public string? BothNames { get; set; }
        public string? Email { get; set; }

        [Required(ErrorMessage = "����, �������� �������.")]
        [Display(Name = "�������")]
        public string Country { get; set; }
        [ValidateNever]
        public IEnumerable<SelectListItem> CountryList { get; set; } = new List<SelectListItem>();

        [Required(ErrorMessage = "����, �������� ��������� �����.")]
        [Phone(ErrorMessage = "��������� ��������� �����.")]
        [Display(Name = "��������� �����")]
        public string PhoneNumber { get; set; }

        [Required(ErrorMessage = "����, �������� ������ ���� �� �������.")]
        [DataType(DataType.Date)]
        [Display(Name = "���� �� �������")]
        public DateTime? DateOfBirth { get; set; }

        public string? ProfileImage { get; set; }

        public string? PasswordHash { get; set; }
        public IEnumerable<Participation>? Participations { get; set; } = new List<Participation>();
    }
}
