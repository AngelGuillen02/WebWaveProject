using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace SistemaVisionTech.Controllers.Acceso
{
    [Route("api/[controller]")]
    [ApiController]
    public class AccesoController : ControllerBase
    {
        [HttpGet("Usuarios")]
        public IActionResult ObtenerUsuarios()
        {
            return Ok("Pong");
        }
    }
}
