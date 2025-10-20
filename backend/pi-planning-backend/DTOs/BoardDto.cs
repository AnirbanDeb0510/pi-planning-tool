namespace PiPlanningBackend.DTOs
{
    public class BoardDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string? Organization { get; set; }
        public string? Project { get; set; }
        public bool DevTestToggle { get; set; }
    }
}
