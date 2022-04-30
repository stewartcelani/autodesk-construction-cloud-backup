using System;
using System.Net;

namespace AutodeskConstructionCloud.ApiClient.Tests;

public class MockHttpMessageHandlerMapping
{
    public string Response { get; set; }
    public HttpStatusCode StatusCode { get; set; }
    public Uri RequestUri { get; set; }
}