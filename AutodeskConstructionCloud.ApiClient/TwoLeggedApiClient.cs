namespace AutodeskConstructionCloud.ApiClient;

public class TwoLeggedApiClient : 
    IClientIdSelectionStage, 
    IClientSecretSelectionStage, 
    IAccountIdSelectionStage,
    IOptionalConfigurationStage,
    ICreateApiClientStage
{
    private string _clientId;
    private string _clientSecret;
    private string _accountId;
    private ApiClientConfiguration? _configuration;

    private TwoLeggedApiClient() {}

    public static IClientIdSelectionStage Configure()
    {
        return new TwoLeggedApiClient();
    }

    public IClientSecretSelectionStage WithClientId(string clientId)
    {
        _clientId = clientId;
        return this;
    }

    public IAccountIdSelectionStage AndClientSecret(string clientSecret)
    {
        _clientSecret = clientSecret;
        return this;
    }

    public IOptionalConfigurationStage ForAccount(string accountId)
    {
        _accountId = accountId;
        return this;
    }

    public ICreateApiClientStage WithOptions(Action<ApiClientOptions> config)
    {
        var configuration = new ApiClientConfiguration(_clientId, _clientSecret, _accountId);
        config?.Invoke(configuration);
        _configuration = configuration;
        return this;
    }

    public ApiClient CreateApiClient()
    {
        if (_configuration is null)
            _configuration = new ApiClientConfiguration(_clientId, _clientSecret, _accountId);
        return new ApiClient(_configuration);
    }
}