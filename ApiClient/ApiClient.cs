using System.Net;
using System.Net.Http.Headers;
using System.Text;
using ACC.ApiClient.Entities;
using ACC.ApiClient.RestApiResponses;
using Newtonsoft.Json;
using File = ACC.ApiClient.Entities.File;

namespace ACC.ApiClient;

public class ApiClient : IApiClient
{
    private string? _accessToken;
    private DateTime? _accessTokenExpiresAt;

    public ApiClient(ApiClientConfiguration config)
    {
        Config = config;
    }

    public ApiClientConfiguration Config { get; }

    public async Task<Project> GetProject(string projectId, bool getRootFolderContents = false)
    {
        Config.Logger?.Trace("Top");
        var projectEndpoint = $"https://developer.api.autodesk.com/project/v1/hubs/{Config.HubId}/projects/{projectId}";
        const string moreDetailsUrl =
            "https://forge.autodesk.com/en/docs/data/v2/reference/http/hubs-hub_id-projects-GET/";
        var responseString = string.Empty;
        await Config.RetryPolicy.ExecuteAsync(async () =>
        {
            Config.Logger?.Trace(
                $"Top RetryPolicy, about to call EnsureAccessToken and then folderEndpoint: {projectEndpoint}");
            await EnsureAccessToken();
            HttpResponseMessage response = await Config.HttpClient.GetAsync(projectEndpoint);
            responseString = await response.Content.ReadAsStringAsync();
            HandleUnsuccessfulStatusCode(
                responseString,
                response,
                moreDetailsUrl);
        });
        var projectResponse = JsonConvert.DeserializeObject<ProjectResponse>(responseString);
        if (projectResponse is null)
            throw new ArgumentNullException("projectResponse");
        var project = new Project(this)
        {
            ProjectId = projectResponse.Data.Id,
            AccountId = Config.AccountId,
            Name = projectResponse.Data.Attributes.Name,
            RootFolderId = projectResponse.Data.Relationships.RootFolder.Data.Id,
            RootFolder = getRootFolderContents
                ? await GetFolder(projectResponse.Data.Id, projectResponse.Data.Relationships.RootFolder.Data.Id)
                : null
        };
        Config.Logger?.Debug($"Returning with project name {project.Name}");
        return project;
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
            if (projectsResponses is null)
                throw new ArgumentNullException("projectsResponses");
            projectsResponses.Add(projectsResponse);
            if (projectsResponse.Links?.Next?.Href is not null)
                projectsEndpoint = projectsResponse.Links.Next.Href;
            else
                break;
        } while (true);

        var projects = new List<Project>();
        foreach (ProjectResponseProject project in projectsResponses.SelectMany(projectsResponse =>
                     projectsResponse.Data))
            projects.Add(new Project(this)
            {
                ProjectId = project.Id,
                AccountId = Config.AccountId,
                Name = project.Attributes.Name,
                RootFolderId = project.Relationships.RootFolder.Data.Id,
                RootFolder = getRootFolderContents
                    ? await GetFolder(project.Id, project.Relationships.RootFolder.Data.Id)
                    : null
            });

