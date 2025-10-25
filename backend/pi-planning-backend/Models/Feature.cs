using System.ComponentModel.DataAnnotations;

namespace PiPlanningBackend.Models
{
    public class Feature
    {
        public int Id { get; set; }

        [Required]
        public int BoardId { get; set; }
        public Board? Board { get; set; }

        public string? AzureId { get; set; }

        [Required, MaxLength(200)]
        public string Title { get; set; } = "";
        public int? Priority { get; set; }
        public string? ValueArea { get; set; }
        public bool IsFinalized { get; set; }

        public ICollection<UserStory> UserStories { get; set; } = new List<UserStory>();
    }
}
