using AutodeskConstructionCloud.ApiClient.Entities;
using File = AutodeskConstructionCloud.ApiClient.Entities.File;

namespace AutodeskConstructionCloud.ApiClient;

public interface IApiClient
{
    public Task EnsureAccessToken();
    public Task<List<Project>> GetProjects();
    public Task<Folder> GetFolder(string projectId, string folderId);
    public Task<(List<Folder>, List<File>)> GetFolderContents(string projectId, string folderId);
    public Task<Folder> GetFolderContentsFor(Folder forFolder);
    public Task<Folder> GetFolderWithContents(string projectId, string folderId);
    
}