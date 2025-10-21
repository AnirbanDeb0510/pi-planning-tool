using System.ComponentModel.DataAnnotations;

namespace PiPlanningBackend.Models
{
    public class UserStory
    {
        public int Id { get; set; }

        [Required]
        public int FeatureId { get; set; }
        public Feature? Feature { get; set; }

        public string? AzureId { get; set; }

        [Required, MaxLength(200)]
        public string Title { get; set; } = "";

        public double? StoryPoints { get; set; }
        public double? DevStoryPoints { get; set; }
        public double? TestStoryPoints { get; set; }

        public int? OriginalSprintId { get; set; }
        public int? CurrentSprintId { get; set; }

        public bool IsMoved { get; set; }
        public string? Notes { get; set; }
    }
}
