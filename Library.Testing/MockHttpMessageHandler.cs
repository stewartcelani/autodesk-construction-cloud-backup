namespace Library.Testing;

public class MockHttpMessageHandler : HttpMessageHandler
{
    private readonly IEnumerable<MockHttpMessageHandlerMapping> _messageHandlerMappings;

    public MockHttpMessageHandler(MockHttpMessageHandlerMapping messageHandlerMapping)
    {
        _messageHandlerMappings = new List<MockHttpMessageHandlerMapping>
        {
            messageHandlerMapping
        };
    }

    public MockHttpMessageHandler(IEnumerable<MockHttpMessageHandlerMapping> messageHandlerMappings)
    {
        _messageHandlerMappings = messageHandlerMappings;
    }

    public string? Input { get; private set; }
    public int NumberOfCalls { get; private set; }
    public List<HttpRequestMessage> HttpRequestMessages { get; set; } = new();

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        NumberOfCalls++;
        HttpRequestMessages.Add(request);

        if (request.Content is not null) Input = await request.Content.ReadAsStringAsync(cancellationToken);

        MockHttpMessageHandlerMapping mapping =
            _messageHandlerMappings.First(m => m.RequestUri == request.RequestUri);

        return new HttpResponseMessage
        {
            RequestMessage = request,
            StatusCode = mapping.StatusCode,
            Content = new StringContent(mapping.Response)
        };
    }
}