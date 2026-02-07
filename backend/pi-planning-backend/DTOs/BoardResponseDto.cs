namespace PiPlanningBackend.DTOs
{
    public class BoardResponseDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public bool IsLocked { get; set; }
        public bool IsFinalized { get; set; }
        public bool DevTestToggle { get; set; }
        public DateTime StartDate { get; set; }
        public required List<SprintDto> Sprints { get; set; }
        public List<FeatureResponseDto> Features { get; set; } = [];
        public List<TeamMemberResponseDto> TeamMembers { get; set; } = [];
    }

    public class FeatureResponseDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = "";
        public string? AzureId { get; set; }
        public int? Priority { get; set; }
        public string? ValueArea { get; set; }
        public List<UserStoryDto> UserStories { get; set; } = [];
    }

    public class SprintDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }

    public class TeamMemberResponseDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public bool IsDev { get; set; }
        public bool IsTest { get; set; }
        public List<TeamMemberSprintDto> SprintCapacities { get; set; } = [];
    }

    public class TeamMemberSprintDto
    {
        public int SprintId { get; set; }
        public double CapacityDev { get; set; }
        public double CapacityTest { get; set; }
    }
}