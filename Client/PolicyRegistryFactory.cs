using Polly;
using Polly.Registry;
using System;
using System.Net.Http;

namespace Client
{
    public static class PolicyRegistryFactory
    {
        public static PolicyRegistry GetRegistry()
        {
            PolicyRegistry registry = new PolicyRegistry();

            IAsyncPolicy<HttpResponseMessage> httpRetryPolicy 
                 = Policy.HandleResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode)
              .WaitAndRetryAsync(3, retryAttempt =>
                  TimeSpan.FromSeconds(Math.Pow(2, retryAttempt) / 2), onRetry: (httpResponseMessage, retryCount) =>
                  {
                      // Log
                      Console.WriteLine(httpResponseMessage.Result.StatusCode);
                      Console.WriteLine($"Retrying...");
                  });

            registry.Add("SimpleHttpWaitAndRetry", httpRetryPolicy);

            IAsyncPolicy httpClientTimeoutException = Policy.Handle<HttpRequestException>()
               .WaitAndRetryAsync(1, retryAttempt => TimeSpan.FromSeconds(retryAttempt));

            registry.Add("HttpClientTimeout", httpClientTimeoutException);

            return registry;
        }
    }
}
