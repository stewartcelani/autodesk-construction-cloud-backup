using System.Net;

namespace Library.Testing;

public class MockHttpMessageHandlerMapping
{
    public string Response { get; set; }
    public HttpStatusCode StatusCode { get; set; }
    public Uri RequestUri { get; set; }
}