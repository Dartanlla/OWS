using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace OWSGlobalData.Controllers
{
    /// <summary>
    /// Public System API calls.
    /// </summary>
    /// <remarks>
    /// Contains system related API calls that are all publicly accessible.
    /// </remarks>
    [Route("api/[controller]")]
    [ApiController]
    public class SystemController : Controller
    {
        /// <summary>
        /// Gets the Api Status.
        /// </summary>
        /// <remarks>
        /// Get the Api Status and return true
        /// </remarks>
        [HttpGet]
        [Route("Status")]
        public IActionResult Status()
        {
            return Ok(true);
        }
    }
}
