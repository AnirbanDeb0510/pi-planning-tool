namespace PiPlanningBackend.DTOs
{
    public class BoardCreatedDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string? Organization { get; set; }
        public string? Project { get; set; }
        public int NumSprints { get; set; }
        public int SprintDuration { get; set; }
        public DateTime StartDate { get; set; }
        public bool IsLocked { get; set; }
        public bool IsFinalized { get; set; }
        public bool DevTestToggle { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
