using AutodeskConstructionCloud.ApiClient.Entities;
using File = AutodeskConstructionCloud.ApiClient.Entities.File;

namespace AutodeskConstructionCloud.ApiClient;

public interface IApiClient
{
    public Task<Project> GetProject(string projectId, bool getRootFolderContents);
    public Task<List<Project>> GetProjects(bool getRootFolderContents);
    public Task<Folder> GetFolder(string projectId, string folderId, bool getFolderContents);
    public Task GetFolderContents(Folder folder);
    public Task GetFolderContentsRecursively(Folder folder);
}