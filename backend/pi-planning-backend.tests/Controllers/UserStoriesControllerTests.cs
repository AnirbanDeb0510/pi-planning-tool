using System.Threading;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Moq;
using PiPlanningBackend.Controllers;
using PiPlanningBackend.DTOs;
using PiPlanningBackend.Hubs;
using PiPlanningBackend.Services.Interfaces;
using Xunit;

namespace PiPlanningBackend.Tests.Controllers
{
    public class UserStoriesControllerTests
    {
        private readonly Mock<IFeatureService> _featureService = new();
        private readonly Mock<IHubContext<PlanningHub>> _hubContext = new();
        private readonly Mock<IHubClients> _hubClients = new();
        private readonly Mock<IClientProxy> _clientProxy = new();
        private readonly UserStoriesController _controller;

        public UserStoriesControllerTests()
        {
            _clientProxy
                .Setup(p => p.SendCoreAsync(It.IsAny<string>(), It.IsAny<object?[]>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            _hubClients.Setup(c => c.Group(It.IsAny<string>())).Returns(_clientProxy.Object);
            _hubContext.SetupGet(h => h.Clients).Returns(_hubClients.Object);

            _controller = new UserStoriesController(_featureService.Object, _hubContext.Object)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext()
                }
            };
        }

        [Fact]
        public async Task MoveStory_ReturnsNoContent()
        {
            MoveStoryDto dto = new() { TargetSprintId = 3 };
            _featureService.Setup(s => s.MoveUserStoryAsync(1, 10, 3)).Returns(Task.CompletedTask);

            IActionResult result = await _controller.MoveStory(1, 10, dto);

            Assert.IsType<NoContentResult>(result);
            _featureService.Verify(s => s.MoveUserStoryAsync(1, 10, 3), Times.Once);
        }

        [Fact]
        public async Task RefreshStory_WhenServiceReturnsNull_ReturnsNotFound()
        {
            _featureService
                .Setup(s => s.RefreshUserStoryFromAzureAsync(1, 10, "org", "proj", "pat"))
                .ReturnsAsync((UserStoryDto?)null);

            IActionResult result = await _controller.RefreshStory(1, 10, "org", "proj", "pat");

            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task RefreshStory_WhenServiceReturnsStory_ReturnsOk()
        {
            UserStoryDto story = new() { Id = 10, Title = "Story A", SprintId = 3 };
            _featureService
                .Setup(s => s.RefreshUserStoryFromAzureAsync(1, 10, "org", "proj", "pat"))
                .ReturnsAsync(story);

            IActionResult result = await _controller.RefreshStory(1, 10, "org", "proj", "pat");

            OkObjectResult ok = Assert.IsType<OkObjectResult>(result);
            UserStoryDto payload = Assert.IsType<UserStoryDto>(ok.Value);
            Assert.Equal(10, payload.Id);
            Assert.Equal("Story A", payload.Title);
        }
    }
}