        Config.Logger?.Debug($"Returning {projects.Count} projects.");
        return projects;
    }

    public async Task<Folder> GetFolder(string projectId, string folderId, bool getFolderContents = false)
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
        if (folderResponse is null)
            throw new ArgumentNullException("folderResponse");
        string parentFolderId = folderResponse.Data.Relationships.Parent.Data.Id;
        Folder folder = MapFolderFromFolderContentsResponseData(projectId, parentFolderId, folderResponse.Data);
        if (getFolderContents) await GetFolderContents(folder);

        Config.Logger?.Debug($"Returning with folderId {folder.FolderId} and name {folder.Name}");
        return folder;
    }

    public async Task GetFolderContentsRecursively(Folder folder)
    {
        Config.Logger?.Trace("Top");
        await GetFolderContents(folder);
        foreach (Folder folderFolder in folder.Subfolders) await GetFolderContentsRecursively(folderFolder);

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
                folderContentsEndpoint = folderContentsResponse.Links.Next.Href;
            else
                break;
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

        Config.Logger?.Trace($"Returning with {folder.Files.Count} files and {folder.Subfolders.Count} folders");
    }

    private static void CreateDirectory(Folder folder, string rootDirectory)
    {
        if (folder.DirectoryInfo is not null) return;
        string path = Path.Combine(rootDirectory, folder.GetPath()[1..]);
        folder.DirectoryInfo = Directory.CreateDirectory(path);
    }

    public static void CreateDirectories(IEnumerable<Folder> folders, string rootDirectory)
    {
        foreach (Folder folder in folders) CreateDirectory(folder, rootDirectory);
    }

    private async Task<string> GetSignedS3DownloadUrl(string bucketKey, string objectKey, CancellationToken ct)
    {
        // Build the endpoint URL
        string signedUrlEndpoint = $"https://developer.api.autodesk.com/oss/v2/buckets/{bucketKey}/objects/{objectKey}/signeds3download?minutesExpiration=30";
        
        Config.Logger?.Debug($"Requesting signed S3 URL from endpoint: {signedUrlEndpoint}");
        
        // Ensure we have a valid access token (this method should already exist in your code)
        await EnsureAccessToken();
        Config.Logger?.Debug($"Using access token with first 5 chars: {(_accessToken?.Length > 5 ? _accessToken.Substring(0, 5) : "null")}...");

        try
        {
            // Make the API call
            var request = new HttpRequestMessage(HttpMethod.Get, signedUrlEndpoint);
            request.Headers.Add("Authorization", $"Bearer {_accessToken}");
            
            // Log all request headers for debugging
            foreach (var header in request.Headers)
            {
                Config.Logger?.Debug($"Request header: {header.Key}: {string.Join(", ", header.Value)}");
            }
            
            Config.Logger?.Debug($"Sending request to {signedUrlEndpoint} with method {request.Method}");
            DateTime startTime = DateTime.Now;
            HttpResponseMessage response = await Config.HttpClient.SendAsync(request, ct);
            TimeSpan duration = DateTime.Now - startTime;
            
            Config.Logger?.Debug($"Received response in {duration.TotalMilliseconds}ms with status code: {(int)response.StatusCode} {response.StatusCode}");
            
            // Log response headers
            foreach (var header in response.Headers)
            {
                Config.Logger?.Debug($"Response header: {header.Key}: {string.Join(", ", header.Value)}");
            }
            
            string responseString = await response.Content.ReadAsStringAsync(ct);
            Config.Logger?.Debug($"Response content length: {responseString.Length} characters");
            
            // For debugging, log a small sample of the response if it's very long
            if (responseString.Length > 100)
            {
                Config.Logger?.Debug($"Response content sample: {responseString.Substring(0, 100)}...");
            }
            else
            {
                Config.Logger?.Debug($"Response content: {responseString}");
            }
            
            // Check if the response is valid JSON
            bool isValidJson = false;
            try 
            {
                var obj = Newtonsoft.Json.JsonConvert.DeserializeObject(responseString);
                isValidJson = obj != null;
                Config.Logger?.Debug($"Response is valid JSON: {isValidJson}");
            }
            catch 
            {
                Config.Logger?.Debug("Response is not valid JSON");
            }

            // Check if the request failed
            if (!response.IsSuccessStatusCode)
            {
                // Log other errors and stop
                Config.Logger?.Error($"Failed to generate signed S3 URL for {bucketKey}/{objectKey}. Status: {response.StatusCode}. Response: {responseString}");
                throw new HttpRequestException($"Failed to generate signed S3 URL: {responseString}", null, response.StatusCode);
            }

            // Parse the response to get the signed URL
            Config.Logger?.Debug($"Parsing response JSON to extract signed URL");
            var signedUrlResponse = Newtonsoft.Json.JsonConvert.DeserializeObject<SignedUrlResponse>(responseString);
            
            if (signedUrlResponse == null || string.IsNullOrEmpty(signedUrlResponse.Url))
            {
                Config.Logger?.Error($"Received empty or invalid signed URL response for {bucketKey}/{objectKey}: {responseString}");
                throw new InvalidOperationException($"Received empty or invalid signed URL response: {responseString}");
            }
            
            Config.Logger?.Debug($"Successfully obtained signed S3 URL for {bucketKey}/{objectKey} with length: {signedUrlResponse.Url.Length}");
            
            // Return the URL exactly as received without any modifications
            return signedUrlResponse.Url;
        }
        catch (TaskCanceledException ex)
        {
            Config.Logger?.Error($"Network timeout or cancellation when requesting signed URL for {bucketKey}/{objectKey}: {ex.Message}");
            throw;
        }
        catch (HttpRequestException ex)
        {
            Config.Logger?.Error($"HTTP error when requesting signed URL for {bucketKey}/{objectKey}: {ex.Message}, Status: {ex.StatusCode}");
            throw;
        }
        catch (Exception ex)
        {
            Config.Logger?.Error($"Unexpected error when requesting signed URL for {bucketKey}/{objectKey}: {ex.GetType().Name}: {ex.Message}");
            throw;
        }
    }

    // Helper class to deserialize the API response
    private class SignedUrlResponse
    {
        [Newtonsoft.Json.JsonProperty("url")]
        public string Url { get; set; }
    }

    public async Task<FileInfo> DownloadFile(
        File file, string rootDirectory, CancellationToken ct = default)
    {
        CreateDirectory(file.ParentFolder, rootDirectory);
        string downloadPath = Path.Combine(rootDirectory, file.GetPath()[1..]);

        if (Config.DryRun)
        {
            await System.IO.File.WriteAllBytesAsync(downloadPath, Array.Empty<byte>(), ct);
            file.FileInfo = new FileInfo(downloadPath);
            file.FileSizeOnDisk = file.FileInfo.Length;
            Config.Logger?.Info($"{file.FileInfo.FullName} ({file.FileSizeOnDiskInMb} MB)");
            return file.FileInfo;
        }

        return await Config.RetryPolicy.ExecuteAsync(async () =>
        {
            try
            {
                // Log the original download URL for debugging
                Config.Logger?.Debug($"Original download URL: {file.DownloadUrl}");
                
                // Extract bucketKey and objectKey from the file.DownloadUrl
                var uri = new Uri(file.DownloadUrl);
                Config.Logger?.Debug($"URL Path: {uri.AbsolutePath}");
                
                var pathSegments = uri.AbsolutePath.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
                
                // Log all path segments for debugging
                for (int i = 0; i < pathSegments.Length; i++)
                {
                    Config.Logger?.Debug($"Path segment [{i}]: {pathSegments[i]}");
                }
                
                // Update indices to correctly extract bucket and object keys
                // The URL format is typically: https://developer.api.autodesk.com/oss/v2/buckets/wip.dm.prod/objects/object-id.rvt
                // When split, the segments are: ["oss", "v2", "buckets", "wip.dm.prod", "objects", "object-id.rvt"]
                string bucketKey = pathSegments[3]; // Corrected from [2] to [3] to get actual bucket name
                string objectKey = pathSegments[5]; // Corrected from [4] to [5] to get actual object ID
                
                Config.Logger?.Debug($"Extracted bucket key: {bucketKey}, object key: {objectKey} from URL: {file.DownloadUrl}");

                // Get signed S3 URL for download
                string signedUrl = await GetSignedS3DownloadUrl(bucketKey, objectKey, ct);
                
                // Log the signed URL (partial for security) for debugging
                if (!string.IsNullOrEmpty(signedUrl))
                {
                    string redactedUrl = signedUrl;
                    int queryIndex = signedUrl.IndexOf('?');
                    
                    if (queryIndex > 0)
                    {
                        // Only log the base URL and not the query parameters that contain credentials
                        redactedUrl = signedUrl.Substring(0, queryIndex) + "?[QUERY_PARAMETERS_REDACTED]";
                    }
                    
                    Config.Logger?.Debug($"Generated signed S3 URL: {redactedUrl}");
                }
                else
                {
                    Config.Logger?.Warn("Generated signed S3 URL is null or empty");
                }
                
                Config.Logger?.Info($"Downloading file from signed URL for {bucketKey}/{objectKey}");
                file.DownloadAttempts++;
                
                // Create a separate HttpClient instance for the download to avoid any default headers
                // that might interfere with the signed URL request
                using (var downloadClient = new HttpClient())
                {
                    // Log the request details before sending
                    Config.Logger?.Debug($"Sending download request to S3 with method: GET");
                    
                    try
                    {
                        // Use HttpClient without any additional headers for signed URL
                        HttpResponseMessage response = await downloadClient.GetAsync(signedUrl, ct);
                        
                        // Log response status code
                        Config.Logger?.Debug($"Download response status code: {(int)response.StatusCode} {response.StatusCode}");
                        
                        // If the request failed, get detailed error information
                        if (!response.IsSuccessStatusCode)
                        {
                            string errorContent = await response.Content.ReadAsStringAsync(ct);
                            Config.Logger?.Error($"S3 download error response: {errorContent}");
                            response.EnsureSuccessStatusCode(); // This will throw with the status code
                        }
                        
                        await using Stream stream = await response.Content.ReadAsStreamAsync(ct);
                        await using FileStream fileStream = new(downloadPath, FileMode.Create);
                        await stream.CopyToAsync(fileStream, ct);
                        file.FileSizeOnDisk = fileStream.Length;
                        file.FileInfo = new FileInfo(downloadPath);
                        
                        Config.Logger?.Info($"Successfully downloaded file {bucketKey}/{objectKey}, size: {file.FileSizeOnDisk} bytes");
                        
                        if (file.FileSizeOnDisk == file.StorageSize)
                            Config.Logger?.Debug($"{file.FileInfo.FullName} ({file.FileSizeOnDiskInMb} MB)");
                        else
                            Config.Logger?.Warn(
                                @$"{file.FileInfo.FullName} ({file.FileSizeOnDiskInMb}/{file.ApiReportedStorageSizeInMb} bytes) (Mismatch between size reported by API and size downloaded to disk)");
                        return file.FileInfo;
                    }
                    catch (HttpRequestException ex)
                    {
                        // Log detailed information about the HTTP exception
                        Config.Logger?.Error($"HTTP error during download: {ex.GetType().Name}: {ex.Message}, Status: {ex.StatusCode}");
                        
                        // Rethrow to be handled by the retry policy
                        throw;
                    }
                }
            }
            catch (Exception ex)
            {
                Config.Logger?.Error($"Error in DownloadFile: {ex.GetType().Name}: {ex.Message}");
                throw;
            }
        });
    }

    public async Task<List<FileInfo>> DownloadFiles(
        IEnumerable<File> fileList, string rootDirectory, CancellationToken ct = default)
    {
        List<FileInfo> fileInfoList = new();

        var parallelOptions = new ParallelOptions
        {
            CancellationToken = ct,
            MaxDegreeOfParallelism = Config.MaxDegreeOfParallelism
        };

        await Parallel.ForEachAsync(fileList, parallelOptions, async (file, ctx) =>
        {
            FileInfo fileInfo = await DownloadFile(file, rootDirectory, ctx);
            fileInfoList.Add(fileInfo);
        });

        return fileInfoList;
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
        return new File
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

    private async Task EnsureAccessToken()
    {
        Config.Logger?.Trace("Top");
        if (_accessTokenExpiresAt < DateTime.Now || _accessTokenExpiresAt is null)
        {
            Config.Logger?.Trace("Access token expired or null, calling GetAccessToken");
            (_accessToken, _accessTokenExpiresAt) = await GetAccessToken();
            Config.HttpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", _accessToken);
        }
    }

    private async Task<(string, DateTime)> GetAccessToken()
    {
        Config.Logger?.Trace("Top");
        const string moreDetailsUrl =
            "https://aps.autodesk.com/en/docs/oauth/v2/reference/http/gettoken-POST";
        const string authenticateEndpoint = "https://developer.api.autodesk.com/authentication/v2/token";
        var clientIdAndSecret = $"{Config.ClientId}:{Config.ClientSecret}";
        var base64ClientIdAndSecretBytes = Convert.ToBase64String(Encoding.UTF8.GetBytes(clientIdAndSecret));
        var values = new Dictionary<string, string>
        {
            { "grant_type", "client_credentials" },
            { "scope", "data:read" }
        };
        var body = new FormUrlEncodedContent(values);
        var responseString = string.Empty;
        await Config.RetryPolicy.ExecuteAsync(async () =>
        {
            Config.Logger?.Trace($"Top RetryPolicy, about to call authenticate endpoint: {authenticateEndpoint}");
            using var requestMessage = new HttpRequestMessage(HttpMethod.Post, authenticateEndpoint);
            requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Basic", base64ClientIdAndSecretBytes);
            requestMessage.Content = body;
            HttpResponseMessage response = await Config.HttpClient.SendAsync(requestMessage);
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
        DateTime accessTokenExpiresAt = DateTime.Now.AddSeconds(modifiedExpiresIn);
        Config.Logger?.Debug(
            $"Authenticated -- returning with accessToken: {accessToken}, accessTokenExpiresAt: {accessTokenExpiresAt:O}");
        return (accessToken, accessTokenExpiresAt);
    }

    private void HandleUnsuccessfulStatusCode(string responseString, HttpResponseMessage response,
        string moreDetailsUrl)
    {
        (HttpStatusCode statusCode, var statusCodeAsInt, Uri requestUri) = (response.StatusCode,
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
}