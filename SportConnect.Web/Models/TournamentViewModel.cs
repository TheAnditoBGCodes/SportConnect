using Microsoft.AspNetCore.Mvc.Rendering;
using SportConnect.Models;
using SportConnect.Utility;
using System.ComponentModel.DataAnnotations;

public class TournamentViewModel
{
    public int? Id { get; set; }

    [Required(ErrorMessage = "Името е задължително")]
    [StringLength(100, MinimumLength = 5, ErrorMessage = "Името трябва да е между 5 и 100 символа")]
    public string Name { get; set; }

    [Required(ErrorMessage = "Описанието е задължително")]
    [StringLength(100, MinimumLength = 5, ErrorMessage = "Описанието трябва да е между 5 и 100 символа")]
    public string Description { get; set; }

    public string? OrganizerId { get; set; }
    public SportConnectUser? Organizer { get; set; }
    public string? OrganizerName { get; set; }

    [Required(ErrorMessage = "Датата на провеждане е задължителна")]
    [DataType(DataType.DateTime)]
    public DateTime? Date { get; set; }

    [Required(ErrorMessage = "Крайният срок е задължителен")]
    [DeadlineBeforeTournament("Date", ErrorMessage = "Крайният срок трябва да е преди датата на турнира.")]
    [DataType(DataType.DateTime)]
    public DateTime? Deadline { get; set; }

    [Required(ErrorMessage = "Локацията е задължителна")]
    [StringLength(100, MinimumLength = 5, ErrorMessage = "Локацията трябва да е между 5 и 100 символа")]
    public string Location { get; set; }

    [Required(ErrorMessage = "Спортът е задължителен")]
    public int? SportId { get; set; }

    public SelectList? Sports { get; set; }
    public string? SportName { get; set; }
    public Sport? Sport { get; set; }
    public IEnumerable<Participation> Participations { get; set; } = new List<Participation>();

    public Tournament ToTournament()
    {
        return new Tournament
        {
            Id = Id ?? 0,
            Name = Name,
            Description = Description,
            Deadline = Deadline ?? DateTime.MinValue,
            Date = Date ?? DateTime.MinValue,
            Location = Location,
            SportId = SportId.GetValueOrDefault(),
            OrganizerId = OrganizerId
        };
    }
}