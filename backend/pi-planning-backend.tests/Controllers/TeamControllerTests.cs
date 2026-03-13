using System.Threading;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Moq;
using PiPlanningBackend.Controllers;
using PiPlanningBackend.DTOs;
using PiPlanningBackend.Hubs;
using PiPlanningBackend.Models;
using PiPlanningBackend.Services.Interfaces;
using Xunit;

namespace PiPlanningBackend.Tests.Controllers
{
    public class TeamControllerTests
    {
        private readonly Mock<ITeamService> _teamService = new();
        private readonly Mock<IHubContext<PlanningHub>> _hubContext = new();
        private readonly Mock<IHubClients> _hubClients = new();
        private readonly Mock<IClientProxy> _clientProxy = new();
        private readonly TeamController _controller;

        public TeamControllerTests()
        {
            _clientProxy
                .Setup(p => p.SendCoreAsync(It.IsAny<string>(), It.IsAny<object?[]>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            _hubClients.Setup(c => c.Group(It.IsAny<string>())).Returns(_clientProxy.Object);
            _hubContext.SetupGet(h => h.Clients).Returns(_hubClients.Object);

            _controller = new TeamController(_teamService.Object, _hubContext.Object);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };
        }

        [Fact]
        public async Task GetTeam_ReturnsOkWithMembers()
        {
            List<TeamMemberDto> team =
            [
                new TeamMemberDto { Id = 1, Name = "Alice", IsDev = true },
                new TeamMemberDto { Id = 2, Name = "Bob", IsTest = true }
            ];
            _teamService.Setup(s => s.GetTeamAsync(1)).ReturnsAsync(team);

            IActionResult result = await _controller.GetTeam(1);

            OkObjectResult ok = Assert.IsType<OkObjectResult>(result);
            List<TeamMemberDto> payload = Assert.IsType<List<TeamMemberDto>>(ok.Value);
            Assert.Equal(2, payload.Count);
        }

        [Fact]
        public async Task AddTeamMember_ReturnsOkWithCreatedMember()
        {
            TeamMemberDto request = new() { Name = "Alice", IsDev = true };
            TeamMemberResponseDto created = new() { Id = 10, Name = "Alice", IsDev = true, SprintCapacities = [] };
            _teamService.Setup(s => s.AddTeamMemberAsync(1, request)).ReturnsAsync(created);

            IActionResult result = await _controller.AddTeamMember(1, request);

            OkObjectResult ok = Assert.IsType<OkObjectResult>(result);
            TeamMemberResponseDto payload = Assert.IsType<TeamMemberResponseDto>(ok.Value);
            Assert.Equal(10, payload.Id);
        }

        [Fact]
        public async Task UpdateTeamMember_WhenServiceReturnsNull_ReturnsNotFound()
        {
            TeamMemberDto request = new() { Name = "Alice", IsDev = true };
            _teamService.Setup(s => s.UpdateTeamMemberAsync(1, 10, request)).ReturnsAsync((TeamMemberResponseDto?)null);

            IActionResult result = await _controller.UpdateTeamMember(1, 10, request);

            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task DeleteTeamMember_WhenServiceReturnsFalse_ReturnsNotFound()
        {
            _teamService.Setup(s => s.DeleteTeamMemberAsync(1, 10)).ReturnsAsync(false);

            IActionResult result = await _controller.DeleteTeamMember(1, 10);

            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task DeleteTeamMember_WhenServiceReturnsTrue_ReturnsNoContent()
        {
            _teamService.Setup(s => s.DeleteTeamMemberAsync(1, 10)).ReturnsAsync(true);

            IActionResult result = await _controller.DeleteTeamMember(1, 10);

            Assert.IsType<NoContentResult>(result);
        }

        [Fact]
        public async Task UpdateCapacity_WhenServiceReturnsNull_ReturnsNotFound()
        {
            UpdateTeamMemberCapacityDto dto = new() { CapacityDev = 8, CapacityTest = 2 };
            _teamService.Setup(s => s.UpdateCapacityAsync(1, 2, 10, dto)).ReturnsAsync((TeamMemberSprint?)null);

            IActionResult result = await _controller.UpdateCapacity(1, 10, 2, dto);

            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task UpdateCapacity_WhenServiceReturnsEntity_ReturnsMappedDto()
        {
            UpdateTeamMemberCapacityDto dto = new() { CapacityDev = 8, CapacityTest = 2 };
            TeamMemberSprint updated = new() { SprintId = 2, CapacityDev = 8, CapacityTest = 2, TeamMemberId = 10, TeamMember = new TeamMember(), Sprint = new Sprint() };
            _teamService.Setup(s => s.UpdateCapacityAsync(1, 2, 10, dto)).ReturnsAsync(updated);

            IActionResult result = await _controller.UpdateCapacity(1, 10, 2, dto);

            OkObjectResult ok = Assert.IsType<OkObjectResult>(result);
            TeamMemberSprintDto payload = Assert.IsType<TeamMemberSprintDto>(ok.Value);
            Assert.Equal(2, payload.SprintId);
            Assert.Equal(8, payload.CapacityDev);
            Assert.Equal(2, payload.CapacityTest);
        }
    }
}
