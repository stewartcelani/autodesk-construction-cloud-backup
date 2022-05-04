using AutodeskConstructionCloud.ApiClient.Entities;
using File = AutodeskConstructionCloud.ApiClient.Entities.File;

namespace AutodeskConstructionCloud.ApiClient;

public interface IApiClient
{
    public Task EnsureAccessToken();
    public Task<List<Project>> GetProjects(bool getRootFolderContents);
    public Task<Folder> GetFolder(string projectId, string folderId);
    public Task<Folder> GetFolderWithContents(string projectId, string folderId);
    public Task GetFolderContents(Folder folder);
    public Task GetFolderContentsRecursively(Folder folder);
}