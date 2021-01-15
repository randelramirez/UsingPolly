using Core;
using Core.ViewModels;
using Newtonsoft.Json;
using Polly;
using Polly.Fallback;
using Polly.Retry;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Client.Services
{
    public class WaitRetryDelegateTimeoutService : IService
    {
        private static HttpClient httpClient = new HttpClient();

        private readonly AsyncRetryPolicy<HttpResponseMessage> httpWaitAndRetry;
        private readonly AsyncRetryPolicy<HttpResponseMessage> httpretryPolicyForHttpClienTimeout;
        private readonly AsyncRetryPolicy<HttpResponseMessage> httpWaitAndRetryWithDelegate;
        private readonly AsyncFallbackPolicy<HttpResponseMessage> httpFallbackPolicy;
        
        public WaitRetryDelegateTimeoutService()
        {
            httpClient.BaseAddress = new Uri("https://localhost:44354/");
            httpClient.Timeout = new TimeSpan(0, 0, 30);
            httpClient.DefaultRequestHeaders.Clear();

            httpWaitAndRetry = Policy.HandleResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode)
               .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt) / 2));

            httpretryPolicyForHttpClienTimeout = Policy.HandleResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode)
              .Or<HttpRequestException>()
              .RetryAsync(1, onRetry: OnRetry);

            httpWaitAndRetryWithDelegate = Policy.HandleResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode)
              .WaitAndRetryAsync(3, retryAttempt =>
                  TimeSpan.FromSeconds(Math.Pow(2, retryAttempt) / 2), onRetry: (httpResponseMessage, retryCount) =>
                  {
                        // Log
                        Console.WriteLine($"Retrying...");
                  });

            httpFallbackPolicy = Policy.HandleResult<HttpResponseMessage>(r => r.StatusCode == System.Net.HttpStatusCode.NotFound)
                .FallbackAsync(new HttpResponseMessage(System.Net.HttpStatusCode.OK) { });

        }

        static void OnRetry(DelegateResult<HttpResponseMessage> delegateResult, int retryCount)
        {
            if (delegateResult.Exception is HttpRequestException)
            {
                // We check if the exception is a timeout exception
                // Note that timeout is not thrown as a TimeoutException but as TaskCanceledException
                if (delegateResult.Exception.GetBaseException().Message == "The operation timed out")
                {
                    // log something about the timeout  
                    Console.WriteLine("The request timedout, logging...");
                }
            }
        }

        public async Task Run()
        {
            //await GetContactWaitAndRetry();
            //await GetContactWaitAndRetryWithDelegate();
            await GetContactsWithFallbackPolicy();
        }

        public async Task GetContactWaitAndRetry()
        {
            // api/contactsss is an invalid endpoint
            var response = await httpWaitAndRetry.ExecuteAsync(() => GetData());
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

        public async Task GetContactWaitAndRetryWithDelegate()
        {
            // api/contactsss is an invalid endpoint
            var response = await httpWaitAndRetryWithDelegate.ExecuteAsync(() => httpClient.GetAsync("api/contactsss"));
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
        }

        public async Task GetContactsWithFallbackPolicy()
        {
            // api/contactsss is an invalid endpoint

            //var response = await httpFallbackPolicy.ExecuteAsync(() => httpWaitAndRetryWithDelegate.ExecuteAsync(() => GetData()));

            // using WrapAsync
            var response = await Policy.WrapAsync(httpFallbackPolicy, httpWaitAndRetryWithDelegate).ExecuteAsync(() => GetData());

            // we will still get a 200 despite calling  a non-existing endpoint 
            response.EnsureSuccessStatusCode();

            Console.WriteLine($"Status code:{(int)response.StatusCode}, {response.ReasonPhrase}");

            static async Task<HttpResponseMessage> GetData()
            {
                // We creared a separare local method so we can breakpoint in this method to check for retries
                return await httpClient.GetAsync("api/contactsss");
            }
        }
    }
}
