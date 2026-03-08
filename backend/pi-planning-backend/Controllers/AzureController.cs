using Microsoft.AspNetCore.Mvc;
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

        /// <summary>
        /// Retrieves a Feature and its child User Stories directly from Azure DevOps.
        /// </summary>
        /// <param name="organization">Azure DevOps organization.</param>
        /// <param name="project">Azure DevOps project.</param>
        /// <param name="featureId">Feature work item ID.</param>
        /// <param name="pat">Personal Access Token with vso.work scope.</param>
        /// <returns>Feature data with children stories.</returns>
        /// <response code="200">Feature retrieved successfully.</response>
        /// <response code="400">Invalid PAT or feature not found in Azure.</response>
        /// <response code="401">PAT lacks required permissions.</response>
        [HttpGet("feature/{organization}/{project}/{featureId}")]
        [ProducesResponseType(typeof(FeatureDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetFeatureWithChildren([FromRoute] string organization, [FromRoute] string project, [FromRoute] int featureId, [FromQuery][Required] string pat)
        {
            // fetch feature (expand relations)
            FeatureDto featureDto = await _azureService.GetFeatureWithChildrenAsync(organization, project, featureId, pat);

            return Ok(featureDto);
        }
    }
}
