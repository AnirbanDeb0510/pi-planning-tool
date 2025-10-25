using System.ComponentModel.DataAnnotations;

namespace PiPlanningBackend.Models
{
    public class Sprint
    {
        public int Id { get; set; }

        [Required]
        public int BoardId { get; set; }
        public Board? Board { get; set; }

        [Required, MaxLength(100)]
        public string Name { get; set; } = "";
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        public ICollection<TeamMemberSprint> TeamMemberSprints { get; set; } = new List<TeamMemberSprint>();
        public ICollection<UserStory> UserStories { get; set; } = new List<UserStory>();
    }
}
