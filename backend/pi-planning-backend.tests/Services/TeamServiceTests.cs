using Microsoft.Extensions.Logging;
using Moq;
using PiPlanningBackend.DTOs;
using PiPlanningBackend.Models;
using PiPlanningBackend.Repositories.Interfaces;
using PiPlanningBackend.Services.Implementations;
using PiPlanningBackend.Services.Interfaces;
using Xunit;

namespace PiPlanningBackend.Tests.Services
{
    public class TeamServiceTests
    {
        private readonly Mock<ITeamRepository> _teamRepository = new();
        private readonly Mock<IBoardRepository> _boardRepository = new();
        private readonly Mock<IValidationService> _validationService = new();
        private readonly Mock<ILogger<TeamService>> _logger = new();
        private readonly Mock<ICorrelationIdProvider> _correlationIdProvider = new();
        private readonly Mock<ITransactionService> _transactionService = new();
        private readonly TeamService _service;

        public TeamServiceTests()
        {
            _correlationIdProvider.Setup(x => x.GetCorrelationId()).Returns("corr-123");

            // Pass the lambda through for TeamMemberResponseDto-returning transactions
            _transactionService
                .Setup(t => t.ExecuteInTransactionAsync(It.IsAny<Func<Task<TeamMemberResponseDto>>>()))
                .Returns<Func<Task<TeamMemberResponseDto>>>(fn => fn());

            _service = new TeamService(
                _teamRepository.Object,
                _boardRepository.Object,
                _validationService.Object,
                _logger.Object,
                _correlationIdProvider.Object,
                _transactionService.Object);
        }

        private static TeamMemberDto CreateMemberDto(string name = "Alice", bool isDev = true, bool isTest = false) => new()
        {
            Name = name,
            IsDev = isDev,
            IsTest = isTest
        };

        private static Board CreateBoardWithSprints(bool devTestToggle = false)
        {
            Sprint sprint1 = new()
            {
                Id = 2,
                Name = "Sprint 1",
                StartDate = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc),
                EndDate = new DateTime(2026, 3, 14, 0, 0, 0, DateTimeKind.Utc)
            };
            Sprint sprint2 = new()
            {
                Id = 3,
                Name = "Sprint 2",
                StartDate = new DateTime(2026, 3, 15, 0, 0, 0, DateTimeKind.Utc),
                EndDate = new DateTime(2026, 3, 28, 0, 0, 0, DateTimeKind.Utc)
            };
            return new Board { Id = 1, Name = "PI Board", DevTestToggle = devTestToggle, Sprints = [sprint1, sprint2] };
        }

        // ── GetTeamAsync ──────────────────────────────────────────────────────────

        [Fact]
        public async Task GetTeamAsync_ReturnsMappedTeamMemberDtos()
        {
            List<TeamMember> members =
            [
                new() { Id = 1, Name = "Alice", IsDev = true, IsTest = false },
                new() { Id = 2, Name = "Bob", IsDev = false, IsTest = true }
            ];
            _validationService.Setup(v => v.ValidateBoardExists(1)).Returns(Task.CompletedTask);
            _teamRepository.Setup(r => r.GetTeamAsync(1)).ReturnsAsync(members);

            List<TeamMemberDto> result = await _service.GetTeamAsync(1);

            Assert.Equal(2, result.Count);
            Assert.Equal("Alice", result[0].Name);
            Assert.True(result[0].IsDev);
            Assert.Equal("Bob", result[1].Name);
            Assert.True(result[1].IsTest);
        }

        // ── AddTeamMemberAsync ────────────────────────────────────────────────────

        [Fact]
        public async Task AddTeamMemberAsync_WhenNameEmpty_ThrowsArgumentException()
        {
            TeamMemberDto dto = CreateMemberDto(name: "");

            await Assert.ThrowsAsync<ArgumentException>(() => _service.AddTeamMemberAsync(1, dto));
        }

        [Fact]
        public async Task AddTeamMemberAsync_WhenNoRole_ThrowsArgumentException()
        {
            TeamMemberDto dto = CreateMemberDto(isDev: false, isTest: false);

            await Assert.ThrowsAsync<ArgumentException>(() => _service.AddTeamMemberAsync(1, dto));
        }

