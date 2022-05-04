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

    /*
    public List<Folder> FoldersRecursive => RootFolder != null
        ? RootFolder.Folders.SelectMany(folder => folder.Folders).ToList()
        : new List<Folder>();

    public List<File> FilesRecursive => RootFolder != null 
        ? RootFolder.Folders.SelectMany(folder => folder.Files).ToList() 
        : new List<File>();
        */
    
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