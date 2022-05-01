using System.Runtime.CompilerServices;

namespace AutodeskConstructionCloud.ApiClient.Entities;

public class Folder
{
    public string FolderId { get; set; }
    public string ProjectId { get; set; }
    public string ParentFolderId { get; set; }
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
    public List<Folder> Folders { get; set; } = new List<Folder>();
    public List<File> Files { get; set; } = new List<File>();
}