using Microsoft.AspNetCore.Mvc;
using PiPlanningBackend.Services;
using PiPlanningBackend.DTOs;
using PiPlanningBackend.Services.Interfaces;
using System.ComponentModel.DataAnnotations;

namespace PiPlanningBackend.Controllers
{
    [ApiController]
    [Route("api/v1/azure")]
    public class AzureController(IAzureBoardsService azureService) : ControllerBase
    {
        private readonly IAzureBoardsService _azureService = azureService;

        // GET api/v1/azure/feature/{organization}/{project}/{featureId}?pat=xxxx
        [HttpGet("feature/{organization}/{project}/{featureId}")]
        public async Task<IActionResult> GetFeatureWithChildren(string organization, string project, int featureId, [FromQuery][Required] string pat)
        {
            // fetch feature (expand relations)
            FeatureDto featureDto = await _azureService.GetFeatureWithChildrenAsync(organization, project, featureId, pat);

            return Ok(featureDto);
        }


    }
}
