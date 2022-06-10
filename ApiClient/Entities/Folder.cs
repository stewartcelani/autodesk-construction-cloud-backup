using System.Text;
using Library.Extensions;

namespace ACC.ApiClient.Entities;

public class Folder
{
    private readonly ApiClient _apiClient;

    public Folder(ApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    /*
     * These properties are mapped from Autodesk Api
     */
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

    /*
     * Properties not directly from the Autodesk Api are below
     */
    public string FolderId { get; set; }
    public string ProjectId { get; set; }
    public string ParentFolderId { get; set; }
    public Folder? ParentFolder { get; set; }
    public bool IsRootFolder => ParentFolderId.EndsWith("-g");
    public List<Folder> Subfolders { get; set; } = new();
    public List<File> Files { get; set; } = new();
    public bool IsEmpty => Subfolders.Count + Files.Count == 0;
    public bool IsNotEmpty => IsEmpty == false;
    public DirectoryInfo? DirectoryInfo { get; set; }
    public bool Created => DirectoryInfo != null;
    public IEnumerable<Folder> SubfoldersRecursive => Subfolders.FlattenRecursive(x => x.Subfolders);
    public IEnumerable<File> FilesRecursive => Files.Concat(SubfoldersRecursive.SelectMany(x => x.Files));

    public async Task GetContents()
    {
        await _apiClient.GetFolderContents(this);
    }

    public async Task GetContentsRecursively()
    {
        await _apiClient.GetFolderContentsRecursively(this);
    }

    public string GetPath(string? rootFolderId = null, StringBuilder? sb = null, string delimiter = @"\")
    {
        var thisFolderPath = $"{delimiter}{Name}";

        if (sb is null)
            sb = new StringBuilder(thisFolderPath);
        else
            sb.Insert(0, thisFolderPath);

        if (FolderId == rootFolderId || ParentFolder is null) return sb.ToString();

        return ParentFolder.GetPath(rootFolderId, sb);
    }

    public async Task DownloadContents(string downloadPath, CancellationToken ct = default)
    {
        ApiClient.CreateDirectories(Subfolders, downloadPath);
        await _apiClient.DownloadFiles(Files, downloadPath, ct);
    }

    public async Task DownloadContentsRecursively(string downloadPath, CancellationToken ct = default)
    {
        ApiClient.CreateDirectories(SubfoldersRecursive, downloadPath);
        await _apiClient.DownloadFiles(FilesRecursive, downloadPath, ct);
    }
}