using PiPlanningBackend.Models;
using PiPlanningBackend.Repositories.Interfaces;
using PiPlanningBackend.Services.Interfaces;
using PiPlanningBackend.DTOs;
using System.Security.Cryptography;
using System.Text;

namespace PiPlanningBackend.Services.Implementations
{
    public class BoardService(IBoardRepository boardRepository, ISprintService sprintService, IValidationService validationService, ILogger<BoardService> logger, ICorrelationIdProvider correlationIdProvider) : IBoardService
    {
        private readonly IBoardRepository _boardRepository = boardRepository;
        private readonly ISprintService _sprintService = sprintService;
        private readonly IValidationService _validationService = validationService;
        private readonly ILogger<BoardService> _logger = logger;
        private readonly ICorrelationIdProvider _correlationIdProvider = correlationIdProvider;

        public async Task<Board> CreateBoardAsync(BoardCreateDto dto)
        {
            var correlationId = _correlationIdProvider.GetCorrelationId();
            _logger.LogInformation(
                "Board creation started | CorrelationId: {CorrelationId} | Name: {BoardName} | Organization: {Organization} | Project: {Project} | NumSprints: {NumSprints}",
                correlationId, dto.Name, dto.Organization, dto.Project, dto.NumSprints);

            // Ensure StartDate is UTC-aware for PostgreSQL
            var startDateUtc = DateTime.SpecifyKind(dto.StartDate, DateTimeKind.Utc);

            var board = new Board
            {
                Name = dto.Name,
                Organization = dto.Organization,
                Project = dto.Project,
                AzureStoryPointField = dto.AzureStoryPointField,
                AzureDevStoryPointField = dto.AzureDevStoryPointField,
                AzureTestStoryPointField = dto.AzureTestStoryPointField,
                NumSprints = dto.NumSprints,
                SprintDuration = dto.SprintDuration,
                DevTestToggle = dto.DevTestToggle,
                CreatedAt = DateTime.UtcNow,
                StartDate = startDateUtc
            };

            // 🧩 Optional password handling
            if (!string.IsNullOrEmpty(dto.Password))
            {
                board.PasswordHash = PasswordHelper.HashPassword(dto.Password);
                board.IsLocked = true;
            }

            // 🧩 Auto-generate sprints using SprintService
            var sprints = _sprintService.GenerateSprintsForBoard(board, dto.NumSprints, dto.SprintDuration);
            foreach (var sprint in sprints)
            {
                board.Sprints.Add(sprint);
            }

            await _boardRepository.AddAsync(board);
            _logger.LogInformation(
                "Board created successfully | CorrelationId: {CorrelationId} | BoardId: {BoardId} | SprintCount: {SprintCount}",
                correlationId, board.Id, board.Sprints.Count);
            return board;
        }

        public async Task<Board?> GetBoardAsync(int id)
        {
            var correlationId = _correlationIdProvider.GetCorrelationId();
            await _validationService.ValidateBoardExists(id);
            var board = await _boardRepository.GetByIdAsync(id)
                ?? throw new KeyNotFoundException($"Board with ID {id} not found.");
            _logger.LogInformation(
                "Board retrieved | CorrelationId: {CorrelationId} | BoardId: {BoardId} | Name: {BoardName}",
                correlationId, board.Id, board.Name);
            return board;
        }

