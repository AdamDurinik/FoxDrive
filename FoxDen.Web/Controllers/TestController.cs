using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace FoxDen.Web.Controllers
{
    [ApiController]
    [Route("api/test")]
    public class TestController : ControllerBase
    {
        [HttpGet("public")]
        public IActionResult Public() => Ok(new { msg = "public endpoint OK" });

        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [HttpGet("protected")]
        public IActionResult Protected()
        {
            var name = User?.Identity?.Name ?? "anonymous";
            return Ok(new { msg = "protected endpoint OK", user = name, claims = User?.Claims?.Select(c => new { c.Type, c.Value }) });
        }
    }
}
