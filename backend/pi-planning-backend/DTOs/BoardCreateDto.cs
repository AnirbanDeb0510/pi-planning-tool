using System;
using System.ComponentModel.DataAnnotations;

namespace PiPlanningBackend.DTOs
{
    public class BoardCreateDto
    {
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = "";

        public string? Organization { get; set; }
        public string? Project { get; set; }
        public string? AzureStoryPointField { get; set; }
        public string? AzureDevStoryPointField { get; set; }
        public string? AzureTestStoryPointField { get; set; }

        [Range(1, 20)]
        public int NumSprints { get; set; }

        [Range(1, 60)]
        public int SprintDuration { get; set; }

        public bool DevTestToggle { get; set; }
        
        public DateTime StartDate { get; set; } = DateTime.UtcNow;

        /// 🔐 Optional password for board protection
        public string? Password { get; set; }
    }
}
