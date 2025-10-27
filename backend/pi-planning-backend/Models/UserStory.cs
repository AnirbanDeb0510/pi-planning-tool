using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PiPlanningBackend.Models
{
    public class UserStory
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [ForeignKey(nameof(Feature))]
        public int FeatureId { get; set; }
        public Feature? Feature { get; set; }

        [MaxLength(100)]
        public string? AzureId { get; set; }

        [Required, MaxLength(200)]
        public string Title { get; set; } = "";

        public double? StoryPoints { get; set; }
        public double? DevStoryPoints { get; set; }
        public double? TestStoryPoints { get; set; }

        public int? OriginalSprintId { get; set; }

        [Required]
        public int SprintId { get; set; }

        [ForeignKey(nameof(SprintId))]
        public Sprint Sprint { get; set; } = null!;

        public bool IsMoved { get; set; }

        public string? Notes { get; set; }
    }
}
