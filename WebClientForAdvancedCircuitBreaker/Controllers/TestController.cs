using Core.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace WebClientForAdvancedCircuitBreaker.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TestController : ControllerBase
    {
        private readonly HttpClient httpClient;
        private readonly AsyncRetryPolicy<HttpResponseMessage> httpWaitAndRetryPolicy;

        private readonly AsyncCircuitBreakerPolicy<HttpResponseMessage> breakerPolicy;

        public TestController(HttpClient httpClient, AsyncCircuitBreakerPolicy<HttpResponseMessage> breakerPolicy)
        {
            this.httpClient = httpClient;
          
           
            this.breakerPolicy = breakerPolicy;

            this.httpWaitAndRetryPolicy = Policy.HandleResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode)
              .WaitAndRetryAsync(3, retryAttempt =>
                  TimeSpan.FromSeconds(Math.Pow(2, retryAttempt) / 2), onRetry: (httpResponseMessage, retryCount) =>
                  {
                      // Log
                      Console.ForegroundColor = ConsoleColor.Red;
                      Console.WriteLine($"Request failed...{httpResponseMessage.Result.StatusCode}");

                      Console.ForegroundColor = ConsoleColor.Yellow;
                      Console.WriteLine($"Retrying...");
                      Console.ForegroundColor = ConsoleColor.White;
                  });
        }

        public async Task<ActionResult<IEnumerable<ContactViewModel>>> Get()
        {
            var response = await httpWaitAndRetryPolicy.ExecuteAsync(
                 () => this.breakerPolicy.ExecuteAsync(
                     () => GetData(this.httpClient)));

            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            var contacts = new List<ContactViewModel>();

            if (response.Content.Headers.ContentType.MediaType == "application/json")
            {
                contacts = JsonConvert.DeserializeObject<List<ContactViewModel>>(content);
            }
            else if (response.Content.Headers.ContentType.MediaType == "application/xml")
            {
                var serializer = new XmlSerializer(typeof(List<ContactViewModel>));
                contacts = (List<ContactViewModel>)serializer.Deserialize(new StringReader(content));
            }

            static async Task<HttpResponseMessage> GetData(HttpClient httpClient)
            {
                // We try to simulate a flaky web service
                var generator = new Random();
                var number = generator.Next(1, 20);

                // We creared a separare local method so we can breakpoint in this method to check for retries
                var endpoint = number % 2 == 0 ? "contacts" : "contactsss";
                return await httpClient.GetAsync($"api/{endpoint} ");
            }

            return Ok(contacts);
        }
    }
}
