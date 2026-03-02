using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECM.ReservationSystem.Controllers.Admin
{
    [Authorize]
    public class DebugController : Controller
    {
        public IActionResult Claims()
        {
            var claims = User.Claims
                .Select(c => new { c.Type, c.Value })
                .ToList();

            return Json(claims);
        }
    }
}
