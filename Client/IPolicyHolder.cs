using Polly;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;

namespace Client
{
    public interface IPolicyHolder
    {
        IAsyncPolicy HttpClientTimeoutException { get; set; }

        IAsyncPolicy<HttpResponseMessage> HttpRetryPolicy { get; set; }
    }
}


