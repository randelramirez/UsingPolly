using Client.Test.HandlersStub;
using Moq;
using Moq.Protected;
using Polly;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Client.Test
{
    // Note for the following unit tests, we can actually replace the handler stubs as using Moq objects
    public class SampleServiceTest
    {
        [Fact]
        public void GetContactsAsStream_On401Response_MustThrowUnauthorizedApiAccessException()
        {
            // ARRANGE
            IAsyncPolicy<HttpResponseMessage> mockPolicy = Policy.NoOpAsync<HttpResponseMessage>();
            var httpClient = new HttpClient(new Return401UnauthorizedResponseHandler());
            var testableClass = new SampleService(mockPolicy, httpClient);

            //var cancellationTokenSource = new CancellationTokenSource();

            // ACT, ASSERT(GetContactsAsStream())
            Assert.ThrowsAsync<UnauthorizedApiAccessException>(
                () => testableClass.GetContactsAsStream());
        }

        [Fact]
        public void GetContactsAsStream_On401Response_MustThrowUnauthorizedApiAccessException_WithMoq()
        {
            // ARRANGE
            var unauthorizedResponseHttpMessageHandlerMock = new Mock<HttpMessageHandler>();
            // so we can setup a protected method named SendAsync inside HttpMessageHandler
            unauthorizedResponseHttpMessageHandlerMock.Protected()
                  .Setup<Task<HttpResponseMessage>>(
                  "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                ).ReturnsAsync(new HttpResponseMessage()
                {
                    StatusCode = HttpStatusCode.Unauthorized
                });

            IAsyncPolicy<HttpResponseMessage> mockPolicy = Policy.NoOpAsync<HttpResponseMessage>();
            var httpClient = new HttpClient(unauthorizedResponseHttpMessageHandlerMock.Object);
            var testableClass = new SampleService(mockPolicy, httpClient);

            // ACT, ASSERT(GetContactsAsStream())
            Assert.ThrowsAsync<UnauthorizedApiAccessException>(()
                => testableClass.GetContactsAsStream());
        }

        [Fact]
        public async Task GetContactsAsStream_On200Response_ReturnsCorrectNumberOfContacts()
        {
            // ARRANGE
            IAsyncPolicy<HttpResponseMessage> mockPolicy = Policy.NoOpAsync<HttpResponseMessage>();
            var httpClient = new HttpClient(new Return200OkResponseHandler());
            var testableClass = new SampleService(mockPolicy, httpClient);

            // ACT
            //var cancellationTokenSource = new CancellationTokenSource();
            var data = await testableClass.GetContactsAsStream();

            // ASSERT
            Assert.Equal(2, data.Count());
        }
    }
}
