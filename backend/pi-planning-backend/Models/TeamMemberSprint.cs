using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PiPlanningBackend.Models
{
    public class TeamMemberSprint
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int TeamMemberId { get; set; }

        [ForeignKey(nameof(TeamMemberId))]
        public TeamMember TeamMember { get; set; } = null!;

        [Required]
        public int SprintId { get; set; }

        [ForeignKey(nameof(SprintId))]
        public Sprint Sprint { get; set; } = null!;

        // Capacity split for Dev and Test
        public int CapacityDev { get; set; }

        public int CapacityTest { get; set; }
    }
}
