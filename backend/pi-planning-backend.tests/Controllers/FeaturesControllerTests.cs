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
    public class FeaturesControllerTests
    {
        private readonly Mock<IFeatureService> _featureService = new();
        private readonly Mock<IHubContext<PlanningHub>> _hubContext = new();
        private readonly Mock<IHubClients> _hubClients = new();
        private readonly Mock<IClientProxy> _clientProxy = new();
        private readonly FeaturesController _controller;

        public FeaturesControllerTests()
        {
            _clientProxy
                .Setup(p => p.SendCoreAsync(It.IsAny<string>(), It.IsAny<object?[]>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            _hubClients.Setup(c => c.Group(It.IsAny<string>())).Returns(_clientProxy.Object);
            _hubContext.SetupGet(h => h.Clients).Returns(_hubClients.Object);

            _controller = new FeaturesController(_featureService.Object, _hubContext.Object);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };
        }

        [Fact]
        public async Task ImportFeature_ReturnsCreatedAtAction()
        {
            FeatureDto request = new() { Title = "Feature A", AzureId = "AZ-1", Children = [] };
            FeatureDto created = new() { Id = 10, Title = "Feature A", AzureId = "AZ-1", Children = [] };
            _featureService.Setup(s => s.ImportFeatureToBoardAsync(1, request, true)).ReturnsAsync(created);

            IActionResult result = await _controller.ImportFeature(1, request);

            CreatedAtActionResult createdResult = Assert.IsType<CreatedAtActionResult>(result);
            Assert.Equal(nameof(FeaturesController.GetFeature), createdResult.ActionName);
            FeatureDto payload = Assert.IsType<FeatureDto>(createdResult.Value);
            Assert.Equal(10, payload.Id);
        }

        [Fact]
        public void GetFeature_ReturnsOkPlaceholder()
        {
            IActionResult result = _controller.GetFeature(1, 10);

            OkObjectResult ok = Assert.IsType<OkObjectResult>(result);
            string message = Assert.IsType<string>(ok.Value);
            Assert.Contains("BoardId: 1", message);
            Assert.Contains("FeatureId: 10", message);
        }

        [Fact]
        public async Task RefreshFeature_WhenServiceReturnsNull_ReturnsNotFound()
        {
            _featureService
                .Setup(s => s.RefreshFeatureFromAzureAsync(1, 10, "org", "proj", "pat"))
                .ReturnsAsync((FeatureDto?)null);

            IActionResult result = await _controller.RefreshFeature(1, 10, "org", "proj", "pat");

            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task RefreshFeature_WhenServiceReturnsFeature_ReturnsOk()
        {
            FeatureDto refreshed = new() { Id = 10, Title = "Feature A", Children = [] };
            _featureService
                .Setup(s => s.RefreshFeatureFromAzureAsync(1, 10, "org", "proj", "pat"))
                .ReturnsAsync(refreshed);

            IActionResult result = await _controller.RefreshFeature(1, 10, "org", "proj", "pat");

            OkObjectResult ok = Assert.IsType<OkObjectResult>(result);
            FeatureDto payload = Assert.IsType<FeatureDto>(ok.Value);
            Assert.Equal(10, payload.Id);
        }

        [Fact]
        public async Task ReorderFeatures_ReturnsNoContent()
        {
            ReorderFeatureDto dto = new()
            {
                Features = [new ReorderFeatureItemDto { FeatureId = 10, NewPriority = 1 }]
            };
            _featureService.Setup(s => s.ReorderFeaturesAsync(1, dto.Features)).Returns(Task.CompletedTask);

            IActionResult result = await _controller.ReorderFeatures(1, dto);

            Assert.IsType<NoContentResult>(result);
        }

        [Fact]
        public async Task DeleteFeature_WhenDeletedFalse_ReturnsNotFound()
        {
            _featureService.Setup(s => s.DeleteFeatureAsync(1, 10)).ReturnsAsync(false);

            IActionResult result = await _controller.DeleteFeature(1, 10);

            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task DeleteFeature_WhenDeletedTrue_ReturnsNoContent()
        {
            _featureService.Setup(s => s.DeleteFeatureAsync(1, 10)).ReturnsAsync(true);

            IActionResult result = await _controller.DeleteFeature(1, 10);

            Assert.IsType<NoContentResult>(result);
        }
    }
}
