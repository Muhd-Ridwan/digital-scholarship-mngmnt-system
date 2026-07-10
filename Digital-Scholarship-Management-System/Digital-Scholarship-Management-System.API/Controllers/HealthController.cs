using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Digital_Scholarship_Management_System.API.Controllers
{
    //[Route("api/[controller]")] this url become = /api/health because it Minus the name of HealthController by removing the Controller. case-insensitive
    [Route("health")] // This url will become = /health
    [ApiController]
    public class HealthController : ControllerBase
    {
        [HttpGet]
        public IActionResult Get() // IActionResult = flexible response box that lets controller method send back different kinds of HTTP replies (success, error, not found and etc)
        {
            return Ok(new { status = "Healthy" });
        }
    }
}
