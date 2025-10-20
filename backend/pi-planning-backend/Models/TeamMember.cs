namespace PiPlanningBackend.Models
{
    public class TeamMember
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public bool IsDev { get; set; }
        public bool IsTest { get; set; }
    }
}
