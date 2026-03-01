namespace PiPlanningBackend.DTOs.SignalR
{
    public class BoardFinalizedDto
    {
        public int BoardId { get; set; }
        public bool IsFinalized { get; set; } = true;
        public DateTime TimestampUtc { get; set; }
    }

    public class BoardRestoredDto
    {
        public int BoardId { get; set; }
        public bool IsFinalized { get; set; }
        public DateTime TimestampUtc { get; set; }
    }
}