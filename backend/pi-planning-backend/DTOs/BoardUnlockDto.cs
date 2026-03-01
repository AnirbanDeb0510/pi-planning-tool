using System.ComponentModel.DataAnnotations;

namespace PiPlanningBackend.DTOs
{
    public class BoardUnlockDto
    {
        [Required(ErrorMessage = "Password is required to unlock the board")]
        [StringLength(50, MinimumLength = 1, ErrorMessage = "Password must be between 1 and 50 characters")]
        public string Password { get; set; } = "";
    }
}
