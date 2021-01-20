using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.Registry;
using System;
using System.Net.Http;

namespace Client
{
    public static class HttpClientFactoryPolicies
    {
        public static IServiceCollection AddHttpClientWithPolicies(this IServiceCollection services)
        {
            services.ConfigurePolicies();
            services.ConfigureHttpClient();
            return services;
        }

        private static IServiceCollection ConfigurePolicies(this IServiceCollection services)
        {
            IPolicyRegistry<string> registry = services.AddPolicyRegistry();

            IAsyncPolicy<HttpResponseMessage> httpRetryPolicy =
                Policy.HandleResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode)
                    .RetryAsync(5);
            registry.Add(nameof(httpRetryPolicy), httpRetryPolicy);

            IAsyncPolicy<HttpResponseMessage> httpWaitAndpRetryPolicy =
                Policy.HandleResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode)
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
            registry.Add(nameof(httpWaitAndpRetryPolicy), httpWaitAndpRetryPolicy);

            IAsyncPolicy<HttpResponseMessage> noOpPolicy = Policy.NoOpAsync()
                .AsAsyncPolicy<HttpResponseMessage>();
            registry.Add(nameof(noOpPolicy), noOpPolicy);

            return services;
        }

        private static IServiceCollection ConfigureHttpClient(this IServiceCollection services)
        {
            services.AddHttpClient("WithPolicies", client =>
            {
                client.BaseAddress = new Uri("http://localhost:57696/api/");
                client.DefaultRequestHeaders.Add("Accept", "application/json");
            }).AddPolicyHandlerFromRegistry(PolicySelector);

            return services;
        }

        private static IAsyncPolicy<HttpResponseMessage> PolicySelector(IReadOnlyPolicyRegistry<string> policyRegistry,
           HttpRequestMessage httpRequestMessage)
        {
            return httpRequestMessage switch
            {
                _ when httpRequestMessage.Method == HttpMethod.Get => policyRegistry
                    .Get<IAsyncPolicy<HttpResponseMessage>>("httpWaitAndpRetryPolicy"),
                _ when httpRequestMessage.Method == HttpMethod.Post => policyRegistry
                    .Get<IAsyncPolicy<HttpResponseMessage>>("NoOpPolicy"),
                //_ when httpRequestMessage.Method == HttpMethod.Get => policyRegistry
                //    .Get<IAsyncPolicy<HttpResponseMessage>>("httpWaitAndpRetryPolicy"),
                _ => throw new NotImplementedException(),
            };

            //if (httpRequestMessage.RequestUri.LocalPath.StartsWith("find"))
            //{
            //    return policyRegistry.Get<IAsyncPolicy<HttpResponseMessage>>("SimpleHttpRetryPolicy");
            //}
            //else if (httpRequestMessage.RequestUri.LocalPath.StartsWith("create"))
            //{
            //    return policyRegistry.Get<IAsyncPolicy<HttpResponseMessage>>("NoOpPolicy");
            //}
            //else
            //{
            //    return policyRegistry.Get<IAsyncPolicy<HttpResponseMessage>>("SimpleWaitAndRetryPolicy");
            //}
        }
    }
}
