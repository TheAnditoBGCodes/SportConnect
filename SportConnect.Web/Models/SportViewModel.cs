using SportConnect.Models;

namespace SportConnect.Web.Models
{
    public class SportViewModel
    {
        public List<Sport>? Sports { get; set; }
        public List<Sport>? FilteredSports { get; set; }

        public string? Id { get; set; }
        public string? Name { get; set; }

        public string? Description { get; set; }

        public string? ImageUrl { get; set; }

        public async Task<Sport> ToSport()
        {
            return new Sport
            {
                Name = Name,
                Description = Description,
                ImageUrl = ImageUrl
            };
        }
    }
}