        public async Task<BoardResponseDto?> GetBoardWithHierarchyAsync(int boardId)
        {
            var correlationId = _correlationIdProvider.GetCorrelationId();
            _logger.LogInformation(
                "Get board with hierarchy started | CorrelationId: {CorrelationId} | BoardId: {BoardId}",
                correlationId, boardId);

            await _validationService.ValidateBoardExists(boardId);
            var board = await _boardRepository.GetBoardWithFullHierarchyAsync(boardId)
                ?? throw new KeyNotFoundException($"Board with ID {boardId} not found.");

            _logger.LogInformation(
                "Board hierarchy retrieved | CorrelationId: {CorrelationId} | BoardId: {BoardId} | Name: {BoardName} | Features: {FeatureCount} | Teams: {TeamCount} | Sprints: {SprintCount}",
                correlationId, board.Id, board.Name, board.Features.Count, board.TeamMembers.Count, board.Sprints.Count);

            return new BoardResponseDto
            {
                Id = board.Id,
                Name = board.Name,
                Organization = board.Organization,
                Project = board.Project,
                IsLocked = board.IsLocked,
                IsFinalized = board.IsFinalized,
                DevTestToggle = board.DevTestToggle,
                StartDate = board.StartDate,
                Sprints = [.. board.Sprints
                    .OrderBy(s => s.Id)
                    .Select(s => new SprintDto
                    {
                        Id = s.Id,
                        Name = s.Name,
                        StartDate = s.StartDate,
                        EndDate = s.EndDate
                    })],
                Features = [.. board.Features
                    .OrderBy(f => f.Priority)
                    .Select(f => new FeatureResponseDto
                    {
                        Id = f.Id,
                        Title = f.Title,
                        AzureId = f.AzureId,
                        Priority = f.Priority,
                        ValueArea = f.ValueArea,
                        UserStories = [.. f.UserStories
                            .Select(us => new UserStoryDto
                            {
                                Id = us.Id,
                                Title = us.Title,
                                AzureId = us.AzureId,
                                StoryPoints = us.StoryPoints,
                                DevStoryPoints = us.DevStoryPoints,
                                TestStoryPoints = us.TestStoryPoints,
                                SprintId = us.SprintId,
                                OriginalSprintId = us.OriginalSprintId,
                                IsMoved = us.IsMoved
                            })]
                    })],
                TeamMembers = [.. board.TeamMembers
                    .Select(tm => new TeamMemberResponseDto
                    {
                        Id = tm.Id,
                        Name = tm.Name,
                        IsDev = tm.IsDev,
                        IsTest = tm.IsTest,
                        SprintCapacities = [.. tm.TeamMemberSprints
                            .Select(tms => new TeamMemberSprintDto
                            {
                                SprintId = tms.SprintId,
                                CapacityDev = tms.CapacityDev,
                                CapacityTest = tms.CapacityTest
                            })]
                    })]
            };
        }

        public async Task<IEnumerable<BoardSummaryDto>> SearchBoardsAsync(string? searchTerm = null, string? organization = null, string? project = null, bool? isLocked = null, bool? isFinalized = null)
        {
            var correlationId = _correlationIdProvider.GetCorrelationId();
            _logger.LogInformation(
                "Board search started | CorrelationId: {CorrelationId} | SearchTerm: {SearchTerm} | Organization: {Organization} | Project: {Project} | IsLocked: {IsLocked} | IsFinalized: {IsFinalized}",
                correlationId, searchTerm ?? "<null>", organization ?? "<null>", project ?? "<null>", isLocked, isFinalized);

            var boards = await _boardRepository.SearchBoardsAsync(searchTerm, organization, project, isLocked, isFinalized);

            _logger.LogInformation(
                "Boards search completed | CorrelationId: {CorrelationId} | ResultCount: {ResultCount}",
                correlationId, boards.Count());

            return [.. boards.Select(b => new BoardSummaryDto
            {
                Id = b.Id,
                Name = b.Name,
                Organization = b.Organization,
                Project = b.Project,
                CreatedAt = b.CreatedAt,
                IsLocked = b.IsLocked,
                IsFinalized = b.IsFinalized,
                SprintCount = b.Sprints.Count,
                FeatureCount = b.Features.Count
            })];
        }

        public async Task<BoardSummaryDto?> GetBoardPreviewAsync(int boardId)
        {
            var correlationId = _correlationIdProvider.GetCorrelationId();
            _logger.LogInformation(
                "Board preview retrieval started | CorrelationId: {CorrelationId} | BoardId: {BoardId}",
                correlationId, boardId);

            await _validationService.ValidateBoardExists(boardId);
            var board = await _boardRepository.GetBoardWithFeaturesAsync(boardId)
                ?? throw new KeyNotFoundException($"Board with ID {boardId} not found.");

            _logger.LogInformation(
                "Board preview retrieved | CorrelationId: {CorrelationId} | BoardId: {BoardId} | Name: {BoardName} | FeatureCount: {FeatureCount}",
                correlationId, board.Id, board.Name, board.Features.Count);

            return new BoardSummaryDto
            {
                Id = board.Id,
                Name = board.Name,
                Organization = board.Organization,
                Project = board.Project,
                CreatedAt = board.CreatedAt,
                IsLocked = board.IsLocked,
                IsFinalized = board.IsFinalized,
                SprintCount = 0,  // Not needed for preview
                FeatureCount = board.Features.Count,
                SampleFeatureAzureId = board.Features.Count > 0 ? board.Features.First().AzureId : null
            };
        }

