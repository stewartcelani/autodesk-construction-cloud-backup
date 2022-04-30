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
        var projectsEndpoint = $"https://developer.api.autodesk.com/project/v1/hubs/b.{Config.AccountId}/projects";
        var projectsResponses = new List<ProjectsResponse>();
        do
        {
            var responseString = string.Empty;
            await Config.RetryPolicy.ExecuteAsync(async () =>
            {
                Config.Logger?.Trace("Top RetryPolicy, about to call EnsureAccessToken and then projects endpoint.");
                await EnsureAccessToken();
                HttpResponseMessage response = await Config.HttpClient.GetAsync(projectsEndpoint);
                responseString = await response.Content.ReadAsStringAsync();
                if (!response.IsSuccessStatusCode)
                {
                    HandleUnsuccessfulStatusCode(
                        responseString,
                        response.StatusCode,
                        "https://forge.autodesk.com/en/docs/data/v2/reference/http/hubs-hub_id-projects-GET/");
                }
            });
            var projectsResponse = JsonConvert.DeserializeObject<ProjectsResponse>(responseString);
            projectsResponses.Add(projectsResponse);
            if (projectsResponse.Links?.Next?.Href is not null)
            {
                projectsEndpoint = projectsResponse.Links.Next.Href;
            }
            else
            {
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
        var responseString = string.Empty;
        await Config.RetryPolicy.ExecuteAsync(async () =>
        {
            Config.Logger?.Trace("Top RetryPolicy, about to call authenticate endpoint.");
            const string authenticateEndpoint = "https://developer.api.autodesk.com/authentication/v1/authenticate";
            HttpResponseMessage response = await Config.HttpClient.PostAsync(authenticateEndpoint, body);
            responseString = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
            {
                HandleUnsuccessfulStatusCode(
                    responseString,
                    response.StatusCode,
                    "https://forge.autodesk.com/en/docs/oauth/v1/reference/http/authenticate-POST/");
            }
        });
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
            _accessTokenExpiresAt = DateTime.Now.Subtract(TimeSpan.FromMinutes(1)); // Will force the next EnsureAccessToken to call GetAccessToken();
            var unauthorizedAccessException = new UnauthorizedAccessException(responseString);
            Config.Logger?.Error(unauthorizedAccessException,
                $"Error {(int)statusCode} error getting two-legged access token from Autodesk API. " +
                "This usually indicates an incorrect clientId and/or clientSecret. " +
                $"See {moreDetailsUrl} for more details.");
            throw unauthorizedAccessException;
        }
        var httpRequestException = new HttpRequestException(responseString);
        Config.Logger?.Error(httpRequestException,
            $"Error {(int)statusCode} error communicating with Autodesk API. " +
            $"See {moreDetailsUrl} for more details.");
        throw httpRequestException;
    }
    
}