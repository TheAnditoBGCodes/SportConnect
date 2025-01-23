using System.ComponentModel.DataAnnotations;

namespace SportConnect.Models
{
    public class Sport
    {
        [Key]
        public int SportId { get; set; }

        [Required]
        public string Name { get; set; }

        [Required]
        public string Description { get; set; }

        public ICollection<Tournament> Tournaments { get; set; }
    }
}