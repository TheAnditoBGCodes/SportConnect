using SportConnect.Models;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Drawing;

namespace SportConnect.Web.Models
{
    public class ParticipationViewModel
    {
        public int Id { get; set; }

        public string ParticipantId { get; set; }
        public string ParticipantName { get; set; }

        public DateTime RegistrationDate { get; set; }

        public DateTime TournamentDate { get; set; }
        public DateTime TournamentDeadLine { get; set; }

        public int TournamentId { get; set; }
        public string TournamentName { get; set; }
        public string TournamentSport { get; set; }

    }
}
