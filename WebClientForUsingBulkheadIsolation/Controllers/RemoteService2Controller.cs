using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebClientForUsingBulkheadIsolation.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RemoteService2Controller : ControllerBase
    {
        public async Task<IActionResult> Get()
        {
            await Task.Delay(10000); // simulate some data processing by delaying for 10 seconds 

            return Ok("Successful request");
        }
    }
}
