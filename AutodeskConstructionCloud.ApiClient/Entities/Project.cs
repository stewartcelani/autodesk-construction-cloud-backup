using Library.Extensions;

namespace AutodeskConstructionCloud.ApiClient.Entities;

public class Project
{
    private readonly ApiClient _apiClient;

    public Project(ApiClient apiClient)
    {
        _apiClient = apiClient;
    }
    
    public string ProjectId { get; set; }
    public string AccountId { get; set; }
    public string Name { get; set; }
    
    public string RootFolderId { get; set; }
    public Folder? RootFolder { get; set; }
    
    public IEnumerable<Folder> SubfoldersRecursive
    {
        get
        {
            return RootFolder is null ? new List<Folder>() : RootFolder.Subfolders.RecursiveFlatten(x => x.Subfolders);
        }
    }
    
    public IEnumerable<File> FilesRecursive
    {
        get
        {
            return RootFolder is null ? new List<File>() : RootFolder.Files.Concat(SubfoldersRecursive.SelectMany(x => x.Files));
        }
    }

    public async Task GetContents()
    {
        RootFolder ??= await _apiClient.GetFolder(ProjectId, RootFolderId);
        await RootFolder.GetContents();
    }

    public async Task GetContentsRecursively()
    {
        RootFolder ??= await _apiClient.GetFolder(ProjectId, RootFolderId);
        await RootFolder.GetContentsRecursively();
    }
}