using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SportConnect.Models
{
    public class Tournament
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        public string Name { get; set; }

        public string Description { get; set; }

        [ForeignKey(nameof(Organizer))]
        public string OrganizerId { get; set; }
        public SportConnectUser Organizer { get; set; }

        public DateTime Date { get; set; }

        public DateTime Deadline { get; set; }

        public string Country { get; set; }
        public string ImageUrl { get; set; }

        [ForeignKey(nameof(Sport))]
        public string SportId { get; set; }
        public Sport Sport { get; set; }

        public IEnumerable<Participation> Participations { get; set; } = new List<Participation>();
    }
}