using PiPlanningBackend.Models;
using PiPlanningBackend.Services.Interfaces;

namespace PiPlanningBackend.Services.Implementations
{
    public class SprintService : ISprintService
    {
        /// <summary>
        /// Generates sprints for a board with auto-calculated dates.
        /// Sprint 0 is a placeholder/parking lot (no real dates).
        /// Sprints 1-N have actual date ranges based on sprintDurationDays.
        /// </summary>
        public List<Sprint> GenerateSprintsForBoard(Board board, int numSprints, int sprintDurationDays)
        {
            var sprints = new List<Sprint>();
            var startDateUtc = board.StartDate;

            // Sprint 0 is a placeholder/parking lot (no real dates)
            var sprintZero = new Sprint
            {
                Name = "Sprint 0",
                StartDate = startDateUtc,
                EndDate = startDateUtc  // Placeholder sprint has same start/end
            };
            sprints.Add(sprintZero);

            // Actual sprints (1 through NumSprints)
            var currentSprintStart = startDateUtc;
            for (int i = 1; i <= numSprints; i++)
            {
                var sprint = new Sprint
                {
                    Name = $"Sprint {i}",
                    StartDate = currentSprintStart,
                    EndDate = currentSprintStart.AddDays(sprintDurationDays - 1)
                };
                sprints.Add(sprint);

                currentSprintStart = currentSprintStart.AddDays(sprintDurationDays);
            }

            return sprints;
        }
    }
}
