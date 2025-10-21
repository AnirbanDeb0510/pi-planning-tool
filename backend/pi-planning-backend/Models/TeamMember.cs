using System.ComponentModel.DataAnnotations;

namespace PiPlanningBackend.Models
{
    public class TeamMember
    {
        public int Id { get; set; }

        [Required, MaxLength(100)]
        public string Name { get; set; } = "";
        public bool IsDev { get; set; }
        public bool IsTest { get; set; }

        // Optional link to board
        public int BoardId { get; set; }
        public Board? Board { get; set; }

        // Navigation to sprints
        public ICollection<TeamMemberSprint> TeamMemberSprints { get; set; } = [];
    }
}
