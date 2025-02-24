using System.ComponentModel.DataAnnotations;

namespace SportConnect.Models
{
    public class Sport
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Името е задължително")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Описанието е задължително")]
        public string Description { get; set; }

        public IEnumerable<Tournament> Tournaments { get; set; } = new List<Tournament>();
    }
}