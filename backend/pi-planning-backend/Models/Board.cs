using System.ComponentModel.DataAnnotations;

namespace PiPlanningBackend.Models
{
    public class Board
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(100)]
        public string Name { get; set; } = "";

        [MaxLength(100)]
        public string? Organization { get; set; }

        [MaxLength(100)]
        public string? Project { get; set; }

        [MaxLength(100)]
        public string? AzureStoryPointField { get; set; }

        [MaxLength(100)]
        public string? AzureDevStoryPointField { get; set; }

        [MaxLength(100)]
        public string? AzureTestStoryPointField { get; set; }

        public int NumSprints { get; set; }
        public int SprintDuration { get; set; }

        [Required]
        public DateTime StartDate { get; set; } = DateTime.UtcNow;

        public bool IsLocked { get; set; }
        public string? PasswordHash { get; set; }
        public bool IsFinalized { get; set; }
        public bool DevTestToggle { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<Sprint> Sprints { get; set; } = [];
        public ICollection<Feature> Features { get; set; } = [];
        public ICollection<TeamMember> TeamMembers { get; set; } = [];
    }
}
