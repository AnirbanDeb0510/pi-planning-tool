using System.ComponentModel.DataAnnotations;

namespace PiPlanningBackend.DTOs
{
    public class ImportFeaturesDto
    {
        [Range(1, int.MaxValue, ErrorMessage = "BoardId must be a positive integer")]
        public int BoardId { get; set; }

        [Required(ErrorMessage = "At least one Azure feature ID is required")]
        [MinLength(1, ErrorMessage = "At least one feature ID must be provided")]
        public List<int> AzureFeatureIds { get; set; } = new();

        [StringLength(500)]
        public string? Pat { get; set; }
    }
}
