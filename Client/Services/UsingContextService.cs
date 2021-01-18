using Core.ViewModels;
using Newtonsoft.Json;
using Polly;
using Polly.Retry;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Client.Services
{
    public class UsingContextService : IService
    {
        private static readonly HttpClient httpClient = new HttpClient();
        private readonly AsyncRetryPolicy<HttpResponseMessage> httpWaitAndRetryWithDelegate;

        public UsingContextService()
        {
            httpClient.BaseAddress = new Uri("https://localhost:44354/");
            httpClient.Timeout = new TimeSpan(0, 0, 30);
            httpClient.DefaultRequestHeaders.Clear();

            httpWaitAndRetryWithDelegate = Policy.HandleResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode)

             .WaitAndRetryAsync(3, retryAttempt =>
                 TimeSpan.FromSeconds(Math.Pow(2, retryAttempt) / 2), onRetry: (httpResponseMessage, retryCount, context) =>
                 {
                     Console.ForegroundColor = ConsoleColor.Blue;
                     Console.WriteLine($"Request failed...will retry after {retryCount.Seconds} seconds of " +
                         $"{context.PolicyKey} at {context.OperationKey},   CorrelationId: {context.CorrelationId}");
                     Console.ForegroundColor = ConsoleColor.White;

                     if (context.ContainsKey("ClientAppName"))
                     {
                         // Log
                         Console.WriteLine($"ClientAppName: {context["ClientAppName"]}");
                     }
                     if (context.ContainsKey("SecretMessage"))
                     {
                         // Log
                         Console.WriteLine($"SecretMessage: { context["SecretMessage"]}");
                     }
                     if (context.ContainsKey("Version"))
                     {
                         // Log
                         Console.WriteLine($"Version: {context["Version"]}");
                     }

                     // Log
                     Console.WriteLine(httpResponseMessage.Result.StatusCode);

                     Console.ForegroundColor = ConsoleColor.Yellow;
                     Console.WriteLine($"Retrying...");
                     Console.ForegroundColor = ConsoleColor.White;
                 });
        }

        public async Task Run()
        {
            await WaitAndRetry();
        }

        public async Task WaitAndRetry()
        {
            IDictionary<string, object> contextDictionary = new Dictionary<string, object>
            {
                { "ClientAppName", nameof(UsingContextService) }, {"SecretMessage", Guid.NewGuid()}, {"Version", Environment.Version}
            };

            var pollySampleContext = new Context("PollySampleContext", contextDictionary);

            // api/contactsss is an invalid endpoint
            var response = await httpWaitAndRetryWithDelegate.ExecuteAsync((context) => GetData(), pollySampleContext);
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
