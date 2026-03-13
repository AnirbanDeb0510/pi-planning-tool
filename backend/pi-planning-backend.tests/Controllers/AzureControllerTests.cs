using Microsoft.AspNetCore.Mvc;
using Moq;
using PiPlanningBackend.Controllers;
using PiPlanningBackend.DTOs;
using PiPlanningBackend.Services.Interfaces;
using Xunit;

namespace PiPlanningBackend.Tests.Controllers
{
    public class AzureControllerTests
    {
        private readonly Mock<IAzureBoardsService> _azureService = new();
        private readonly AzureController _controller;

        public AzureControllerTests()
        {
            _controller = new AzureController(_azureService.Object);
        }

        [Fact]
        public async Task GetFeatureWithChildren_ReturnsOkWithFeature()
        {
            FeatureDto feature = new()
            {
                Id = 42,
                Title = "Feature Alpha",
                AzureId = "AZ-42",
                Children = [new UserStoryDto { Id = 101, Title = "Story 1" }]
            };
            _azureService
                .Setup(s => s.GetFeatureWithChildrenAsync("contoso", "project-a", 42, "pat-token"))
                .ReturnsAsync(feature);

            IActionResult result = await _controller.GetFeatureWithChildren("contoso", "project-a", 42, "pat-token");

            OkObjectResult ok = Assert.IsType<OkObjectResult>(result);
            FeatureDto payload = Assert.IsType<FeatureDto>(ok.Value);
            Assert.Equal(42, payload.Id);
            Assert.Equal("Feature Alpha", payload.Title);
            Assert.Single(payload.Children!);
        }

        [Fact]
        public async Task GetFeatureWithChildren_PassesThroughRouteAndQueryValues()
        {
            _azureService
                .Setup(s => s.GetFeatureWithChildrenAsync("org", "proj", 7, "pat"))
                .ReturnsAsync(new FeatureDto { Id = 7, Title = "F", Children = [] });

            _ = await _controller.GetFeatureWithChildren("org", "proj", 7, "pat");

            _azureService.Verify(s => s.GetFeatureWithChildrenAsync("org", "proj", 7, "pat"), Times.Once);
        }
    }
}
