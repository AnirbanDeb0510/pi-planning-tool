using PiPlanningBackend.Models;
using PiPlanningBackend.Repositories.Interfaces;
using PiPlanningBackend.Services.Interfaces;
using PiPlanningBackend.DTOs;
using System.Security.Cryptography;
using System.Text;

namespace PiPlanningBackend.Services.Implementations
{
    public class BoardService : IBoardService
    {
        private readonly IBoardRepository _boardRepository;

        public BoardService(IBoardRepository boardRepository)
        {
            _boardRepository = boardRepository;
        }

        public async Task<Board> CreateBoardAsync(BoardCreateDto dto)
        {
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

            // 🧩 Auto-generate sprints
            // Sprint 0 is a placeholder/parking lot (no real dates)
            var sprintZero = new Sprint
            {
                Name = "Sprint 0",
                StartDate = startDateUtc,
                EndDate = startDateUtc  // Placeholder sprint has same start/end
            };
            board.Sprints.Add(sprintZero);

            // Actual sprints (1 through NumSprints)
            var currentSprintStart = startDateUtc;
            for (int i = 1; i <= dto.NumSprints; i++)
            {
                var sprint = new Sprint
                {
                    Name = $"Sprint {i}",
                    StartDate = currentSprintStart,
                    EndDate = currentSprintStart.AddDays(dto.SprintDuration - 1)
                };
                board.Sprints.Add(sprint);

                currentSprintStart = currentSprintStart.AddDays(dto.SprintDuration);
            }

            await _boardRepository.AddAsync(board);
            return board;
        }

        public async Task<Board?> GetBoardAsync(int id)
        {
            return await _boardRepository.GetByIdAsync(id);
        }

        public async Task<BoardResponseDto?> GetBoardWithHierarchyAsync(int boardId)
        {
            var board = await _boardRepository.GetBoardWithFullHierarchyAsync(boardId);
            if (board == null)
                return null;

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
                Sprints = board.Sprints
                    .OrderBy(s => s.Id)
                    .Select(s => new SprintDto
                    {
                        Id = s.Id,
                        Name = s.Name,
                        StartDate = s.StartDate,
                        EndDate = s.EndDate
                    })
                    .ToList(),
                Features = board.Features
                    .OrderBy(f => f.Priority)
                    .Select(f => new FeatureResponseDto
                    {
                        Id = f.Id,
                        Title = f.Title,
                        AzureId = f.AzureId,
                        Priority = f.Priority,
                        ValueArea = f.ValueArea,
                        UserStories = f.UserStories
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
                            })
                            .ToList()
                    })
                    .ToList(),
                TeamMembers = board.TeamMembers
                    .Select(tm => new TeamMemberResponseDto
                    {
                        Id = tm.Id,
                        Name = tm.Name,
                        IsDev = tm.IsDev,
                        IsTest = tm.IsTest,
                        SprintCapacities = tm.TeamMemberSprints
                            .Select(tms => new TeamMemberSprintDto
                            {
                                SprintId = tms.SprintId,
                                CapacityDev = tms.CapacityDev,
                                CapacityTest = tms.CapacityTest
                            })
                            .ToList()
                    })
                    .ToList()
            };
        }

        public async Task<IEnumerable<BoardSummaryDto>> SearchBoardsAsync(string? searchTerm = null, string? organization = null, string? project = null, bool? isLocked = null, bool? isFinalized = null)
        {
            var boards = await _boardRepository.SearchBoardsAsync(searchTerm, organization, project, isLocked, isFinalized);

            return boards.Select(b => new BoardSummaryDto
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
            }).ToList();
        }

        public async Task<BoardSummaryDto?> GetBoardPreviewAsync(int boardId)
        {
            var board = await _boardRepository.GetBoardWithFeaturesAsync(boardId);
            if (board == null)
                return null;

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
            var board = await _boardRepository.GetBoardWithFullHierarchyAsync(boardId);
            if (board == null)
                return (false, new List<string> { "Board not found" });

            var warnings = new List<string>();

            // Check if board is already finalized
            if (board.IsFinalized)
                return (false, new List<string> { "Board is already finalized" });

            // Warning checks (non-blocking)
            if (board.TeamMembers.Count == 0)
                warnings.Add("⚠️ No team members assigned to the board");

            if (board.Features.Count == 0)
                warnings.Add("⚠️ No features assigned to the board");

            if (board.Sprints.Count <= 1) // Sprint 0 is always present
                warnings.Add("⚠️ No planned sprints defined");

            // Check if all features have stories
            var featuresWithoutStories = board.Features.Where(f => f.UserStories.Count == 0).ToList();
            if (featuresWithoutStories.Any())
                warnings.Add($"⚠️ {featuresWithoutStories.Count} feature(s) have no user stories assigned");

            // Check team member capacity distribution (warning only)
            var teamMembersWithNoCapacity = board.TeamMembers
                .Where(tm => tm.TeamMemberSprints.Count == 0)
                .ToList();
            if (teamMembersWithNoCapacity.Any())
                warnings.Add($"⚠️ {teamMembersWithNoCapacity.Count} team member(s) have no capacity allocated");

            return (true, warnings);
        }

        public async Task<BoardSummaryDto?> FinalizeBoardAsync(int boardId)
        {
            var board = await _boardRepository.GetBoardWithFullHierarchyAsync(boardId);
            if (board == null)
                return null;

            // Set finalization flag and timestamp
            board.IsFinalized = true;
            board.FinalizedAt = DateTime.UtcNow;

            // Set OriginalSprintId = CurrentSprintId for all user stories
            foreach (var feature in board.Features)
            {
                foreach (var userStory in feature.UserStories)
                {
                    userStory.OriginalSprintId = userStory.SprintId;
                }
            }

            await _boardRepository.SaveChangesAsync();

            // Return summary board state
            return await GetBoardPreviewAsync(boardId);
        }

        public async Task<BoardSummaryDto?> RestoreBoardAsync(int boardId)
        {
            var board = await _boardRepository.GetBoardWithFullHierarchyAsync(boardId);
            if (board == null)
                return null;

            // Clear finalization flag (keep FinalizedAt for audit trail)
            board.IsFinalized = false;

            await _boardRepository.SaveChangesAsync();

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
