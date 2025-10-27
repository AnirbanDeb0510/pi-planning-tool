using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PiPlanningBackend.Models
{
    public class Sprint
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [ForeignKey(nameof(Board))]
        public int BoardId { get; set; }
        public Board? Board { get; set; }

        [Required, MaxLength(100)]
        public string Name { get; set; } = "";

        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        public ICollection<TeamMemberSprint> TeamMemberSprints { get; set; } = [];
        public ICollection<UserStory> UserStories { get; set; } = [];
    }
}
