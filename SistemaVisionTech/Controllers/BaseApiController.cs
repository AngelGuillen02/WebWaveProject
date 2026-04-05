using Microsoft.AspNetCore.Mvc;
using SistemaVisionTech.Common;

namespace SistemaVisionTech.Controllers
{
    [ApiController]
    public abstract class BaseApiController : ControllerBase
    {
        protected IActionResult HandleResult<T>(Result<T> result)
        {
            if (result.Success)
                return Ok(result.Data);

            if (result.IsValidationError)
                return BadRequest(new { mensaje = result.Error });

            return Conflict(new { mensaje = result.Error });
        }

        protected IActionResult HandleResult(Result result)
        {
            if (result.Success)
                return NoContent();

            if (result.IsValidationError)
                return BadRequest(new { mensaje = result.Error });

            return Conflict(new { mensaje = result.Error });
        }

        protected IActionResult HandleCreatedResult<T>(
            Result<T> result, string actionName, Func<T, object> routeValues)
        {
            if (result.Success)
                return CreatedAtAction(actionName, routeValues(result.Data!), result.Data);

            if (result.IsValidationError)
                return BadRequest(new { mensaje = result.Error });

            return Conflict(new { mensaje = result.Error });
        }
    }
}
