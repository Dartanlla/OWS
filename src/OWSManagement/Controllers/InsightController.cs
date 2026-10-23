using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using OWSData.Repositories.Interfaces;
using OWSManagement.Auth;

namespace OWSManagement.Controllers
{
    /// <summary>Where new players get to and where they stop, quest completion, and characters far above the median.</summary>
    [Route("api/[controller]")]
    [ApiController]
    public class InsightController : Controller
    {
        private readonly IConsoleInsightRepository _insight;
        private readonly ConsoleCustomer _customer;

        public InsightController(IConsoleInsightRepository insight, ConsoleCustomer customer)
        {
            _insight = insight;
            _customer = customer;
        }

        [HttpGet]
        [Route("")]
        public async Task<IActionResult> Get()
        {
            return Ok(new
            {
                NewPlayers = await _insight.GetNewPlayers(_customer.CustomerGUID),
                Stopped = await _insight.GetStoppedPlayers(_customer.CustomerGUID),
                Quests = await _insight.GetQuestFunnel(_customer.CustomerGUID),
                Outliers = await _insight.GetOutliers(_customer.CustomerGUID)
            });
        }
    }
}