        [Fact]
        public async Task AddTeamMemberAsync_HappyPath_ReturnsMemberWithSprintCapacitiesForEachSprint()
        {
            Board board = CreateBoardWithSprints();
            _validationService.Setup(v => v.ValidateBoardExists(1)).Returns(Task.CompletedTask);
            _boardRepository.Setup(r => r.GetBoardWithSprintsAsync(1)).ReturnsAsync(board);
            _teamRepository.Setup(r => r.AddTeamMemberAsync(It.IsAny<TeamMember>())).Returns(Task.CompletedTask);
            _teamRepository.Setup(r => r.AddTeamMemberSprintAsync(It.IsAny<TeamMemberSprint>())).Returns(Task.CompletedTask);
            _teamRepository.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

            TeamMemberResponseDto result = await _service.AddTeamMemberAsync(1, CreateMemberDto());

            Assert.Equal("Alice", result.Name);
            Assert.True(result.IsDev);
            Assert.Equal(2, result.SprintCapacities.Count); // one entry per sprint
        }

        [Fact]
        public async Task AddTeamMemberAsync_WithDevTestToggle_DevMemberGetsDevCapacityOnly()
        {
            // 14-day sprint → 10 working days capacity
            Board board = CreateBoardWithSprints(devTestToggle: true);
            _validationService.Setup(v => v.ValidateBoardExists(1)).Returns(Task.CompletedTask);
            _boardRepository.Setup(r => r.GetBoardWithSprintsAsync(1)).ReturnsAsync(board);
            _teamRepository.Setup(r => r.AddTeamMemberAsync(It.IsAny<TeamMember>())).Returns(Task.CompletedTask);
            _teamRepository.Setup(r => r.AddTeamMemberSprintAsync(It.IsAny<TeamMemberSprint>())).Returns(Task.CompletedTask);
            _teamRepository.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

            TeamMemberResponseDto result = await _service.AddTeamMemberAsync(1, CreateMemberDto(isDev: true, isTest: false));

            // Dev toggle ON + isDev only → CapacityDev = workingDays, CapacityTest = 0
            Assert.All(result.SprintCapacities, c =>
            {
                Assert.True(c.CapacityDev > 0);
                Assert.Equal(0, c.CapacityTest);
            });
        }

        [Fact]
        public async Task AddTeamMemberAsync_WithDevTestToggle_TestMemberGetsTestCapacityOnly()
        {
            Board board = CreateBoardWithSprints(devTestToggle: true);
            _validationService.Setup(v => v.ValidateBoardExists(1)).Returns(Task.CompletedTask);
            _boardRepository.Setup(r => r.GetBoardWithSprintsAsync(1)).ReturnsAsync(board);
            _teamRepository.Setup(r => r.AddTeamMemberAsync(It.IsAny<TeamMember>())).Returns(Task.CompletedTask);
            _teamRepository.Setup(r => r.AddTeamMemberSprintAsync(It.IsAny<TeamMemberSprint>())).Returns(Task.CompletedTask);
            _teamRepository.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

            TeamMemberResponseDto result = await _service.AddTeamMemberAsync(1, CreateMemberDto(isDev: false, isTest: true));

            // Dev toggle ON + isTest only → CapacityDev = 0, CapacityTest = workingDays
            Assert.All(result.SprintCapacities, c =>
            {
                Assert.Equal(0, c.CapacityDev);
                Assert.True(c.CapacityTest > 0);
            });
        }

        // ── UpdateTeamMemberAsync ─────────────────────────────────────────────────

        [Fact]
        public async Task UpdateTeamMemberAsync_WhenNameEmpty_ThrowsArgumentException()
        {
            TeamMemberDto dto = CreateMemberDto(name: "");

            await Assert.ThrowsAsync<ArgumentException>(() => _service.UpdateTeamMemberAsync(1, 5, dto));
        }

        [Fact]
        public async Task UpdateTeamMemberAsync_WhenNoRole_ThrowsArgumentException()
        {
            TeamMemberDto dto = CreateMemberDto(isDev: false, isTest: false);

            await Assert.ThrowsAsync<ArgumentException>(() => _service.UpdateTeamMemberAsync(1, 5, dto));
        }

        // ── DeleteTeamMemberAsync ─────────────────────────────────────────────────

