using ACC.ApiClient.Entities;

namespace ACC.ApiClient;

public interface IApiClient
{
    public Task<Project> GetProject(string projectId, bool getRootFolderContents);
    public Task<List<Project>> GetProjects(bool getRootFolderContents);
    public Task<Folder> GetFolder(string projectId, string folderId, bool getFolderContents);
    public Task GetFolderContents(Folder folder);
    public Task GetFolderContentsRecursively(Folder folder);
}