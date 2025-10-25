namespace PiPlanningBackend.DTOs
{
    public class TeamMemberDto
    {
        public int? Id { get; set; } // null for new members
        public string Name { get; set; } = "";
        public bool IsDev { get; set; }
        public bool IsTest { get; set; }
        public int? BoardId { get; set; }
    }
}
