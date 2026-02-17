using System.ComponentModel.DataAnnotations;

namespace PiPlanningBackend.DTOs
{
    public class UpdateFeaturePriorityDto
    {
        [Range(0, int.MaxValue, ErrorMessage = "Priority must be a non-negative integer")]
        public int NewPriority { get; set; }
    }
}
