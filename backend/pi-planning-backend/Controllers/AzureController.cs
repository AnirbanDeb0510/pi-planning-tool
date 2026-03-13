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
        /// <param name="storyPointField">Optional Azure field name for story points.</param>
        /// <param name="devField">Optional Azure field name for dev points.</param>
        /// <param name="testField">Optional Azure field name for test points.</param>
        /// <returns>Feature data with children stories.</returns>
        /// <response code="200">Feature retrieved successfully.</response>
        /// <response code="400">Invalid PAT or feature not found in Azure.</response>
        /// <response code="401">PAT lacks required permissions.</response>
        [HttpGet("feature/{organization}/{project}/{featureId}")]
        [ProducesResponseType(typeof(FeatureDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetFeatureWithChildren(
            [FromRoute] string organization,
            [FromRoute] string project,
            [FromRoute] int featureId,
            [FromQuery][Required] string pat,
            [FromQuery] string? storyPointField = null,
            [FromQuery] string? devField = null,
            [FromQuery] string? testField = null)
        {
            // fetch feature (expand relations)
            FeatureDto featureDto = await _azureService.GetFeatureWithChildrenAsync(
                organization,
                project,
                featureId,
                pat,
                storyPointField,
                devField,
                testField);

            return Ok(featureDto);
        }

        /// <summary>
        /// Retrieves a Feature and its child User Stories from Azure DevOps using board-level Azure configuration.
        /// </summary>
        /// <param name="boardId">Board ID that contains organization/project and field mappings.</param>
        /// <param name="featureId">Feature work item ID.</param>
        /// <param name="pat">Personal Access Token with vso.work scope.</param>
        /// <returns>Feature data with children stories.</returns>
        /// <response code="200">Feature retrieved successfully.</response>
        /// <response code="400">Board is missing Azure organization/project configuration.</response>
        /// <response code="404">Board not found.</response>
        [HttpGet("boards/{boardId:int}/feature/{featureId:int}")]
        [ProducesResponseType(typeof(FeatureDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetFeatureWithChildrenForBoard(
            [FromRoute] int boardId,
            [FromRoute] int featureId,
            [FromQuery][Required] string pat)
        {
            FeatureDto featureDto = await _azureService.GetFeatureWithChildrenForBoardAsync(boardId, featureId, pat);

            return Ok(featureDto);
        }
    }
}
