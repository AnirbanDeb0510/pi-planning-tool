namespace PiPlanningBackend.DTOs.SignalR
{
    public class StoryMovedDto
    {
        public int BoardId { get; set; }
        public int StoryId { get; set; }
        public int TargetSprintId { get; set; }
        public DateTime TimestampUtc { get; set; }
    }
}
