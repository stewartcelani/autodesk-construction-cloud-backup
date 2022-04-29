using Newtonsoft.Json;
using AutodeskConstructionCloud.ApiClient.RestApiResponses;

namespace AutodeskConstructionCloud.ApiClient;

public class ApiClient : IApiClient
{
    private HttpClient _http = new();
    private string? _accessToken;
    private DateTime? _accessTokenExpiresAt;
    
    public ApiClientConfiguration Config { get; }

    public ApiClient(ApiClientConfiguration config)
    {
        Config = config;
    }

    public async Task GetAllProjects(string[] projectsToExclude = null, string[] projectsToInclude = null)
    {
        Config.Logger?.Trace("Top");
        await EnsureAccessToken();
    }

    private async Task EnsureAccessToken()
    {
        Config.Logger?.Trace("Top");
        if (_accessTokenExpiresAt is null || _accessTokenExpiresAt < DateTime.Now)
        {
            Config.Logger?.Trace("Access token expired or null, calling GetAccessToken");
            (_accessToken, _accessTokenExpiresAt) = await GetAccessToken();
            _http.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _accessToken);
        }
    }
    
    private async Task<(string, DateTime)> GetAccessToken()
    {
        Config.Logger?.Trace("Top");
        var values = new Dictionary<string, string>
        {
            {"client_id", Config.ClientId},
            {"client_secret", Config.ClientSecret},
            {"grant_type", "client_credentials" },
            {"scope", "account:read account:write data:read" }
        };
        var body = new FormUrlEncodedContent(values);
        return await Config.RetryPolicy.ExecuteAsync(async () =>
        {
            HttpResponseMessage response = await _http.PostAsync("https://developer.api.autodesk.com/authentication/v1/authenticate", body);
            response.EnsureSuccessStatusCode();
            string responseString = await response.Content.ReadAsStringAsync();
            var authResponse = JsonConvert.DeserializeObject<AuthenticateResponse>(responseString);
            string accessToken = authResponse.AccessToken;
            int expiresIn = authResponse.ExpiresIn;
            double modifiedExpiresIn = expiresIn * 0.9;
            DateTime accessTokenExpiresAt = (DateTime.Now).AddSeconds(modifiedExpiresIn);
            Config.Logger?.Debug($"Return with accessToken: {accessToken}, accessTokenExpiresAt: {accessTokenExpiresAt}");
            return (accessToken, accessTokenExpiresAt);
        });
    }
}