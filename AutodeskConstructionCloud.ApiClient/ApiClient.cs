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

    public async Task<List<Project>> GetProjects(bool getRootFolderContents = false)
    {
        Config.Logger?.Trace("Top");
        var projectsEndpoint = $"https://developer.api.autodesk.com/project/v1/hubs/{Config.HubId}/projects";
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

        var projects = new List<Project>();
        foreach (ProjectsResponseProject project in projectsResponses.SelectMany(projectsResponse =>
                     projectsResponse.Data))
        {
            projects.Add(new Project(this)
            {
                ProjectId = project.Id, 
                AccountId = Config.AccountId, 
                Name = project.Attributes.Name,
                RootFolderId = project.Relationships.RootFolder.Data.Id,
                RootFolder = getRootFolderContents ? await GetFolder(project.Id, project.Relationships.RootFolder.Data.Id) : null
            });
        }

        Config.Logger?.Debug($"Returning {projects.Count} projects.");
        return projects;
    }
    
    public async Task<Folder> GetFolderWithContents(string projectId, string folderId)
    {
        Config.Logger?.Trace("Top");
        Folder folder = await GetFolder(projectId, folderId);
        await GetFolderContents(folder);
        Config.Logger?.Debug($"Returning folder (id: {folder.FolderId}, name: {folder.Name}) with " +
                             $"{folder.Files.Count} files and {folder.Subfolders.Count} subfolders");
        return folder;
    }

    public async Task<Folder> GetFolder(string projectId, string folderId)
    {
        Config.Logger?.Trace("Top");
        var folderEndpoint =
            $"https://developer.api.autodesk.com/data/v1/projects/{projectId}/folders/{folderId}";
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

    public async Task GetFolderContentsRecursively(Folder folder)
    {
        Config.Logger?.Trace("Top");
        await GetFolderContents(folder);
        foreach (Folder folderFolder in folder.Subfolders)
        {
            await GetFolderContentsRecursively(folderFolder);
        }

        Config.Logger?.Debug($"Returning folder (id: {folder.FolderId}, name: {folder.Name}) with " +
                             $"{folder.Files.Count} files and {folder.Subfolders.Count} subfolders");
    }
    
    public async Task GetFolderContents(Folder folder)
    {
        Config.Logger?.Trace("Top");
        string folderContentsEndpoint =
            $"https://developer.api.autodesk.com/data/v1/projects/{folder.ProjectId}/folders/{folder.FolderId}/contents";
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

        folder.Files = (from response in folderContentsResponses
            where response.Included is not null
            from included in response.Included
            where included.Relationships?.Storage?.Meta?.Link?.Href is not null
            select MapFileFromFolderContentsResponseIncluded(folder, included)).ToList();

        folder.Subfolders = (from response in folderContentsResponses
            where response.Data is not null
            from data in response.Data
            where data.Type.Equals("folders")
            select MapFolderFromFolderContentsResponseData(folder, data)).ToList();

        Config.Logger?.Debug($"Returning with {folder.Files.Count} files and {folder.Subfolders.Count} folders");
    }

    private Folder MapFolderFromFolderContentsResponseData(string projectId, string parentFolderId,
        FolderContentsResponseData data)
    {
        return new Folder(this)
        {
            FolderId = data.Id,
            ProjectId = projectId,
            ParentFolderId = parentFolderId,
            ParentFolder = null,
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
            Subfolders = new List<Folder>(),
            Files = new List<File>()
        };
    }

    private Folder MapFolderFromFolderContentsResponseData(Folder parentFolder, FolderContentsResponseData data)
    {
        return new Folder(this)
        {
            FolderId = data.Id,
            ProjectId = parentFolder.ProjectId,
            ParentFolderId = parentFolder.FolderId,
            ParentFolder = parentFolder,
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
            Subfolders = new List<Folder>(),
            Files = new List<File>()
        };
    }

    private static File MapFileFromFolderContentsResponseIncluded(Folder parentFolder,
        FolderContentsResponseIncluded included)
    {
        return new File()
        {
            FileId = included.Id,
            ProjectId = parentFolder.ProjectId,
            ParentFolder = parentFolder,
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