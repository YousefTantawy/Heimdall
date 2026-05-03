using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HeimdallBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AdminController : ControllerBase
    {
        // The [Authorize] attribute enforces that only users with the "Admin" role can access this.
        [Authorize(Roles = "Admin")]
        [HttpGet("dashboard")]
        public IActionResult GetAdminDashboard()
        {
            return Ok(new { Message = "Access granted. You are recognized as an Admin." });
        }
    }
}