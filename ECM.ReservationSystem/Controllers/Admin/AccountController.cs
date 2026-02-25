using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECM.ReservationSystem.Controllers.Admin
{
    [Authorize]
    public class AccountController : Controller
    {

        private readonly IConfiguration _configuration;

        public AccountController(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        public IActionResult AccessDenied()
        {
            return RedirectToAction("Error", "Home");
        }


        public async Task<IActionResult> Logout()
        {
            var identityServerUrl = _configuration["Settings:Identity:ServerURL"];

            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            await HttpContext.SignOutAsync("oidc", new AuthenticationProperties
            {
                RedirectUri = $"{identityServerUrl}Account/Login"
            });

            return new EmptyResult();
        }
    }


}
