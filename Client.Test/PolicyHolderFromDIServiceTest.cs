using Client.Services;
using Moq;
using Moq.Protected;
using Polly;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Client.Test
{
    public class PolicyHolderFromDIServiceTest
    {
        [Fact]
        public async Task GetContactsAsStream_On401Response_MustThrowUnauthorizedApiAccessException()
        {
            //bool fakeInventoryResponse = true;
            Mock<HttpMessageHandler> httpMessageHandler = new Mock<HttpMessageHandler>();
            httpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()).Throws(new HttpRequestException("Response status code does not indicate success: 404 (Not Found)."));
                //.Returns(Task.FromResult(new HttpResponseMessage
                //{
                //    StatusCode = HttpStatusCode.OK,
                //    Content = new StringContent(fakeInventoryResponse.ToString(), Encoding.UTF8, "application/json"),
                //}));

            HttpClient httpClient = new HttpClient(httpMessageHandler.Object);
            httpClient.BaseAddress = new Uri(@"http://some.invalidurl.com/v1/");
            httpClient.DefaultRequestHeaders.Accept.Clear();
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var mockPolicyHolder = new Mock<IPolicyHolder>();
            mockPolicyHolder.SetupAllProperties();
            mockPolicyHolder.Object.HttpClientTimeoutException = Policy.NoOpAsync();
            mockPolicyHolder.Object.HttpRetryPolicy = Policy.NoOpAsync<HttpResponseMessage>();

            var service = new PolicyHolderFromDIService(mockPolicyHolder.Object, httpClient);

            var exception = await Record.ExceptionAsync(() => service.WaitAndRetry());
           //var exception = await Assert.ThrowsAsync<HttpRequestException>(
           //      () => service.WaitAndRetry());

            Assert.Contains("404", exception.Message);


            //Assert.True(service.WaitAndRetry());

        }

        [Fact]
        public async Task GetContactsAsStream_On401Response_MustThrowUnauthorizedApiAccessException1()
        {
            bool fakeInventoryResponse = true;
            Mock<HttpMessageHandler> httpMessageHandler = new Mock<HttpMessageHandler>();
            httpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
            .Returns(Task.FromResult(new HttpResponseMessage
             {
                 StatusCode = HttpStatusCode.OK,
                 Content = new StringContent(fakeInventoryResponse.ToString(), Encoding.UTF8, "application/json"),
             }));

            HttpClient httpClient = new HttpClient(httpMessageHandler.Object);
            httpClient.BaseAddress = new Uri(@"http://some.invalidurl.com/v1/");
            httpClient.DefaultRequestHeaders.Accept.Clear();
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var mockPolicyHolder = new Mock<IPolicyHolder>();
            mockPolicyHolder.SetupAllProperties();
            mockPolicyHolder.Object.HttpClientTimeoutException = Policy.NoOpAsync();
            mockPolicyHolder.Object.HttpRetryPolicy = Policy.NoOpAsync<HttpResponseMessage>();

            var service = new PolicyHolderFromDIService(mockPolicyHolder.Object, httpClient);

            var exception = await Record.ExceptionAsync(() => service.WaitAndRetry());

            Assert.Null(exception);

            
            //Assert.True(service.WaitAndRetry());

        }
    }
}