        public async Task<(bool Success, List<string> Warnings)> ValidateBoardForFinalizationAsync(int boardId)
        {
            var correlationId = _correlationIdProvider.GetCorrelationId();
            _logger.LogInformation(
                "Board finalization validation started | CorrelationId: {CorrelationId} | BoardId: {BoardId}",
                correlationId, boardId);

            await _validationService.ValidateBoardExists(boardId);
            var board = await _boardRepository.GetBoardWithFullHierarchyAsync(boardId)
                ?? throw new KeyNotFoundException($"Board with ID {boardId} not found.");

            var warnings = new List<string>();

            // Check if board is already finalized
            if (board.IsFinalized)
            {
                _logger.LogInformation(
                    "Board finalization validation failed - already finalized | CorrelationId: {CorrelationId} | BoardId: {BoardId}",
                    correlationId, boardId);
                return (false, new List<string> { "Board is already finalized" });
            }

            // Warning checks (non-blocking)
            if (board.TeamMembers.Count == 0)
                warnings.Add("⚠️ No team members assigned to the board");

            if (board.Features.Count == 0)
                warnings.Add("⚠️ No features assigned to the board");

            if (board.Sprints.Count <= 1) // Sprint 0 is always present
                warnings.Add("⚠️ No planned sprints defined");

            // Check if all features have stories
            var featuresWithoutStories = board.Features.Where(f => f.UserStories.Count == 0).ToList();
            if (featuresWithoutStories.Count != 0)
                warnings.Add($"⚠️ {featuresWithoutStories.Count} feature(s) have no user stories assigned");

            // Check team member capacity distribution (warning only)
            var teamMembersWithNoCapacity = board.TeamMembers
                .Where(tm => tm.TeamMemberSprints.Count == 0)
                .ToList();
            if (teamMembersWithNoCapacity.Count != 0)
                warnings.Add($"⚠️ {teamMembersWithNoCapacity.Count} team member(s) have no capacity allocated");

            _logger.LogInformation(
                "Board finalization validation completed | CorrelationId: {CorrelationId} | BoardId: {BoardId} | Success: true | WarningCount: {WarningCount}",
                correlationId, boardId, warnings.Count);

            return (true, warnings);
        }

        public async Task<BoardSummaryDto?> FinalizeBoardAsync(int boardId)
        {
            var correlationId = _correlationIdProvider.GetCorrelationId();
            _logger.LogInformation(
                "Board finalization started | CorrelationId: {CorrelationId} | BoardId: {BoardId}",
                correlationId, boardId);

            await _validationService.ValidateBoardExists(boardId);
            var board = await _boardRepository.GetBoardWithFullHierarchyAsync(boardId)
                ?? throw new KeyNotFoundException($"Board with ID {boardId} not found.");

            // Set finalization flag and timestamp
            board.IsFinalized = true;
            board.FinalizedAt = DateTime.UtcNow;

            // Set OriginalSprintId = CurrentSprintId for all user stories
            var storyCount = 0;
            foreach (var feature in board.Features)
            {
                foreach (var userStory in feature.UserStories)
                {
                    userStory.OriginalSprintId = userStory.SprintId;
                    storyCount++;
                }
            }

            await _boardRepository.SaveChangesAsync();
            _logger.LogInformation(
                "Board finalized successfully | CorrelationId: {CorrelationId} | BoardId: {BoardId} | Name: {BoardName} | FeatureCount: {FeatureCount} | StoryCount: {StoryCount}",
                correlationId, board.Id, board.Name, board.Features.Count, storyCount);

            // Return summary board state
            return await GetBoardPreviewAsync(boardId);
        }

        public async Task<BoardSummaryDto?> RestoreBoardAsync(int boardId)
        {
            var correlationId = _correlationIdProvider.GetCorrelationId();
            _logger.LogInformation(
                "Board restoration started | CorrelationId: {CorrelationId} | BoardId: {BoardId}",
                correlationId, boardId);

            await _validationService.ValidateBoardExists(boardId);
            var board = await _boardRepository.GetBoardWithFullHierarchyAsync(boardId)
                ?? throw new KeyNotFoundException($"Board with ID {boardId} not found.");

            // Clear finalization flag (keep FinalizedAt for audit trail)
            board.IsFinalized = false;

            await _boardRepository.SaveChangesAsync();
            _logger.LogInformation(
                "Board restored successfully | CorrelationId: {CorrelationId} | BoardId: {BoardId} | Name: {BoardName}",
                correlationId, board.Id, board.Name);

            // Return summary board state
            return await GetBoardPreviewAsync(boardId);
        }

    }

    public static class PasswordHelper
    {
        public static string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(password);
            var hash = sha256.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }

        public static bool VerifyPassword(string password, string storedHash)
        {
            using var sha256 = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(password);
            var hash = sha256.ComputeHash(bytes);
            var computedHash = Convert.ToBase64String(hash);
            return storedHash == computedHash;
        }
    }
}
