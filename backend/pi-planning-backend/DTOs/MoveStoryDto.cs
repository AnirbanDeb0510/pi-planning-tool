using System.ComponentModel.DataAnnotations;

namespace PiPlanningBackend.DTOs
{
    public class MoveStoryDto
    {
        [Range(1, int.MaxValue, ErrorMessage = "TargetSprintId must be a positive integer")]
        public int TargetSprintId { get; set; }
    }
}
