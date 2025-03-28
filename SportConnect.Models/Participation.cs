using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SportConnect.Models
{
    public class Participation
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey(nameof(Participant))]
        public string ParticipantId { get; set; }
        public SportConnectUser Participant { get; set; }

        [ForeignKey(nameof(Tournament))]
        public int TournamentId { get; set; }
        public Tournament Tournament { get; set; }

        public bool Approved { get; set; }
    }
}