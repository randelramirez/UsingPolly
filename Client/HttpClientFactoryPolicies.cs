using Client.TypedClients;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.Registry;
using System;
using System.Net.Http;

namespace Client
{
    public static class HttpClientFactoryPolicies
    {
        public static IServiceCollection AddHttpClientFactoryWithPolicies(this IServiceCollection services)
        {
            services.ConfigurePolicies();
            services.ConfigureHttpClient();
            return services;
        }

        private static IServiceCollection ConfigurePolicies(this IServiceCollection services)
        {
            IPolicyRegistry<string> registry = services.AddPolicyRegistry();

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
            // using named client
            services.AddHttpClient("WithPolicies", client =>
            {

                client.BaseAddress = new Uri("https://localhost:44354/");
                client.DefaultRequestHeaders.Add("Accept", "application/json");
            }).AddPolicyHandlerFromRegistry(policySelector);

            // using typed client
            // we can also try to add a client using an interface  services.AddHttpClient<IContactsService, ContactsService>
            // We are using lambda form for passing the policySelector
            services.AddHttpClient<ContactsClient>().AddPolicyHandlerFromRegistry((policyRegistry, httpRequestMessage) => {
                return httpRequestMessage switch
                {
                    _ when httpRequestMessage.Method == HttpMethod.Get => policyRegistry
                        .Get<IAsyncPolicy<HttpResponseMessage>>("httpWaitAndpRetryPolicy"),
                    _ when httpRequestMessage.Method == HttpMethod.Post => policyRegistry
                        .Get<IAsyncPolicy<HttpResponseMessage>>("noOpPolicy"),
                    //_ when httpRequestMessage.Method == HttpMethod.Get => policyRegistry
                    //    .Get<IAsyncPolicy<HttpResponseMessage>>("httpWaitAndpRetryPolicy"),
                    _ => throw new NotImplementedException(),
                };
            });

            return services;
        }

        private static IAsyncPolicy<HttpResponseMessage> policySelector(IReadOnlyPolicyRegistry<string> policyRegistry,
           HttpRequestMessage httpRequestMessage)
        {
            return httpRequestMessage switch
            {
                _ when httpRequestMessage.Method == HttpMethod.Get => policyRegistry
                    .Get<IAsyncPolicy<HttpResponseMessage>>("httpWaitAndpRetryPolicy"),
                _ when httpRequestMessage.Method == HttpMethod.Post => policyRegistry
                    .Get<IAsyncPolicy<HttpResponseMessage>>("noOpPolicy"),
                _ when httpRequestMessage.RequestUri.LocalPath.Contains("DEV") => policyRegistry
                    .Get<IAsyncPolicy<HttpResponseMessage>>("NoOpPolicy"),
                _ => throw new NotImplementedException(),
            };
        }
    }
}
