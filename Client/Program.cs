using System;
using System.Net.Http;
using System.Threading.Tasks;
using Client.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.CircuitBreaker;
using Polly.Registry;

namespace Client
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var serviceCollection = new ServiceCollection();
            ConfigureServices(serviceCollection);

            var serviceProvider = serviceCollection.BuildServiceProvider();

            try
            {
                // Run our IntegrationService containing all samples and
                // await this call to ensure the application doesn't 
                // prematurely exit.
                await serviceProvider.GetService<IService>().Run();
            }
            catch (Exception generalException)
            {
                // log the exception
                var logger = serviceProvider.GetService<ILogger<Program>>();
                logger.LogError(generalException,
                    "An exception happened while running the integration service.");
            }

            Console.ReadKey();
        }

        private static void ConfigureServices(IServiceCollection serviceCollection)
        {
            serviceCollection.AddLogging(builder =>
            {
                builder.AddConsole();
                builder.AddDebug();
            });

            // if 50% of the requests fails in the span of 60 secs, we disable all requests
            // We can use the circuit breaker to allow the system to recover from possible error/exceptions
            AsyncCircuitBreakerPolicy<HttpResponseMessage> breakerPolicy = Policy.HandleResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode)
                .AdvancedCircuitBreakerAsync<HttpResponseMessage>(0.5, TimeSpan.FromSeconds(60), 7, TimeSpan.FromSeconds(15),
                    OnBreak, OnReset, OnHalfOpen);
            // we make it singleton so that all request that's using this policy will all use use the same state (Open/Close for to all request using this policy)
            serviceCollection.AddSingleton<AsyncCircuitBreakerPolicy<HttpResponseMessage>>(breakerPolicy);

            // simple way to inject HttpClient, only suitable for demo application like this(for Production apps use HttpClientFactory)
            serviceCollection.AddSingleton<HttpClient>(new HttpClient());
            
            serviceCollection.AddSingleton<IPolicyHolder>(new PolicyHolder());
            serviceCollection.AddSingleton<PolicyRegistry>(PolicyRegistryFactory.GetRegistry());

            // Services to run for testing Polly
            //serviceCollection.AddScoped<IService, WaitRetryDelegateTimeoutService>();
            //serviceCollection.AddScoped<IService, PolicyHolderFromDIService>();
            //serviceCollection.AddScoped<IService, UsingPolicyRegistryService>();
            serviceCollection.AddScoped<IService, UsingContextService>();
        }

        private static void OnHalfOpen()
        {
            Console.WriteLine("Connection half open");
        }

        private static void OnReset(Context context)
        {
            Console.WriteLine("Connection reset");
        }

        private static void OnBreak(DelegateResult<HttpResponseMessage> delegateResult, TimeSpan timeSpan, Context context)
        {
            Console.WriteLine($"Connection break: {delegateResult.Result}, {delegateResult.Result}");
        }
    }
}
