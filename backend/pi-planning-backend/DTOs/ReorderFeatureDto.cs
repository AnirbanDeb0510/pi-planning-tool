using System.ComponentModel.DataAnnotations;

namespace PiPlanningBackend.DTOs
{
    public class ReorderFeatureDto
    {
        [Required(ErrorMessage = "Feature list is required")]
        [MinLength(1, ErrorMessage = "At least one feature must be provided")]
        public List<ReorderFeatureItemDto> Features { get; set; } = new();
    }

    public class ReorderFeatureItemDto
    {
        [Range(1, int.MaxValue, ErrorMessage = "FeatureId must be a positive integer")]
        public int FeatureId { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Priority must be a non-negative integer")]
        public int NewPriority { get; set; }
    }
}
