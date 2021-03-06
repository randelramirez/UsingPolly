﻿using Core.ViewModels;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Client.Services
{
    public class PolicyHolderFromDIService : IService
    {
        private static HttpClient httpClient = new HttpClient();

        private readonly IPolicyHolder policyHolder;

        public async Task Run()
        {
            await WaitAndRetry();
        }

        public PolicyHolderFromDIService(IPolicyHolder policyHolder, HttpClient client)
        {
            httpClient = client;
            httpClient.BaseAddress = new Uri("https://localhost:44354/");
            httpClient.Timeout = new TimeSpan(0, 0, 30);
            httpClient.DefaultRequestHeaders.Clear();
            this.policyHolder = policyHolder;
        }

        public async Task WaitAndRetry()
        {
            // api/contactsss is an invalid endpoint
            var response = await policyHolder.HttpRetryPolicy.ExecuteAsync(() => GetData());
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            var contacts = new List<ContactViewModel>();
            if (response.Content.Headers.ContentType.MediaType == "application/json")
            {
                contacts = JsonConvert.DeserializeObject<List<ContactViewModel>>(content);
            }
            else if (response.Content.Headers.ContentType.MediaType == "application/xml")
            {
                var serializer = new XmlSerializer(typeof(List<ContactViewModel>));
                contacts = (List<ContactViewModel>)serializer.Deserialize(new StringReader(content));
            }

            foreach (var contact in contacts)
            {
                Console.WriteLine($"Name: {contact.Name}, Address: {contact.Address}");
            }

            static async Task<HttpResponseMessage> GetData()
            {
                // We creared a separare local method so we can breakpoint in this method to check for retries
                return await httpClient.GetAsync("api/contactsss");
            }
        }
    }
}
