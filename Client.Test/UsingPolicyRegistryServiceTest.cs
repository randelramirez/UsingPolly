using Client.Services;
using Core.ViewModels;
using Moq;
using Moq.Protected;
using Polly;
using Polly.Registry;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Client.Test
{
    public class UsingPolicyRegistryServiceTest
    {
        [Fact]
        public async Task GetTest1()
        {
            //Arrange 
            var data = new List<ContactViewModel>();
            data.Add(new ContactViewModel()
            {
                Id = Guid.NewGuid(),
                Name = "Test",
                Address = "Test"
            });
            data.Add(new ContactViewModel()
            {
                Id = Guid.NewGuid(),
                Name = "Test",
                Address = "Test"
            });

            Mock<HttpMessageHandler> httpMessageHandler = new Mock<HttpMessageHandler>();
            httpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                    .Throws(new HttpRequestException("Response status code does not indicate success: 404 (Not Found)."));

            HttpClient httpClient = new HttpClient(httpMessageHandler.Object);
            httpClient.BaseAddress = new Uri(@"http://api/invalid/");
            httpClient.DefaultRequestHeaders.Accept.Clear();
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            IPolicyRegistry<string> mockRegistry = new PolicyRegistry();
            IAsyncPolicy<HttpResponseMessage> httpRetryPolicy = Policy.NoOpAsync<HttpResponseMessage>();
            IAsyncPolicy httpClientTimeoutExceptionPolicy = Policy.NoOpAsync();

            mockRegistry.Add("SimpleHttpWaitAndRetry", httpRetryPolicy);
            mockRegistry.Add("HttpClientTimeout", httpClientTimeoutExceptionPolicy);

            var service = new UsingPolicyRegistryService(mockRegistry, httpClient);

            // ACT
            var exception = await Record.ExceptionAsync(() => service.WaitAndRetry());

            // ASSERT
            Assert.Contains("404", exception.Message);

        }


        [Fact]
        public async Task GetTest()
        {
            //Arrange 
            var data = new List<ContactViewModel>();
            data.Add(new ContactViewModel()
            {
                Id = Guid.NewGuid(),
                Name = "Test",
                Address = "Test"
            });
            data.Add(new ContactViewModel()
            {
                Id = Guid.NewGuid(),
                Name = "Test",
                Address = "Test"
            });

            var httpMessageHandler = new Mock<HttpMessageHandler>();
            httpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .Returns(Task.FromResult(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = JsonContent.Create(data)
                }));

            HttpClient httpClient = new HttpClient(httpMessageHandler.Object);
            httpClient.BaseAddress = new Uri(@"http://api/valid/");
            httpClient.DefaultRequestHeaders.Accept.Clear();
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            IPolicyRegistry<string> mockRegistry = new PolicyRegistry();
            IAsyncPolicy<HttpResponseMessage> httpRetryPolicy = Policy.NoOpAsync<HttpResponseMessage>();
            IAsyncPolicy httpClientTimeoutExceptionPolicy = Policy.NoOpAsync();

            mockRegistry.Add("SimpleHttpWaitAndRetry", httpRetryPolicy);
            mockRegistry.Add("HttpClientTimeout", httpClientTimeoutExceptionPolicy);

            var service = new UsingPolicyRegistryService(mockRegistry, httpClient);

            var exception = await Record.ExceptionAsync(() => service.WaitAndRetry());

            // ASSERT
            Assert.Null(exception);
        }
    }
}
