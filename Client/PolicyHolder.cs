using Polly;
using Polly.Retry;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;

namespace Client
{
    public class PolicyHolder
    {
        public AsyncRetryPolicy<HttpResponseMessage> HttpRetryPolicy { get; }

        public AsyncRetryPolicy HttpClientTimeoutException { get; }

        public PolicyHolder()
        {
            // Wait and Retry with delegate
            HttpRetryPolicy =
                Policy.HandleResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode)
                    .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt) / 2), onRetry: (httpResponseMessage, retryCount) =>
                    {
                        // Log result
                        Console.WriteLine(httpResponseMessage.Result);

                        Console.WriteLine($"Retrying...");
                    });

            HttpClientTimeoutException = Policy.Handle<HttpRequestException>()
                .WaitAndRetryAsync(1, retryAttempt => TimeSpan.FromSeconds(retryAttempt),
                    onRetry: (exception, timespan) =>
                    {
                        string message = exception.Message;
                        // log the message.
                    }
                );
        }
    }
}
