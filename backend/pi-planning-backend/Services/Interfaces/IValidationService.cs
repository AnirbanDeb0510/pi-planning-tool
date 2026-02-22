using PiPlanningBackend.Models;

namespace PiPlanningBackend.Services.Interfaces
{
    public interface IValidationService
    {
        /// <summary>
        /// Validates that a board exists.
        /// </summary>
        /// <exception cref="KeyNotFoundException">Thrown if board is not found</exception>
        Task ValidateBoardExists(int boardId);

        /// <summary>
        /// Validates that a user story belongs to a specific board.
        /// </summary>
        /// <exception cref="KeyNotFoundException">Thrown if story doesn't exist or doesn't belong to board</exception>
        Task ValidateStoryBelongsToBoard(int storyId, int boardId);

        /// <summary>
        /// Validates that a team member belongs to a specific board.
        /// </summary>
        /// <exception cref="KeyNotFoundException">Thrown if member doesn't exist or doesn't belong to board</exception>
        Task ValidateTeamMemberBelongsToBoard(int memberId, int boardId);

        /// <summary>
        /// Validates that a sprint belongs to a specific board.
        /// </summary>
        /// <exception cref="KeyNotFoundException">Thrown if sprint doesn't exist or doesn't belong to board</exception>
        Task ValidateSprintBelongsToBoard(int sprintId, int boardId);

        /// <summary>
        /// Validates that a feature belongs to a specific board.
        /// </summary>
        /// <exception cref="KeyNotFoundException">Thrown if feature doesn't exist or doesn't belong to board</exception>
        Task ValidateFeatureBelongsToBoard(int featureId, int boardId);

        /// <summary>
        /// Validates that a board is not finalized before allowing modifications.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown if board is finalized</exception>
        void ValidateBoardNotFinalized(Board board, string operation);

        /// <summary>
        /// Validates team member capacity for a sprint.
        /// </summary>
        /// <exception cref="ArgumentException">Thrown if capacity is invalid</exception>
        void ValidateTeamMemberCapacity(int capacity, int sprintWorkDays);
    }
}
