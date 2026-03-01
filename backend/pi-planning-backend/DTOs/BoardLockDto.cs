using System.ComponentModel.DataAnnotations;

namespace PiPlanningBackend.DTOs
{
    public class BoardLockDto
    {
        [Required(ErrorMessage = "Password is required to lock the board")]
        [StringLength(50, MinimumLength = 8, ErrorMessage = "Password must be between 8 and 50 characters")]
        public string Password { get; set; } = "";
    }
}
