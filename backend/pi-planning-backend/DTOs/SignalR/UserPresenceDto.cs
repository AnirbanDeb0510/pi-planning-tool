namespace PiPlanningBackend.DTOs.SignalR
{
    public class UserPresenceDto
    {
        public int BoardId { get; set; }
        public string UserId { get; set; } = "";
        public string DisplayName { get; set; } = "";
        public string Color { get; set; } = "#3B82F6";
        public string Avatar { get; set; } = "?";
        public DateTime TimestampUtc { get; set; }
        public string Reason { get; set; } = "joined";
    }
}
