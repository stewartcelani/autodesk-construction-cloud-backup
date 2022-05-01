using System.Net;
using AutodeskConstructionCloud.ApiClient.Entities;
using Newtonsoft.Json;
using AutodeskConstructionCloud.ApiClient.RestApiResponses;
using File = AutodeskConstructionCloud.ApiClient.Entities.File;

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
        const string moreDetailsUrl =
            "https://forge.autodesk.com/en/docs/data/v2/reference/http/hubs-hub_id-projects-GET/";
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
                HandleUnsuccessfulStatusCode(
                    responseString,
                    response,
                    moreDetailsUrl);
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

    public async Task<Folder> GetFolderContentsFor(Folder folder)
    {
        Config.Logger?.Trace("Top");
        (folder.Folders, folder.Files) = await GetFolderContents(folder.ProjectId, folder.FolderId);
        Config.Logger?.Debug($"Returning folder (id: {folder.FolderId}, name: {folder.Name}) with " +
                             $"{folder.Files.Count} files and {folder.Folders.Count} subfolders");
        return folder;
    }

    public async Task<Folder> GetFolderWithContents(string projectId, string folderId)
    {
        Config.Logger?.Trace("Top");
        Folder folder = await GetFolder(projectId, folderId);
        (folder.Folders, folder.Files) = await GetFolderContents(projectId, folderId);
        Config.Logger?.Debug($"Returning folder (id: {folder.FolderId}, name: {folder.Name}) with " +
                             $"{folder.Files.Count} files and {folder.Folders.Count} subfolders");
        return folder;
    }

    public async Task<Folder> GetFolder(string projectId, string folderId)
    {
        Config.Logger?.Trace("Top");
        var folderEndpoint =
            $"https://developer.api.autodesk.com/data/v1/projects/b.{projectId}/folders/{folderId}";
        const string moreDetailsUrl =
            "https://forge.autodesk.com/en/docs/data/v2/reference/http/projects-project_id-folders-folder_id-contents-GET/";
        var responseString = string.Empty;
        await Config.RetryPolicy.ExecuteAsync(async () =>
        {
            Config.Logger?.Trace(
                $"Top RetryPolicy, about to call EnsureAccessToken and then folderEndpoint: {folderEndpoint}");
            await EnsureAccessToken();
            HttpResponseMessage response = await Config.HttpClient.GetAsync(folderEndpoint);
            responseString = await response.Content.ReadAsStringAsync();
            HandleUnsuccessfulStatusCode(
                responseString,
                response,
                moreDetailsUrl);
        });
        var folderResponse = JsonConvert.DeserializeObject<FolderResponse>(responseString);
        string parentFolderId = folderResponse.Data.Relationships.Parent.Data.Id;
        Folder folder = MapFolderFromFolderContentsResponseData(projectId, parentFolderId, folderResponse.Data);
        Config.Logger?.Debug($"Returning with folderId {folder.FolderId} and name {folder.Name}");
        return folder;
    }


    public async Task<(List<Folder>, List<File>)> GetFolderContents(string projectId, string folderId)
    {
        Config.Logger?.Trace("Top");
        string folderContentsEndpoint =
            $"https://developer.api.autodesk.com/data/v1/projects/b.{projectId}/folders/{folderId}/contents";
        const string moreDetailsUrl =
            "https://forge.autodesk.com/en/docs/data/v2/reference/http/projects-project_id-folders-folder_id-contents-GET/";
        var folderContentsResponses = new List<FolderContentsResponse>();
        do
        {
            var responseString = string.Empty;
            await Config.RetryPolicy.ExecuteAsync(async () =>
            {
                Config.Logger?.Trace(
                    $"Top RetryPolicy, about to call EnsureAccessToken and then folderContentsEndpoint: {folderContentsEndpoint}");
                await EnsureAccessToken();
                HttpResponseMessage response = await Config.HttpClient.GetAsync(folderContentsEndpoint);
                responseString = await response.Content.ReadAsStringAsync();
                HandleUnsuccessfulStatusCode(
                    responseString,
                    response,
                    moreDetailsUrl);
            });
            var folderContentsResponse = JsonConvert.DeserializeObject<FolderContentsResponse>(responseString);
            folderContentsResponses.Add(folderContentsResponse);
            if (folderContentsResponse.Links.Next?.Href is not null)
            {
                folderContentsEndpoint = folderContentsResponse.Links.Next.Href;
            }
            else
            {
                break;
            }
        } while (true);

        List<File> filesInFolder = (from response in folderContentsResponses
            from included in response.Included
            where included.Relationships.Storage.Meta.Link.Href is not null
            select MapFileFromFolderContentsResponseIncluded(projectId, folderId, included)).ToList();
        
        List<Folder> foldersInFolder = (from response in folderContentsResponses
            from data in response.Data
            where data.Type.Equals("folders")
            select MapFolderFromFolderContentsResponseData(projectId, folderId, data)).ToList();
        
        Config.Logger?.Debug($"Returning with {filesInFolder.Count} files and {foldersInFolder.Count} folders");
        return (foldersInFolder, filesInFolder);
    }

    private static Folder MapFolderFromFolderContentsResponseData(string projectId, string parentFolderId,
        FolderContentsResponseData data)
    {
        return new Folder()
        {
            FolderId = data.Id,
            ProjectId = projectId,
            ParentFolderId = parentFolderId,
            Name = data.Attributes.Name,
            Type = data.Type,
            CreateTime = data.Attributes.CreateTime,
            CreateUserId = data.Attributes.CreateUserId,
            CreateUserName = data.Attributes.CreateUserName,
            DisplayName = data.Attributes.DisplayName,
            Hidden = data.Attributes.Hidden,
            LastModifiedTime = data.Attributes.LastModifiedTime,
            LastModifiedTimeRollup = data.Attributes.LastModifiedTimeRollup,
            LastModifiedUserId = data.Attributes.LastModifiedUserId,
            LastModifiedUserName = data.Attributes.LastModifiedUserName,
            ObjectCount = data.Attributes.ObjectCount,
            Folders = new List<Folder>(),
            Files = new List<File>()
        };
    }

    private static File MapFileFromFolderContentsResponseIncluded(string projectId, string folderId, FolderContentsResponseIncluded included)
    {
        return new File()
        {
            FileId = included.Id,
            FolderId = folderId,
            ProjectId = projectId,
            Name = included.Attributes.Name,
            Type = included.Type,
            DownloadUrl = included.Relationships.Storage.Meta.Link.Href,
            CreateTime = included.Attributes.CreateTime,
            CreateUserId = included.Attributes.CreateUserId,
            CreateUserName = included.Attributes.CreateUserName,
            DisplayName = included.Attributes.DisplayName,
            LastModifiedTime = included.Attributes.LastModifiedTime,
            LastModifiedUserId = included.Attributes.LastModifiedUserId,
            LastModifiedUserName = included.Attributes.LastModifiedUserName,
            VersionNumber = included.Attributes.VersionNumber,
            FileType = included.Attributes.FileType,
            StorageSize = included.Attributes.StorageSize,
            Hidden = included.Attributes.Hidden,
            Reserved = included.Attributes.Reserved,
            ReservedTime = included.Attributes.ReservedTime,
            ReservedUserId = included.Attributes.ReservedUserId,
            ReservedUserName = included.Attributes.ReservedUserName
        };
    }

    #region Authentication

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
        const string moreDetailsUrl =
            "https://forge.autodesk.com/en/docs/oauth/v1/reference/http/authenticate-POST/";
        const string authenticateEndpoint = "https://developer.api.autodesk.com/authentication/v1/authenticate";
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
            Config.Logger?.Trace($"Top RetryPolicy, about to call authenticate endpoint: {authenticateEndpoint}");
            HttpResponseMessage response = await Config.HttpClient.PostAsync(authenticateEndpoint, body);
            responseString = await response.Content.ReadAsStringAsync();
            HandleUnsuccessfulStatusCode(
                responseString,
                response,
                moreDetailsUrl);
        });
        var authResponse = JsonConvert.DeserializeObject<AuthenticateResponse>(responseString);
        string accessToken = authResponse.AccessToken;
        int expiresIn = authResponse.ExpiresIn;
        double modifiedExpiresIn = expiresIn * 0.9;
        DateTime accessTokenExpiresAt = (DateTime.Now).AddSeconds(modifiedExpiresIn);
        Config.Logger?.Debug(
            $"Authenticated -- returning with accessToken: {accessToken}, accessTokenExpiresAt: {accessTokenExpiresAt:O}");
        return (accessToken, accessTokenExpiresAt);
    }

    private void HandleUnsuccessfulStatusCode(string responseString, HttpResponseMessage response,
        string moreDetailsUrl)
    {
        (HttpStatusCode statusCode, int statusCodeAsInt, Uri requestUri) = (response.StatusCode,
            (int)response.StatusCode, response.RequestMessage!.RequestUri!);
        if (statusCodeAsInt is >= 200 and <= 299) return;
        Config.Logger?.Trace($"Top after guard clause (status code: ${statusCodeAsInt})");
        switch (statusCode)
        {
            case HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden:
            {
                _accessTokenExpiresAt = DateTime.Now.Subtract(TimeSpan.FromMinutes(1));
                Config.HttpClient.DefaultRequestHeaders.Authorization = null;
                var unauthorizedAccessException = new HttpRequestException(responseString, null, statusCode);
                Config.Logger?.Error(unauthorizedAccessException,
                    $"Error {statusCodeAsInt} error obtaining two-legged access token from {requestUri}. " +
                    "This usually indicates an incorrect clientId and/or clientSecret. " +
                    $"See {moreDetailsUrl} for more details.");
                throw unauthorizedAccessException;
            }
            case HttpStatusCode.NotFound:
            {
                var notFoundException = new HttpRequestException(responseString, null, statusCode);
                Config.Logger?.Error(notFoundException, $"{requestUri} not found.");
                throw notFoundException;
            }
            default:
            {
                var httpRequestException = new HttpRequestException(responseString, null, statusCode);
                Config.Logger?.Error(httpRequestException,
                    $"Error {statusCodeAsInt} received from {requestUri}. " +
                    $"See {moreDetailsUrl} for more details.");
                throw httpRequestException;
            }
        }
    }

    #endregion
}