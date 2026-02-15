using System.ComponentModel.DataAnnotations;

namespace PiPlanningBackend.DTOs
{
    public class TeamMemberDto
    {
        public int? Id { get; set; } // null for new members

        [Required(ErrorMessage = "Team member name is required")]
        [StringLength(100, MinimumLength = 1, ErrorMessage = "Team member name must be between 1 and 100 characters")]
        public string Name { get; set; } = "";

        public bool IsDev { get; set; }
        public bool IsTest { get; set; }
        public int? BoardId { get; set; }
    }
}
