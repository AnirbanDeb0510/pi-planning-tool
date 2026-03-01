namespace PiPlanningBackend.DTOs.SignalR
{
    public class CursorPresenceDto
    {
        public int BoardId { get; set; }
        public string UserId { get; set; } = "";
        public string DisplayName { get; set; } = "";
        public CursorPositionDto Cursor { get; set; } = new();
        public string CoordinateSpace { get; set; } = "board";
        public string Color { get; set; } = "#3B82F6";
        public string Avatar { get; set; } = "?";
        public string Activity { get; set; } = "active";
        public long Sequence { get; set; }
        public DateTime TimestampUtc { get; set; }
    }

    public class CursorPositionDto
    {
        public int X { get; set; }
        public int Y { get; set; }
    }
}
