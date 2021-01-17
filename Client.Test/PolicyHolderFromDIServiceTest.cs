using Client.Services;
using Core.ViewModels;
using Moq;
using Moq.Protected;
using Polly;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Client.Test
{
    public class PolicyHolderFromDIServiceTest
    {
        [Fact]
        public async Task CallInavlidApiEndpoint_ThrowsHttpRequestException_MessageMustHave404()
        {
            // ARRANGE
            Mock<HttpMessageHandler> httpMessageHandler = new Mock<HttpMessageHandler>();
            httpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                    .Throws(new HttpRequestException("Response status code does not indicate success: 404 (Not Found)."));

            HttpClient httpClient = new HttpClient(httpMessageHandler.Object);
            httpClient.BaseAddress = new Uri(@"http://api/invalid/");
            httpClient.DefaultRequestHeaders.Accept.Clear();
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var mockPolicyHolder = new Mock<IPolicyHolder>();
            mockPolicyHolder.SetupAllProperties();
            mockPolicyHolder.Object.HttpClientTimeoutException = Policy.NoOpAsync();
            mockPolicyHolder.Object.HttpRetryPolicy = Policy.NoOpAsync<HttpResponseMessage>();

            var service = new PolicyHolderFromDIService(mockPolicyHolder.Object, httpClient);

            // ACT
            var exception = await Record.ExceptionAsync(() => service.WaitAndRetry());

            // ASSERT
            Assert.Contains("404", exception.Message);
        }

        [Fact]
        public async Task CallValidApiEndpoint_DoesNotThrowException()
        {
            // ARRANGE
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
            .Returns(Task.FromResult(new HttpResponseMessage
             {
                 StatusCode = HttpStatusCode.OK,
                 Content = JsonContent.Create(data)
            }));

            HttpClient httpClient = new HttpClient(httpMessageHandler.Object);
            httpClient.BaseAddress = new Uri(@"http://api/valid/");
            httpClient.DefaultRequestHeaders.Accept.Clear();
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var mockPolicyHolder = new Mock<IPolicyHolder>();
            mockPolicyHolder.SetupAllProperties();
            mockPolicyHolder.Object.HttpClientTimeoutException = Policy.NoOpAsync();
            mockPolicyHolder.Object.HttpRetryPolicy = Policy.NoOpAsync<HttpResponseMessage>();

            var service = new PolicyHolderFromDIService(mockPolicyHolder.Object, httpClient);

            // ACT
            var exception = await Record.ExceptionAsync(() => service.WaitAndRetry());
            
            // ASSERT
            Assert.Null(exception);
        }
    }
}
