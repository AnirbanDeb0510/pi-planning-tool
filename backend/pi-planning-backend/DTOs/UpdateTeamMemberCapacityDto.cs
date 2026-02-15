using System.ComponentModel.DataAnnotations;

namespace PiPlanningBackend.DTOs
{
    public class UpdateTeamMemberCapacityDto
    {
        [Range(0, int.MaxValue, ErrorMessage = "Capacity Dev must be a positive integer")]
        public int CapacityDev { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Capacity Test must be a positive integer")]
        public int CapacityTest { get; set; }
    }
}