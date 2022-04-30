using System.Net;
using Newtonsoft.Json;
using AutodeskConstructionCloud.ApiClient.RestApiResponses;
using NLog;
using Polly.Retry;

namespace AutodeskConstructionCloud.ApiClient;

public class ApiClient : IApiClient
{
    private string? _accessToken;
    private DateTime? _accessTokenExpiresAt;
    
    public ApiClientConfiguration Config { get; private set; }

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
            Config.Logger?.Debug("Access token expired or null, calling GetAccessToken");
            (_accessToken, _accessTokenExpiresAt) = await GetAccessToken();
            Config.HttpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _accessToken);
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
            HttpResponseMessage response = await Config.HttpClient.PostAsync("https://developer.api.autodesk.com/authentication/v1/authenticate", body);
            string responseString = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
            {
                HandleUnsuccessfulStatusCode(responseString, response.StatusCode);
            }
            var authResponse = JsonConvert.DeserializeObject<AuthenticateResponse>(responseString);
            string accessToken = authResponse.AccessToken;
            int expiresIn = authResponse.ExpiresIn;
            double modifiedExpiresIn = expiresIn * 0.9;
            DateTime accessTokenExpiresAt = (DateTime.Now).AddSeconds(modifiedExpiresIn);
            Config.Logger?.Debug($"Return with accessToken: {accessToken}, accessTokenExpiresAt: {accessTokenExpiresAt}");
            return (accessToken, accessTokenExpiresAt);
        });
    }

    private void HandleUnsuccessfulStatusCode(string responseString, HttpStatusCode statusCode)
    {
        if (statusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
        {
            var unauthorizedAccessException = new UnauthorizedAccessException(responseString);
            Config.Logger?.Fatal(unauthorizedAccessException,
                $"Fatal {(int)statusCode} error getting two-legged access token from Autodesk API. " +
                "This usually indicates an incorrect clientId and/or clientSecret. All retries exceeded." +
                "See https://forge.autodesk.com/en/docs/oauth/v1/reference/http/authenticate-POST/ for more details.");
            throw unauthorizedAccessException;
        }
        
        var httpRequestException = new HttpRequestException(responseString);
        Config.Logger?.Fatal(httpRequestException,
            $"Fatal {(int)statusCode} error getting two-legged access token from Autodesk API. " +
            "All retries exceeded." +
            "See https://forge.autodesk.com/en/docs/oauth/v1/reference/http/authenticate-POST/ for more details.");
        throw httpRequestException;
    }
}