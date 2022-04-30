using System.Net;
using System.Reflection;
using System.Runtime.CompilerServices;
using AutodeskConstructionCloud.ApiClient.Entities;
using Newtonsoft.Json;
using AutodeskConstructionCloud.ApiClient.RestApiResponses;
using NLog;
using NLog.Fluent;
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

    public async Task<List<Project>> GetProjects()
    {
        Config.Logger?.Trace("Top");
        await EnsureAccessToken();

        var projectsEndpoint = $"https://developer.api.autodesk.com/project/v1/hubs/b.{Config.AccountId}/projects";
        var projectsResponses = new List<ProjectsResponse>();
        Config.Logger?.Trace("Entering pagination while loop.");
        do
        {
            HttpResponseMessage response = await Config.RetryPolicy.ExecuteAsync(async () =>
                await Config.HttpClient.GetAsync(projectsEndpoint));
            string responseString = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
            {
                HandleUnsuccessfulStatusCode(
                    responseString,
                    response.StatusCode,
                    "https://forge.autodesk.com/en/docs/data/v2/reference/http/hubs-hub_id-projects-GET/");
            }
            var projectsResponse = JsonConvert.DeserializeObject<ProjectsResponse>(responseString);
            projectsResponses.Add(projectsResponse);
            if (projectsResponse.Links?.Next?.Href is not null)
            {
                projectsEndpoint = projectsResponse.Links.Next.Href;
            }
            else
            {
                Config.Logger?.Trace("No more pagination needed. Breaking.");
                break;
            }
        } while (true);

        List<Project> projects = (from projectsResponse in projectsResponses
            from project in projectsResponse.Data
            select new Project
            {
                ProjectId = project.Id, 
                AccountId = Config.AccountId, 
                Name = project.Attributes.Name,
                RootFolderId = project.Relationships.RootFolder.Data.Id
            }).ToList();

        Config.Logger?.Debug($"Returning {projects.Count} projects.");
        return projects;
    }

    public async Task EnsureAccessToken()
    {
        Config.Logger?.Trace("Top");
        if (_accessTokenExpiresAt is null || _accessTokenExpiresAt < DateTime.Now)
        {
            Config.Logger?.Debug("Access token expired or null, calling GetAccessToken");
            (_accessToken, _accessTokenExpiresAt) = await GetAccessToken();
            Config.HttpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _accessToken);
        }
    }

    private async Task<(string, DateTime)> GetAccessToken()
    {
        Config.Logger?.Trace("Top");
        var values = new Dictionary<string, string>
        {
            { "client_id", Config.ClientId },
            { "client_secret", Config.ClientSecret },
            { "grant_type", "client_credentials" },
            { "scope", "account:read account:write data:read" }
        };
        var body = new FormUrlEncodedContent(values);
        const string authenticateEndpoint = "https://developer.api.autodesk.com/authentication/v1/authenticate";
        HttpResponseMessage response = await Config.RetryPolicy.ExecuteAsync(async () =>
            await Config.HttpClient.PostAsync(authenticateEndpoint, body));
        string responseString = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
        {
            HandleUnsuccessfulStatusCode(
                responseString,
                response.StatusCode,
                "https://forge.autodesk.com/en/docs/oauth/v1/reference/http/authenticate-POST/");
        }

        var authResponse = JsonConvert.DeserializeObject<AuthenticateResponse>(responseString);
        string accessToken = authResponse.AccessToken;
        int expiresIn = authResponse.ExpiresIn;
        double modifiedExpiresIn = expiresIn * 0.9;
        DateTime accessTokenExpiresAt = (DateTime.Now).AddSeconds(modifiedExpiresIn);
        Config.Logger?.Debug(
            $"Authenticated. Returning with accessToken: {accessToken}, accessTokenExpiresAt: {accessTokenExpiresAt:O}");
        return (accessToken, accessTokenExpiresAt);
    }

    private void HandleUnsuccessfulStatusCode(string responseString, HttpStatusCode statusCode, string moreDetailsUrl)
    {
        Config.Logger?.Trace("Top");
        if (statusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
        {
            var unauthorizedAccessException = new UnauthorizedAccessException(responseString);
            Config.Logger?.Fatal(unauthorizedAccessException,
                $"Fatal {(int)statusCode} error getting two-legged access token from Autodesk API. " +
                "This usually indicates an incorrect clientId and/or clientSecret. All retries exceeded. " +
                $"See {moreDetailsUrl} for more details.");
            throw unauthorizedAccessException;
        }

        var httpRequestException = new HttpRequestException(responseString);
        Config.Logger?.Fatal(httpRequestException,
            $"Fatal {(int)statusCode} error communicating with Autodesk API. " +
            "All retries exceeded. " +
            $"See {moreDetailsUrl} for more details.");
        throw httpRequestException;
    }
}