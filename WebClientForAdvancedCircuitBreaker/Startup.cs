using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Polly;
using Polly.CircuitBreaker;
using System;
using System.Net.Http;
using System.Net.Http.Headers;

namespace WebClientForAdvancedCircuitBreaker
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
            // if 50% of the requests fails in the span of 60 secs, we disable all requests
            // We can use the circuit breaker to allow the system to recover from possible error/exceptions
            AsyncCircuitBreakerPolicy<HttpResponseMessage> breakerPolicy = Policy.HandleResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode)
                .AdvancedCircuitBreakerAsync<HttpResponseMessage>(0.5, TimeSpan.FromSeconds(60), 7, TimeSpan.FromSeconds(15),
                    OnBreak, OnReset, OnHalfOpen);
            services.AddSingleton<AsyncCircuitBreakerPolicy<HttpResponseMessage>>(breakerPolicy);

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

        private void OnHalfOpen()
        {
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine("Connection half open");
            Console.ResetColor();
        }

        private void OnReset(Context context)
        {
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine("Connection reset");
            Console.ResetColor();
        }

        private void OnBreak(DelegateResult<HttpResponseMessage> delegateResult, TimeSpan timeSpan, Context context)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Connection break: {delegateResult.Result}, {delegateResult.Result}");
            Console.ResetColor();
        }
    }
}
