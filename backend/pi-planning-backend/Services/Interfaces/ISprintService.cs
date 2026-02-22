using PiPlanningBackend.Models;

namespace PiPlanningBackend.Services.Interfaces
{
    public interface ISprintService
    {
        /// <summary>
        /// Generates sprints for a board with auto-calculated dates.
        /// Sprint 0 is a placeholder, sprints 1-N have actual date ranges.
        /// </summary>
        List<Sprint> GenerateSprintsForBoard(Board board, int numSprints, int sprintDurationDays);
    }
}
