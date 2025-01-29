using System.ComponentModel.DataAnnotations;

namespace SportConnect.Models
{
    public class Sport
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Name { get; set; }

        [Required]
        public string Description { get; set; }

        public IEnumerable<Tournament> Tournaments { get; set; } = new List<Tournament>();
    }
}