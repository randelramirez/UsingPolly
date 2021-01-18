using Core.ViewModels;
using Newtonsoft.Json;
using Polly;
using Polly.Fallback;
using Polly.Retry;
using Polly.Timeout;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Client.Services
{
    public class WaitRetryDelegateTimeoutService : IService
    {
        private static HttpClient httpClient = new HttpClient();

        private readonly AsyncTimeoutPolicy<HttpResponseMessage> timeoutPolicy;
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

            // this is not the same as setting timeout for HttpClient
            // if the request does not respond in the given time (ex. 5 seconds), TimeoutRejectedException is thrown
            timeoutPolicy = Policy.TimeoutAsync<HttpResponseMessage>(5,TimeoutStrategy.Pessimistic);

            httpretryPolicyForHttpClienTimeout = Policy.HandleResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode)
              .Or<HttpRequestException>()
              .RetryAsync(1, onRetry: OnRetry);

            httpWaitAndRetryWithDelegate = Policy.HandleResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode)
              .WaitAndRetryAsync(3, retryAttempt =>
                  TimeSpan.FromSeconds(Math.Pow(2, retryAttempt) / 2), onRetry: (httpResponseMessage, retryCount) =>
                  {
                      // Log
                      Console.WriteLine(httpResponseMessage.Result.StatusCode);
                        Console.WriteLine($"Retrying...");
                  });

            httpFallbackPolicy = Policy.HandleResult<HttpResponseMessage>(r => r.StatusCode == HttpStatusCode.NotFound)
                .FallbackAsync(new HttpResponseMessage(HttpStatusCode.OK) { });
        }

        static void OnRetry(DelegateResult<HttpResponseMessage> delegateResult, int retryCount)
        {
            if (delegateResult.Exception is HttpRequestException)
            {
                // We check if the exception is a timeout exception
                if (delegateResult.Exception.GetBaseException()
                    .Message.Contains("A connection attempt failed because the connected party did not properly respond after a period of time"))
                {
                    // log something about the timeout  
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("The request timedout, logging...");
                    Console.ForegroundColor = ConsoleColor.White;
                }
            }
        }

        public async Task Run()
        {
            //await TimeoutPolicy();
            //await WaitAndRetry();
            //await WaitAndRetryWithHttpClientTimeout();
            //await WaitAndRetryWithDelegate();
            //await WithFallbackPolicy();
            await WrappingFallbackRetryAndTimeoutPolicy();
        }

        public async Task TimeoutPolicy()
        {
            // api/contactsss is an invalid endpoint
            var response = await timeoutPolicy.ExecuteAsync(() => GetData());
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
                return await httpClient.GetAsync("api/contacts");
            }
        }

        public async Task WaitAndRetry()
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

        public async Task WaitAndRetryWithHttpClientTimeout()
        {
            // api/contactsss is an invalid endpoint
            var response = await httpretryPolicyForHttpClienTimeout.ExecuteAsync(() => GetData());
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
                return await httpClient.GetAsync("http://10.255.255.1/someUnreachableEndpoint/");
            }
        }

        public async Task WaitAndRetryWithDelegate()
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

        public async Task WithFallbackPolicy()
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

        public async Task WrappingFallbackRetryAndTimeoutPolicy()
        {
            // api/contactsss is an invalid endpoint
            // using WrapAsync
            var response = await Policy.WrapAsync(httpFallbackPolicy, httpWaitAndRetryWithDelegate, timeoutPolicy)
                .ExecuteAsync(() => GetData()); // this will return 200, because the original response was 404 and we have a fallback policy

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
