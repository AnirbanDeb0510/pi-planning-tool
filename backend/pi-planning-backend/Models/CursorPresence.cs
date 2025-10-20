namespace PiPlanningBackend.Models
{
    public class CursorPresence
    {
        public int Id { get; set; }
        public int BoardId { get; set; }
        public int? TeamMemberId { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
        public DateTime LastSeen { get; set; } = DateTime.UtcNow;
    }
}
