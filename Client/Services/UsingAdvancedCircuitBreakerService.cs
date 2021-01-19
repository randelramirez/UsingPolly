using Core.ViewModels;
using Newtonsoft.Json;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Client.Services
{
    public class UsingAdvancedCircuitBreakerService : IService
    {
        private static HttpClient httpClient;
        private readonly AsyncRetryPolicy<HttpResponseMessage> httpWaitAndRetryPolicy;

        private readonly AsyncCircuitBreakerPolicy<HttpResponseMessage> breakerPolicy;
        public UsingAdvancedCircuitBreakerService(HttpClient httpClient, AsyncCircuitBreakerPolicy<HttpResponseMessage> breakerPolicy)
        {
            UsingAdvancedCircuitBreakerService.httpClient = httpClient;
            httpClient.BaseAddress = new Uri("https://localhost:44354/");
            httpClient.Timeout = new TimeSpan(0, 0, 30);
            httpClient.DefaultRequestHeaders.Clear();

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

        public async Task Run()
        {
            await Get();
        }

        public async Task Get()
        {
            //var response = await httpWaitAndRetry.ExecuteAsync(() => GetData());


            var response = await httpWaitAndRetryPolicy.ExecuteAsync(
                 () => this.breakerPolicy.ExecuteAsync(
                     () =>  GetData()));

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

            foreach (var contact in contacts)
            {
                Console.WriteLine($"Name: {contact.Name}, Address: {contact.Address}");
            }

            static async Task<HttpResponseMessage> GetData()
            {
                // We creared a separare local method so we can breakpoint in this method to check for retries
                return await httpClient.GetAsync("api/contactsss");
            }
        }
    }
}
