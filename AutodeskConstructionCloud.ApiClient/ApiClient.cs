namespace AutodeskConstructionCloud.ApiClient;

public class ApiClient
{
    public ApiClientConfiguration Config { get; }
    private HttpClient _http = new();
    
    public ApiClient(ApiClientConfiguration config)
    {
        Config = config;
    }

    public void Example()
    {
        Config.Logger?.Trace("Example of TRACE logging via NLog.");
        Config.Logger?.Info("Example of INFO logging via NLog.");
    }
}