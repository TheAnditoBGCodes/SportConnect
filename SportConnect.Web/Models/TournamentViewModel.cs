using Microsoft.AspNetCore.Mvc.Rendering;
using SportConnect.Models;
using SportConnect.Utility;
using System.ComponentModel.DataAnnotations;

public class TournamentViewModel
{
    public int? Id { get; set; }

    public string? Name { get; set; }

    public string? Description { get; set; }

    public string? OrganizerId { get; set; }
    public SportConnectUser? Organizer { get; set; }
    public string? OrganizerName { get; set; }

    public string? DateOrder { get; set; }

    public DateTime? Date { get; set; }

    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }

    public DateTime? Deadline { get; set; }

    public string? Location { get; set; }

    public int? SportId { get; set; }
    public bool? Approved { get; set; }

    public string? Country { get; set; }
    public IEnumerable<SelectListItem>? CountryList { get; set; } = new List<SelectListItem>();

    public SelectList? Sports { get; set; }
    public string? SportName { get; set; }
    public string? ImageUrl { get; set; }
    public Sport? Sport { get; set; }
    public IEnumerable<Participation>? Participations { get; set; } = new List<Participation>();
    public IEnumerable<Participation>? UserParticipations { get; set; } = new List<Participation>();
    public List<Tournament>? Tournaments { get; set; }
    public List<Tournament>? FilteredTournaments { get; set; }
    public string? UserId { get; set; }

    [DataType(DataType.Time)]
    public TimeSpan? DateTimer { get; set; }

    [DataType(DataType.Time)]
    public TimeSpan? DeadlineTime { get; set; }

    public Tournament GetTournament()
    {
        return new Tournament
        {
            Name = Name,
            Description = Description,
            OrganizerId = OrganizerId,
            Date = (DateTime)Date,
            Deadline = (DateTime)Deadline,
            Country = Country,
            ImageUrl = ImageUrl,
            SportId = (int)SportId,
        };
    }
}