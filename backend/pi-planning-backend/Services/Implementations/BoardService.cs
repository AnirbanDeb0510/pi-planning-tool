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
                CreatedAt = DateTime.UtcNow
            };

            // 🧩 Optional password handling
            if (!string.IsNullOrEmpty(dto.Password))
            {
                board.PasswordHash = PasswordHelper.HashPassword(dto.Password);
                board.IsLocked = true;
            }

            // 🧩 Auto-generate sprints
            var startDate = DateTime.UtcNow.Date;
            for (int i = 1; i <= dto.NumSprints; i++)
            {
                var sprint = new Sprint
                {
                    Name = $"Sprint {i}",
                    StartDate = startDate,
                    EndDate = startDate.AddDays(dto.SprintDuration - 1)
                };
                board.Sprints.Add(sprint);

                startDate = startDate.AddDays(dto.SprintDuration);
            }

            await _boardRepository.AddAsync(board);
            return board;
        }

        public async Task<Board?> GetBoardAsync(int id)
        {
            return await _boardRepository.GetByIdAsync(id);
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
