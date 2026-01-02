using System.Net;

namespace McpHealthServer.Tests.Fake;

public class FakeHttpClientFactory : IHttpClientFactory
{
    private readonly HttpStatusCode _status;

    public FakeHttpClientFactory(HttpStatusCode status)
    {
        _status = status;
    }

    public HttpClient CreateClient(string name)
    {
        return new HttpClient(new FakeHandler(_status));
    }

    private class FakeHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _status;

        public FakeHandler(HttpStatusCode status)
        {
            _status = status;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(new HttpResponseMessage(_status));
        }
    }
}