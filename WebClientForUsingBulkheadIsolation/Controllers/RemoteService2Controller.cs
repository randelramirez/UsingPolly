using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace WebClientForUsingBulkheadIsolation.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RemoteService2Controller : ControllerBase
    {
        public async Task<IActionResult> Get()
        {
            await Task.Delay(10000);

            return Ok("Successful request");
        }
    }
}
