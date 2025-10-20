namespace PiPlanningBackend.Models
{
    public class Board
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string? Organization { get; set; }
        public string? Project { get; set; }
        public string? AzureStoryPointField { get; set; }
        public string? AzureDevStoryPointField { get; set; }
        public string? AzureTestStoryPointField { get; set; }
        public int NumSprints { get; set; }
        public int SprintDuration { get; set; }
        public bool IsLocked { get; set; }
        public string? PasswordHash { get; set; }
        public bool IsFinalized { get; set; }
        public bool DevTestToggle { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<Sprint> Sprints { get; set; } = new List<Sprint>();
        public ICollection<Feature> Features { get; set; } = new List<Feature>();
    }
}
