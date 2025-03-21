using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

        [Required]
        public bool Approved { get; set; }
    }
}