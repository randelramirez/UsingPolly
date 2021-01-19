using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Polly;
using Polly.Bulkhead;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace WebClientForUsingBulkheadIsolation
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
            // 2 request can be in the Execution slot at the same time
            // 4 is the max request that can be in the Queue slot
            // basically, we can handle 6 parallel requests, 2 will be executed and 4 will be queued
            AsyncBulkheadPolicy<HttpResponseMessage> bulkheadIsolationPolicy = Policy
                .BulkheadAsync<HttpResponseMessage>(2, 4, onBulkheadRejectedAsync: OnBulkheadRejectedAsync);

            HttpClient httpClient = new HttpClient()
            {
                BaseAddress = new Uri("https://localhost:5001/api/")
            };
            httpClient.DefaultRequestHeaders.Accept.Clear();
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            services.AddSingleton<HttpClient>(httpClient);
            services.AddSingleton<AsyncBulkheadPolicy<HttpResponseMessage>>(bulkheadIsolationPolicy);
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

        private Task OnBulkheadRejectedAsync(Context context)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Request failed, OnBulkheadRejectedAsync Executed");
            Console.ResetColor();
            return Task.CompletedTask;
        }
    }
}
