using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace PiPlanningBackend.Filters
{
    /// <summary>
    /// Automatically validates ModelState for all API requests with [FromBody] parameters.
    /// Returns 400 BadRequest with field-level validation errors without requiring
    /// explicit "if (!ModelState.IsValid)" checks in every controller method.
    /// 
    /// Applied via [ValidateModelState] attribute on controller or method.
    /// Or register globally in Program.cs to apply to all controllers.
    /// </summary>
    public class ValidateModelStateFilter : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            if (!context.ModelState.IsValid)
            {
                // Create standardized validation error response
                var errors = context.ModelState
                    .Where(x => x.Value?.Errors.Count > 0)
                    .ToDictionary(
                        kvp => kvp.Key,
                        kvp => kvp.Value?.Errors.Select(e => e.ErrorMessage).ToArray() ?? Array.Empty<string>()
                    );

                var errorResponse = new
                {
                    error = new
                    {
                        message = "Validation failed",
                        details = "One or more validation errors occurred",
                        errors = errors,
                        timestamp = DateTime.UtcNow
                    }
                };

                context.Result = new BadRequestObjectResult(errorResponse);
            }

            base.OnActionExecuting(context);
        }
    }
}
