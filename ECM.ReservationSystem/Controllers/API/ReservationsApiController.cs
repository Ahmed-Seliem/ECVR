using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ECM.ReservationSystem.Controllers.API
{
    [Route("api/[controller]")]
    [ApiController]
    public class ReservationsApiController : ControllerBase
    {
        [HttpGet]
        public IActionResult Get()
        {
            return Ok(new { message = "API is running" });
        }
    }
}
