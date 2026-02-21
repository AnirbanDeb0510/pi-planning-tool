using PiPlanningBackend.Models;
using PiPlanningBackend.Repositories.Interfaces;
using PiPlanningBackend.Services.Interfaces;

namespace PiPlanningBackend.Services.Implementations
{
    public class ValidationService(
        IBoardRepository boardRepository,
        IFeatureRepository featureRepository,
        IUserStoryRepository userStoryRepository,
        ITeamRepository teamRepository) : IValidationService
    {
        private readonly IBoardRepository _boardRepository = boardRepository;
        private readonly IFeatureRepository _featureRepository = featureRepository;
        private readonly IUserStoryRepository _userStoryRepository = userStoryRepository;
        private readonly ITeamRepository _teamRepository = teamRepository;

        /// <summary>
        /// Validates that a board exists.
        /// </summary>
        public async Task ValidateBoardExists(int boardId)
        {
            var board = await _boardRepository.GetByIdAsync(boardId);
            if (board == null)
            {
                throw new KeyNotFoundException($"Board with ID {boardId} not found.");
            }
        }

        /// <summary>
        /// Validates that a user story belongs to a specific board.
        /// </summary>
        public async Task ValidateStoryBelongsToBoard(int storyId, int boardId)
        {
            var story = await _userStoryRepository.GetByIdWithDetailsAsync(storyId);

            if (story == null || story.Feature?.BoardId != boardId)
            {
                throw new KeyNotFoundException(
                    $"User story with ID {storyId} not found or does not belong to board {boardId}.");
            }
        }

        /// <summary>
        /// Validates that a team member belongs to a specific board.
        /// </summary>
        public async Task ValidateTeamMemberBelongsToBoard(int memberId, int boardId)
        {
            var member = await _teamRepository.GetTeamMemberAsync(memberId);

            if (member == null || member.BoardId != boardId)
            {
                throw new KeyNotFoundException(
                    $"Team member with ID {memberId} not found in board {boardId}.");
            }
        }

        /// <summary>
        /// Validates that a sprint belongs to a specific board.
        /// </summary>
        public async Task ValidateSprintBelongsToBoard(int sprintId, int boardId)
        {
            var board = await _boardRepository.GetBoardWithSprintsAsync(boardId);

            if (board == null)
            {
                throw new KeyNotFoundException($"Board with ID {boardId} not found.");
            }

            var sprint = board.Sprints.FirstOrDefault(s => s.Id == sprintId);

            if (sprint == null)
            {
                throw new KeyNotFoundException(
                    $"Sprint with ID {sprintId} not found in board {boardId}.");
            }
        }

        /// <summary>
        /// Validates that a feature belongs to a specific board.
        /// </summary>
        public async Task ValidateFeatureBelongsToBoard(int featureId, int boardId)
        {
            var feature = await _featureRepository.GetByIdAsync(featureId);

            if (feature == null || feature.BoardId != boardId)
            {
                throw new KeyNotFoundException(
                    $"Feature with ID {featureId} not found or does not belong to board {boardId}.");
            }
        }

        /// <summary>
        /// Validates that a board is not finalized before allowing modifications.
        /// </summary>
        public void ValidateBoardNotFinalized(Board board, string operation)
        {
            if (board.IsFinalized)
            {
                throw new InvalidOperationException(
                    $"Cannot {operation} on finalized board '{board.Name}'. The board is locked for modifications.");
            }
        }

        /// <summary>
        /// Validates team member capacity for a sprint.
        /// </summary>
        public void ValidateTeamMemberCapacity(int capacity, int sprintWorkDays)
        {
            if (capacity < 0)
            {
                throw new ArgumentException($"Capacity cannot be negative. Received: {capacity}", nameof(capacity));
            }

            if (capacity > sprintWorkDays)
            {
                throw new ArgumentException(
                    $"Capacity {capacity} exceeds available sprint work days {sprintWorkDays}.",
                    nameof(capacity));
            }
        }
    }
}
