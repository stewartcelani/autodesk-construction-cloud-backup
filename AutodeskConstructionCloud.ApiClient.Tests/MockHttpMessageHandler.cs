using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace AutodeskConstructionCloud.ApiClient.Tests;

public class MockHttpMessageHandler : HttpMessageHandler
{
    private readonly IEnumerable<MockHttpMessageHandlerMapping> _messageHandlerMappings;

    public string? Input { get; private set; }
    public int NumberOfCalls { get; private set; }


    public MockHttpMessageHandler(MockHttpMessageHandlerMapping messageHandlerMapping)
    {
        _messageHandlerMappings = new List<MockHttpMessageHandlerMapping>()
        {
            messageHandlerMapping
        };
    }
    
    public MockHttpMessageHandler(IEnumerable<MockHttpMessageHandlerMapping> messageHandlerMappings)
    {
        _messageHandlerMappings = messageHandlerMappings;
    }
    
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        NumberOfCalls++;

        if (request.Content is not null)
        {
            Input = await request.Content.ReadAsStringAsync(cancellationToken);
        }

        MockHttpMessageHandlerMapping mapping = 
            _messageHandlerMappings.First(m => m.RequestUri == request.RequestUri);
        return new HttpResponseMessage
        {
            StatusCode = mapping.StatusCode,
            Content = new StringContent(mapping.Response)
        };
    }
}