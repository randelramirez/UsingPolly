using System;
using System.Net.Http;
using System.Threading.Tasks;
using Client.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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

        private static void ConfigureServices(IServiceCollection services)
        {
            services.AddLogging(builder =>
            {
                builder.AddConsole();
                builder.AddDebug();
            });

            // simple way to inject HttpClient, only suitable for demo application like this(for Production apps use HttpClientFactory)
            services.AddSingleton<HttpClient>(new HttpClient());
            
            services.AddSingleton<IPolicyHolder>(new PolicyHolder());
            services.AddSingleton<PolicyRegistry>(PolicyRegistryFactory.GetRegistry());

            // Services to run for testing Polly
            //services.AddScoped<IService, WaitRetryDelegateTimeoutService>();
            //services.AddScoped<IService, PolicyHolderFromDIService>();
            //services.AddScoped<IService, UsingPolicyRegistryService>();
            //services.AddScoped<IService, UsingContextService>();

            services.AddHttpClientFactoryWithPolicies();
            services.AddScoped<IService, HttpClientFactoryManagementService>();
        }
    }
}
