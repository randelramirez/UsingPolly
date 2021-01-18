using Core.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Polly;
using Polly.Registry;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace WebClientForCachingDemo.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TestController : ControllerBase
    {
        private readonly IReadOnlyPolicyRegistry<string> policyRegistry;
        private readonly HttpClient httpClient;

        public TestController(IReadOnlyPolicyRegistry<string> policyRegistry, HttpClient httpClient)
        {
            this.policyRegistry = policyRegistry;
            this.httpClient = httpClient;
        }

        public async Task<IActionResult> Get()
        {
            // To verify this, run the API project, and then set a breakpoint on the Action method, only the request should hit the breakpoint

            var _cachePolicy = policyRegistry.Get<IAsyncPolicy<HttpResponseMessage>>("myCachePolicy");
            Context policyExecutionContext = new Context(nameof(TestController));

            // the remote endpoint should not be hit and instead return a response from the cache(the first request will hit the remote endpoint)
            HttpResponseMessage response = await _cachePolicy.ExecuteAsync(
                (context) => httpClient.GetAsync("api/contacts"), policyExecutionContext);

            if (response.IsSuccessStatusCode)
            {
                var contacts = JsonConvert.DeserializeObject<IEnumerable<ContactViewModel>>(await response.Content.ReadAsStringAsync());
                return Ok(contacts);
            }

            return StatusCode((int)response.StatusCode, response.Content.ReadAsStringAsync());
        }
    }
}
