using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Caching;
using Polly.Caching.Memory;
using Polly.Registry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace WebClientForCachingDemo
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        private IPolicyRegistry<string> _myRegistry;

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            HttpClient httpClient = new HttpClient()
            {
                BaseAddress = new Uri("https://localhost:44354/api/")
            };

            httpClient.DefaultRequestHeaders.Accept.Clear();
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

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
