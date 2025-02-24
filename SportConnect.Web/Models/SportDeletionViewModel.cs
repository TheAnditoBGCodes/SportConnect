using Microsoft.AspNetCore.Mvc.Rendering;
using SportConnect.Models;
using SportConnect.Utility;
using System.ComponentModel.DataAnnotations;

public class SportDeletionViewModel
{
    [Required(ErrorMessage = "Името е задължително")]
    [StringLength(100, MinimumLength = 5, ErrorMessage = "Името трябва да е между 5 и 100 символа")]
    public string? Name { get; set; }

    [Required(ErrorMessage = "Описанието е задължително")]
    [StringLength(100, MinimumLength = 5, ErrorMessage = "Описанието трябва да е между 5 и 100 символа")]
    public string? Description { get; set; }

    public IEnumerable<Tournament>? Tournaments { get; set; } = new List<Tournament>();
}