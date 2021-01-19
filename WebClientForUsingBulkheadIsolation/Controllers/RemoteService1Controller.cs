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
        private static int _requestCount = 0;
        private readonly HttpClient _httpClient;
        private readonly AsyncBulkheadPolicy<HttpResponseMessage> _bulkheadIsolationPolicy;

        public RemoteService1Controller(HttpClient httpClient, AsyncBulkheadPolicy<HttpResponseMessage> bulkheadIsolationPolicy)
        {
            _httpClient = httpClient;
            _bulkheadIsolationPolicy = bulkheadIsolationPolicy;
        }

        public async Task<IActionResult> Get()
        {
            _requestCount++;
            LogBulkheadInfo();
          

            HttpResponseMessage response = await _bulkheadIsolationPolicy.ExecuteAsync(
                     () => _httpClient.GetAsync("remoteservice2"));

            if (response.IsSuccessStatusCode)
            {
               var itemsInStock = JsonConvert.DeserializeObject<string>(await response.Content.ReadAsStringAsync());
                return Ok(itemsInStock);
            }

            return StatusCode((int)response.StatusCode, response.Content.ReadAsStringAsync());
        }

        private void LogBulkheadInfo()
        {
            Console.WriteLine($"PollyDemo RequestCount {_requestCount}");
            Console.WriteLine($"PollyDemo BulkheadAvailableCount " +
                                               $"{_bulkheadIsolationPolicy.BulkheadAvailableCount}");
            Console.WriteLine($"PollyDemo QueueAvailableCount " +
                                               $"{_bulkheadIsolationPolicy.QueueAvailableCount}");
        }
    }
}
