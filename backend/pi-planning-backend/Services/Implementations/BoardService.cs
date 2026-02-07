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
