using SportConnect.Models;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Drawing;

namespace SportConnect.Web.Models
{
    public class TournamentFilterViewModel
    {
        public DateTime? Date { get; set; }
        public int? SportId { get; set; }
        public SelectList Sports { get; set; }
        public List<Tournament> Tournaments { get; set; }
        public List<Participation> UserParticipations { get; set; }
        public string? UserId { get; set; }
    }
}
