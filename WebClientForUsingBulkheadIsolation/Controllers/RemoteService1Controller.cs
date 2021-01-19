using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Polly.Bulkhead;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace WebClientForUsingBulkheadIsolation.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RemoteService1Controller : ControllerBase
    {
        private readonly HttpClient httpClient;
        private readonly AsyncBulkheadPolicy<HttpResponseMessage> bulkheadIsolationPolicy;

        public RemoteService1Controller(HttpClient httpClient, AsyncBulkheadPolicy<HttpResponseMessage> bulkheadIsolationPolicy)
        {
            this.httpClient = httpClient;
            this.bulkheadIsolationPolicy = bulkheadIsolationPolicy;
        }

        // To test the Bulkhead isolation, open this Action in multiple tabs(at least 7 parallel requests to see the error)
        public async Task<IActionResult> Get()
        {
            LogBulkheadInfo();

            HttpResponseMessage response = await bulkheadIsolationPolicy.ExecuteAsync(
                     () => httpClient.GetAsync("remoteservice2"));

            if (response.IsSuccessStatusCode)
            {
                var content = JsonConvert.DeserializeObject<string>(await response.Content.ReadAsStringAsync());
                return Ok(content);
            }

            return StatusCode((int)response.StatusCode, response.Content.ReadAsStringAsync());
        }

        private void LogBulkheadInfo()
        {
            Console.WriteLine($"{this.GetType().Assembly.GetName()} BulkheadAvailableCount " +
                                               $"{bulkheadIsolationPolicy.BulkheadAvailableCount}");
            Console.WriteLine($"{this.GetType().Assembly.GetName()}QueueAvailableCount " +
                                               $"{bulkheadIsolationPolicy.QueueAvailableCount}");
        }
    }
}
