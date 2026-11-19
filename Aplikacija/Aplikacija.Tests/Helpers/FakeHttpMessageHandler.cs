using System.Net;

namespace Aplikacija.Tests.Helpers;

/// <summary>Zamjenski objekat za vanjski sistem (PandaScore API): vraca unaprijed zadan odgovor.</summary>
public class FakeHttpMessageHandler : HttpMessageHandler
{
    private readonly HttpStatusCode _status;
    private readonly string _content;

    public HttpRequestMessage? LastRequest { get; private set; }

    public FakeHttpMessageHandler(HttpStatusCode status, string content)
    {
        _status = status;
        _content = content;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        LastRequest = request;
        return Task.FromResult(new HttpResponseMessage(_status) { Content = new StringContent(_content) });
    }
}
