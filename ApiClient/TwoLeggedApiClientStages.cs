namespace ACC.ApiClient;

public interface IClientIdSelectionStage
{
    public IClientSecretSelectionStage WithClientId(string clientId);
}

public interface IClientSecretSelectionStage
{
    public IAccountIdSelectionStage AndClientSecret(string clientSecret);
}

public interface IAccountIdSelectionStage
{
    public IOptionalConfigurationStage ForAccount(string accountId);
}

public interface IOptionalConfigurationStage
{
    public ICreateApiClientStage WithOptions(Action<ApiClientOptions> options);
    public ApiClient Create();
}

public interface ICreateApiClientStage
{
    public ApiClient Create();
}