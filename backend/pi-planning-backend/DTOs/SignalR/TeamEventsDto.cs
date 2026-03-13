namespace PiPlanningBackend.DTOs.SignalR
{
    public class TeamMemberAddedDto
    {
        public int BoardId { get; set; }
        public TeamMemberResponseDto TeamMember { get; set; } = new();
        public DateTime TimestampUtc { get; set; }
    }

    public class TeamMemberUpdatedDto
    {
        public int BoardId { get; set; }
        public TeamMemberResponseDto TeamMember { get; set; } = new();
        public DateTime TimestampUtc { get; set; }
    }

    public class TeamMemberDeletedDto
    {
        public int BoardId { get; set; }
        public int TeamMemberId { get; set; }
        public DateTime TimestampUtc { get; set; }
    }

    public class CapacityUpdatedDto
    {
        public int BoardId { get; set; }
        public int TeamMemberId { get; set; }
        public int SprintId { get; set; }
        public int CapacityDev { get; set; }
        public int CapacityTest { get; set; }
        public DateTime TimestampUtc { get; set; }
    }
}
