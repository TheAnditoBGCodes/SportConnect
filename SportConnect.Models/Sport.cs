using System.ComponentModel.DataAnnotations;

namespace SportConnect.Models
{
    public class Sport
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        public string Name { get; set; }

        public string Description { get; set; }

        public string ImageUrl { get; set; }

        public IEnumerable<Tournament> Tournaments { get; set; } = new List<Tournament>();
    }
}