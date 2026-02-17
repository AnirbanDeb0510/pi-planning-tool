using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using PiPlanningBackend.DTOs;
using PiPlanningBackend.Services.Interfaces;

namespace PiPlanningBackend.Controllers
{
    [ApiController]
    [Route("api/v1/boards/{boardId}/features")]
    public class FeaturesController(IFeatureService featureService) : ControllerBase
    {
        private readonly IFeatureService _featureService = featureService;

        // POST api/v1/boards/{boardId}/features/import
        [HttpPost("import")]
        public async Task<IActionResult> ImportFeature(int boardId, [FromBody] FeatureDto dto)
        {
            // ModelState validation handled globally by ValidateModelStateFilter
            var created = await _featureService.ImportFeatureToBoardAsync(boardId, dto);
            return CreatedAtAction(nameof(GetFeature), new { boardId, id = created.Id }, created);
        }

        [HttpGet("{id}")]
        public IActionResult GetFeature(int boardId, int id)
        {
            // optional: implement retrieval controller or use other endpoint
            return Ok();
        }

        // PATCH api/boards/{boardId}/features/{id}/refresh?org=&project=&pat=
        [HttpPatch("{id}/refresh")]
        public async Task<IActionResult> RefreshFeature(int boardId, int id, [FromQuery] string organization, [FromQuery] string project, [FromQuery] string pat)
        {
            var f = await _featureService.RefreshFeatureFromAzureAsync(boardId, id, organization, project, pat);
            if (f == null) return NotFound();
            return Ok(f);
        }

        // PATCH api/boards/{boardId}/features/reorder
        [HttpPatch("reorder")]
        public async Task<IActionResult> ReorderFeatures(int boardId, [FromBody] ReorderFeatureDto dto)
        {
            // ModelState validation handled globally by ValidateModelStateFilter
            await _featureService.ReorderFeaturesAsync(boardId, dto.Features);
            return NoContent();
        }

        // DELETE api/boards/{boardId}/features/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteFeature(int boardId, int id)
        {
            var deleted = await _featureService.DeleteFeatureAsync(boardId, id);
            if (!deleted) return NotFound();
            return NoContent();
        }
    }
}
