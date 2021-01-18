using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Polly;
using Polly.Caching;
using Polly.Caching.Memory;
using Polly.Registry;
using System;
using System.Net.Http;
using System.Net.Http.Headers;

namespace WebClientForCachingDemo
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            // Setup needed for memory cache
            services.AddMemoryCache();
            services.AddSingleton<Polly.Caching.IAsyncCacheProvider, MemoryCacheProvider>();
            services.AddSingleton<Polly.Registry.IReadOnlyPolicyRegistry<string>, Polly.Registry.PolicyRegistry>((serviceProvider) =>
            {
                PolicyRegistry registry = new PolicyRegistry();
                registry.Add("myCachePolicy",
                    Policy.CacheAsync<HttpResponseMessage>(
                        serviceProvider
                            .GetRequiredService<IAsyncCacheProvider>()
                            .AsyncFor<HttpResponseMessage>(),
                        TimeSpan.FromMinutes(5)));
                return registry;
            });

            // use HttpClientFactory in real apps
            HttpClient httpClient = new HttpClient()
            {
                BaseAddress = new Uri("https://localhost:44354/")
            };

            httpClient.DefaultRequestHeaders.Accept.Clear();
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            services.AddSingleton<HttpClient>(httpClient);

            services.AddControllers();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
