using System;
using System.Net.Http;
using System.Threading.Tasks;
using Client.MessageHandlers;
using Client.Services;
using Client.TypedClients;
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

        private static void ConfigureServices(IServiceCollection serviceCollection)
        {
            serviceCollection.AddLogging(builder =>
            {
                builder.AddConsole();
                builder.AddDebug();
            });

            serviceCollection.AddSingleton<PolicyHolder>();
            serviceCollection.AddSingleton<PolicyRegistry>(PolicyRegistryFactory.GetRegistry());
            //serviceCollection.AddScoped<IService, WaitRetryDelegateTimeoutService>();
            //serviceCollection.AddScoped<IService, PolicyHolderFromDIService>();
            serviceCollection.AddScoped<IService, UsingPolicyRegistryService>();

        }
    }
}
