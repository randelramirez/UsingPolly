using Client.TypedClients;
using Core;
using Core.ViewModels;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace Client.Services
{
    public class HttpClientFactoryManagementService : IService
    {
        private readonly IHttpClientFactory httpClientFactory;
        private readonly ContactsClient contactsClient;

        public HttpClientFactoryManagementService(IHttpClientFactory httpClientFactory)
        {
            this.httpClientFactory = httpClientFactory;

            // Using a Typed client
            // The HttpClient instances injected by DI, can be disposed of safely, because the associated HttpMessageHandler is managed by the factory. 
            //this.contactsClient = contactsClient;
        }

        public async Task Run()
        {
            //await GetContactsWithHttpClientFromFactory();
            await GetContactsWithNamedHttpClientFromFactory();
            //await GetContactsWithTypedHttpClient();
        }

        public async Task GetContactsWithHttpClientFromFactory()
        {
            var httpClient = httpClientFactory.CreateClient();

            var request = new HttpRequestMessage(
                HttpMethod.Get,
                "https://localhost:44354/api/contacts");
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            using var response = await httpClient.SendAsync(request,
                HttpCompletionOption.ResponseHeadersRead);
            var stream = await response.Content.ReadAsStreamAsync();
            response.EnsureSuccessStatusCode();

            using var streamReader = new StreamReader(stream, new UTF8Encoding(), true, 1024, false);
            using var jsonTextReader = new JsonTextReader(streamReader);
            var jsonSerializer = new JsonSerializer();
            
            var contacts = jsonSerializer.Deserialize<List<ContactViewModel>>(jsonTextReader);
            foreach (var contact in contacts)
            {
                Console.WriteLine($"Name: {contact.Name}, Address: {contact.Address}");
            }
        }

        // We expect the GET request to retry
        private async Task GetContactsWithNamedHttpClientFromFactory()
        {
            var httpClient = httpClientFactory.CreateClient("WithPolicies");

            var request = new HttpRequestMessage(
                HttpMethod.Get,
                "api/contactsss");
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            request.Headers.AcceptEncoding.Add(new StringWithQualityHeaderValue("gzip"));

            using var response = await httpClient.SendAsync(request,
                HttpCompletionOption.ResponseHeadersRead);
            var stream = await response.Content.ReadAsStreamAsync();
            response.EnsureSuccessStatusCode();

            using var streamReader = new StreamReader(stream, new UTF8Encoding(), true, 1024, false);
            using var jsonTextReader = new JsonTextReader(streamReader);
            var jsonSerializer = new JsonSerializer();

            var contacts = jsonSerializer.Deserialize<List<ContactViewModel>>(jsonTextReader);
            foreach (var contact in contacts)
            {
                Console.WriteLine($"Name: {contact.Name}, Address: {contact.Address}");
            }
        }


        public async Task<ContactViewModel> CreateContact()
        {
            var httpClient = httpClientFactory.CreateClient("WithPolicies");
            var newContact = new Contact()
            {
                Name = $"New Name {DateTimeOffset.UtcNow}",
                Address = $"New Address {DateTimeOffset.UtcNow}"
            };

            var serializedMovieToCreate = JsonConvert.SerializeObject(newContact);

            var request = new HttpRequestMessage(HttpMethod.Post, "api/contactsss");
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            request.Content = new StringContent(serializedMovieToCreate);
            request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

            var response = await httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            var createdContact = JsonConvert.DeserializeObject<ContactViewModel>(content);
            Console.WriteLine($"Name: {createdContact.Name}, Address: {createdContact.Address}");
            return createdContact;
        }

        private async Task GetContactsWithTypedHttpClient()
        {
            var contacts = await this.contactsClient.GetContacts();
            foreach (var contact in contacts)
            {
                Console.WriteLine($"Name: {contact.Name}, Address: {contact.Address}");
            }
        }
    }
}