        [Fact]
        public async Task DeleteTeamMemberAsync_WhenMemberExists_ReturnsTrue()
        {
            TeamMember member = new() { Id = 5, BoardId = 1, Name = "Alice" };
            Board board = new() { Id = 1, Name = "PI Board" };

            _validationService.Setup(v => v.ValidateTeamMemberBelongsToBoard(5, 1)).Returns(Task.CompletedTask);
            _teamRepository.Setup(r => r.GetTeamMemberAsync(5)).ReturnsAsync(member);
            _validationService.Setup(v => v.ValidateBoardExists(1)).Returns(Task.CompletedTask);
            _boardRepository.Setup(r => r.GetBoardWithSprintsAsync(1)).ReturnsAsync(board);
            _teamRepository.Setup(r => r.DeleteTeamMemberAsync(member)).Returns(Task.CompletedTask);
            _teamRepository.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

            bool result = await _service.DeleteTeamMemberAsync(1, 5);

            Assert.True(result);
        }

        [Fact]
        public async Task UpdateCapacityAsync_WhenBoardLocked_ThrowsUnauthorizedAccessException()
        {
            Sprint sprint = new()
            {
                Id = 2,
                StartDate = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc),
                EndDate = new DateTime(2026, 3, 14, 0, 0, 0, DateTimeKind.Utc)
            };
            Board board = new() { Id = 1, Name = "PI Board", IsLocked = true, IsFinalized = false };
            TeamMember member = new() { Id = 5, BoardId = 1, Name = "Alice", IsDev = true, IsTest = false };
            TeamMemberSprint tms = new()
            {
                TeamMemberId = 5,
                TeamMember = member,
                SprintId = 2,
                Sprint = sprint,
                CapacityDev = 5,
                CapacityTest = 0
            };

            sprint.Board = board;

            UpdateTeamMemberCapacityDto dto = new() { CapacityDev = 6, CapacityTest = 0 };

            _validationService.Setup(v => v.ValidateTeamMemberBelongsToBoard(5, 1)).Returns(Task.CompletedTask);
            _validationService.Setup(v => v.ValidateSprintBelongsToBoard(2, 1)).Returns(Task.CompletedTask);
            _teamRepository.Setup(r => r.GetTeamMemberSprintAsync(2, 5)).ReturnsAsync(tms);
            _validationService.Setup(v => v.ValidateBoardExists(1)).Returns(Task.CompletedTask);
            _boardRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(board);
            _validationService
                .Setup(v => v.ValidateBoardNotLocked(board, It.IsAny<string>()))
                .Throws<UnauthorizedAccessException>();

            await Assert.ThrowsAsync<UnauthorizedAccessException>(
                () => _service.UpdateCapacityAsync(1, 2, 5, dto));
        }

        [Fact]
        public async Task UpdateCapacityAsync_WhenBoardFinalized_AllowsCapacityUpdate()
        {
            Sprint sprint = new()
            {
                Id = 2,
                StartDate = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc),
                EndDate = new DateTime(2026, 3, 14, 0, 0, 0, DateTimeKind.Utc)
            };
            Board board = new() { Id = 1, Name = "PI Board", IsLocked = false, IsFinalized = true, DevTestToggle = true };
            TeamMember member = new() { Id = 5, BoardId = 1, Name = "Alice", IsDev = true, IsTest = false };
            TeamMemberSprint tms = new()
            {
                TeamMemberId = 5,
                TeamMember = member,
                SprintId = 2,
                Sprint = sprint,
                CapacityDev = 5,
                CapacityTest = 0
            };

            sprint.Board = board;

            UpdateTeamMemberCapacityDto dto = new() { CapacityDev = 8, CapacityTest = 0 };

            _validationService.Setup(v => v.ValidateTeamMemberBelongsToBoard(5, 1)).Returns(Task.CompletedTask);
            _validationService.Setup(v => v.ValidateSprintBelongsToBoard(2, 1)).Returns(Task.CompletedTask);
            _teamRepository.Setup(r => r.GetTeamMemberSprintAsync(2, 5)).ReturnsAsync(tms);
            _validationService.Setup(v => v.ValidateBoardExists(1)).Returns(Task.CompletedTask);
            _boardRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(board);
            _teamRepository.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

            TeamMemberSprint? result = await _service.UpdateCapacityAsync(1, 2, 5, dto);

            Assert.NotNull(result);
            Assert.Equal(8, result!.CapacityDev);
            Assert.Equal(0, result.CapacityTest);
            _validationService.Verify(v => v.ValidateBoardNotLocked(board, It.IsAny<string>()), Times.Once);
            _validationService.Verify(v => v.ValidateBoardNotFinalized(It.IsAny<Board>(), It.IsAny<string>()), Times.Never);
        }
    }
}
