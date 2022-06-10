namespace ACC.ApiClient;

public class TwoLeggedApiClient :
    IClientIdSelectionStage,
    IClientSecretSelectionStage,
    IAccountIdSelectionStage,
    IOptionalConfigurationStage,
    ICreateApiClientStage
{
    private string _accountId;
    private string _clientId;
    private string _clientSecret;
    private ApiClientConfiguration? _configuration;

    private TwoLeggedApiClient()
    {
    }

    public IOptionalConfigurationStage ForAccount(string accountId)
    {
        _accountId = accountId;
        return this;
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

    public ICreateApiClientStage WithOptions(Action<ApiClientOptions> config)
    {
        var configuration = new ApiClientConfiguration(_clientId, _clientSecret, _accountId);
        config?.Invoke(configuration);
        configuration.RetryPolicy =
            configuration.GetRetryPolicy(configuration.RetryAttempts, configuration.InitialRetryInSeconds);
        _configuration = configuration;
        return this;
    }

    public ApiClient Create()
    {
        _configuration ??= new ApiClientConfiguration(_clientId, _clientSecret, _accountId);
        if (string.IsNullOrEmpty(_configuration.HubId)) _configuration.HubId = $"b.{_configuration.AccountId}";
        return new ApiClient(_configuration);
    }

    public static IClientIdSelectionStage Configure()
    {
        return new TwoLeggedApiClient();
    }
}