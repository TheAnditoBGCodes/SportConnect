using Microsoft.AspNetCore.Mvc.Rendering;
using SportConnect.Models;
using System.ComponentModel.DataAnnotations;

public class TournamentViewModel
{
    public string? PreviousId { get; set; }
    public bool? Approved { get; set; }

    public string? Id { get; set; }
    public string? Name { get; set; }

    public string? Description { get; set; }

    public string? OrganizerId { get; set; }
    public string? OrganizerName { get; set; }

    public string? DateOrder { get; set; }

    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }

    public DateTime? Date { get; set; }
    [DataType(DataType.Time)]
    public TimeSpan? DateTimer { get; set; }

    public DateTime? Deadline { get; set; }
    [DataType(DataType.Time)]
    public TimeSpan? DeadlineTime { get; set; }

    public string? Country { get; set; }
    public IEnumerable<SelectListItem>? CountryList { get; set; } = new List<SelectListItem>();

    public string? SportId { get; set; }
    public SelectList? Sports { get; set; }

    public string? ImageUrl { get; set; }

    public List<Tournament>? Tournaments { get; set; }
    public List<Tournament>? FilteredTournaments { get; set; }

    public async Task<Tournament> ToTournament()
    {
        return new Tournament
        {
            Name = Name,
            Description = Description,
            OrganizerId = OrganizerId,
            Date = ((DateTime)Date).ToString("yyyy-MM-ddTHH:mm:ss"),
            Deadline = ((DateTime)Deadline).ToString("yyyy-MM-ddTHH:mm:ss"),
            Country = Country,
            ImageUrl = ImageUrl,
            SportId = SportId,
        };
    }
}