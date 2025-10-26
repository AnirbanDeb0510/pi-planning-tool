using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PiPlanningBackend.Models
{
    public class Feature
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [ForeignKey(nameof(Board))]
        public int BoardId { get; set; }
        public Board? Board { get; set; }

        [MaxLength(100)]
        public string? AzureId { get; set; }

        [Required, MaxLength(200)]
        public string Title { get; set; } = "";

        public int? Priority { get; set; }

        [MaxLength(100)]
        public string? ValueArea { get; set; }

        public bool IsFinalized { get; set; }

        public ICollection<UserStory> UserStories { get; set; } = [];
    }
}