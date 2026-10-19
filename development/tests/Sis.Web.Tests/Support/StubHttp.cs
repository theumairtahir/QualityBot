using System.Net;
using System.Net.Http.Json;

namespace Sis.Web.Tests.Support;

/// <summary>An HttpClient whose responses come from a delegate — no network.</summary>
internal static class StubHttp
{
    public static readonly Uri BaseAddress = new("http://api.test/");

    public static HttpClient Create(Func<HttpRequestMessage, HttpResponseMessage> respond) =>
        new(new Handler(respond)) { BaseAddress = BaseAddress };

    public static HttpResponseMessage Json<T>(T body) =>
        new(HttpStatusCode.OK) { Content = JsonContent.Create(body) };

    private sealed class Handler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(respond(request));
    }
}
