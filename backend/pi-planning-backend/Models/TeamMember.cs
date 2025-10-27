using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PiPlanningBackend.Models
{
    public class TeamMember
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(100)]
        public string Name { get; set; } = "";

        public bool IsDev { get; set; }
        public bool IsTest { get; set; }

        [Required]
        [ForeignKey(nameof(Board))]
        public int BoardId { get; set; }
        public Board? Board { get; set; }

        public ICollection<TeamMemberSprint> TeamMemberSprints { get; set; } = [];
    }
}
