using Microsoft.Extensions.Logging;
using Moq;
using PiPlanningBackend.Models;
using PiPlanningBackend.Services.Implementations;
using PiPlanningBackend.Services.Interfaces;
using Xunit;

namespace PiPlanningBackend.Tests.Services
{
    public class SprintServiceTests
    {
        private readonly SprintService _service;
        private readonly DateTime _startDate = new(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc);

        public SprintServiceTests()
        {
            Mock<ILogger<SprintService>> logger = new();
            Mock<ICorrelationIdProvider> correlationIdProvider = new();
            correlationIdProvider.Setup(x => x.GetCorrelationId()).Returns("corr-123");

            _service = new SprintService(logger.Object, correlationIdProvider.Object);
        }

        private Board CreateBoard() => new() { Id = 1, Name = "PI Board", StartDate = _startDate };

        [Fact]
        public void GenerateSprintsForBoard_CreatesSprintZeroAndConfiguredSprints()
        {
            Board board = CreateBoard();

            List<Sprint> sprints = _service.GenerateSprintsForBoard(board, 3, 14);

            Assert.Equal(4, sprints.Count);
            Assert.Equal("Sprint 0", sprints[0].Name);
            Assert.Equal(_startDate, sprints[0].StartDate);
            Assert.Equal(_startDate, sprints[0].EndDate);

            Assert.Equal("Sprint 1", sprints[1].Name);
            Assert.Equal(_startDate, sprints[1].StartDate);
            Assert.Equal(_startDate.AddDays(13), sprints[1].EndDate);

            Assert.Equal("Sprint 2", sprints[2].Name);
            Assert.Equal(_startDate.AddDays(14), sprints[2].StartDate);
            Assert.Equal(_startDate.AddDays(27), sprints[2].EndDate);

            Assert.Equal("Sprint 3", sprints[3].Name);
            Assert.Equal(_startDate.AddDays(28), sprints[3].StartDate);
            Assert.Equal(_startDate.AddDays(41), sprints[3].EndDate);
        }

        [Fact]
        public void GenerateSprintsForBoard_ZeroSprints_ReturnsOnlySprintZero()
        {
            Board board = CreateBoard();

            List<Sprint> sprints = _service.GenerateSprintsForBoard(board, 0, 14);

            Assert.Single(sprints);
            Assert.Equal("Sprint 0", sprints[0].Name);
            Assert.Equal(_startDate, sprints[0].StartDate);
            Assert.Equal(_startDate, sprints[0].EndDate);
        }

        [Fact]
        public void GenerateSprintsForBoard_OneSprint_ReturnsSprintZeroAndOneSprint()
        {
            Board board = CreateBoard();

            List<Sprint> sprints = _service.GenerateSprintsForBoard(board, 1, 14);

            Assert.Equal(2, sprints.Count);
            Assert.Equal("Sprint 0", sprints[0].Name);
            Assert.Equal(_startDate, sprints[0].StartDate);
            Assert.Equal(_startDate, sprints[0].EndDate);
            Assert.Equal("Sprint 1", sprints[1].Name);
            Assert.Equal(_startDate, sprints[1].StartDate);
            Assert.Equal(_startDate.AddDays(13), sprints[1].EndDate);
        }

        [Fact]
        public void GenerateSprintsForBoard_SingleDayDuration_EachSprintStartsAndEndsOnSameDay()
        {
            Board board = CreateBoard();

            List<Sprint> sprints = _service.GenerateSprintsForBoard(board, 2, 1);

            Assert.Equal(3, sprints.Count);
            // Sprint 0 placeholder
            Assert.Equal(_startDate, sprints[0].StartDate);
            Assert.Equal(_startDate, sprints[0].EndDate);
            // Sprint 1: duration 1 day → StartDate == EndDate
            Assert.Equal(_startDate, sprints[1].StartDate);
            Assert.Equal(_startDate, sprints[1].EndDate);
            // Sprint 2: starts next day, also single-day
            Assert.Equal(_startDate.AddDays(1), sprints[2].StartDate);
            Assert.Equal(_startDate.AddDays(1), sprints[2].EndDate);
        }
    }
}
