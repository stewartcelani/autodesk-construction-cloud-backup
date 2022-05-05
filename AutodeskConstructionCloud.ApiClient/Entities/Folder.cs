using Library.Extensions;


namespace AutodeskConstructionCloud.ApiClient.Entities;

public class Folder
{
    private readonly ApiClient _apiClient;

    public Folder(ApiClient apiClient)
    {
        _apiClient = apiClient;
    }
    
    public string FolderId { get; set; }
    public string ProjectId { get; set; }
    public string ParentFolderId { get; set; }
    public Folder? ParentFolder { get; set; }
    public bool IsRootFolder => ParentFolderId.EndsWith("-g");
    public string Name { get; set; }
    public string Type { get; set; }
    public DateTime CreateTime { get; set; }
    public string CreateUserId { get; set; }
    public string CreateUserName { get; set; }
    public string DisplayName { get; set; }
    public bool Hidden { get; set; } 
    public DateTime LastModifiedTime { get; set; }
    public DateTime LastModifiedTimeRollup { get; set; }
    public string LastModifiedUserId { get; set; }
    public string LastModifiedUserName { get; set; }
    public int ObjectCount { get; set; }
    public List<Folder> Subfolders { get; set; } = new List<Folder>();
    public List<File> Files { get; set; } = new List<File>();
    public bool IsEmpty => Subfolders.Count + Files.Count == 0;
    public bool IsNotEmpty => IsEmpty == false;
    public DirectoryInfo? DirectoryInfo { get; set; }
    public bool Created => DirectoryInfo != null;
    public IEnumerable<Folder> SubfoldersRecursive
    {
        get
        {
            return Subfolders.RecursiveFlatten(x => x.Subfolders);
        }
    }
    
    public IEnumerable<File> FilesRecursive
    {
        get
        {
            return Files.Concat(SubfoldersRecursive.SelectMany(x => x.Files));
        }
    }
    public async Task GetContents()
    {
        await _apiClient.GetFolderContents(this);
    }
    public async Task GetContentsRecursively()
    {
        await _apiClient.GetFolderContentsRecursively(this);
    }
}