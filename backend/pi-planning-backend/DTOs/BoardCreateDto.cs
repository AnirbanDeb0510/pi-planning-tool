using System;
using System.ComponentModel.DataAnnotations;

namespace PiPlanningBackend.DTOs
{
    public class BoardCreateDto
    {
        [Required(ErrorMessage = "Board name is required")]
        [StringLength(100, MinimumLength = 1, ErrorMessage = "Board name must be between 1 and 100 characters")]
        public string Name { get; set; } = "";

        [Required(ErrorMessage = "Organization is required")]
        [StringLength(100, MinimumLength = 1, ErrorMessage = "Organization must be between 1 and 100 characters")]
        public string Organization { get; set; } = "";

        [Required(ErrorMessage = "Project is required")]
        [StringLength(100, MinimumLength = 1, ErrorMessage = "Project must be between 1 and 100 characters")]
        public string Project { get; set; } = "";

        [StringLength(100)]
        public string? AzureStoryPointField { get; set; }

        [StringLength(100)]
        public string? AzureDevStoryPointField { get; set; }

        [StringLength(100)]
        public string? AzureTestStoryPointField { get; set; }

        [Range(1, 20)]
        public int NumSprints { get; set; }

        [Range(1, 60)]
        public int SprintDuration { get; set; }

        public bool DevTestToggle { get; set; }

        [DataType(DataType.DateTime)]
        public DateTime StartDate { get; set; } = DateTime.UtcNow;

        [StringLength(50)]
        public string? Password { get; set; }
    }
}
