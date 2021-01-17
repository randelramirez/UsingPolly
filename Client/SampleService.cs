using Core.ViewModels;
using Newtonsoft.Json;
using Polly;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace Client
{
    // A service created for Demo of Unit Testing
    public class SampleService
    {
        private readonly IAsyncPolicy<HttpResponseMessage> httpRetryPolicy;
        private readonly HttpClient httpClient;

        public SampleService(IAsyncPolicy<HttpResponseMessage> httpRetryPolicy, HttpClient httpClient)
        {
            this.httpRetryPolicy = httpRetryPolicy;
            this.httpClient = httpClient;
            this.httpClient.BaseAddress = new Uri("https://localhost:44354/");
        }

        public async Task<IEnumerable<ContactViewModel>> GetContactsAsStream()
        {
            var request = new HttpRequestMessage(
              HttpMethod.Get,
              "api/contacts/");
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            using var response = await this.httpRetryPolicy.ExecuteAsync(() => httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead));
            if (!response.IsSuccessStatusCode)
            {
                // inspect the status code
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    // show this to the user
                    Console.WriteLine("The requested contact cannot be found.");
                    return null;
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    // trigger a login flow
                    throw new UnauthorizedApiAccessException();
                }
                response.EnsureSuccessStatusCode();
            }

            var stream = await response.Content.ReadAsStreamAsync();
            using var streamReader = new StreamReader(stream);
            using var jsonTextReader = new JsonTextReader(streamReader);
            var jsonSerializer = new JsonSerializer();
            return jsonSerializer.Deserialize<IEnumerable<ContactViewModel>>(jsonTextReader);
        }
    }
}
