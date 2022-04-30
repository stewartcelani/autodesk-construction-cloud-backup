namespace AutodeskConstructionCloud.ApiClient.Entities;

public class File
{
    public string FileId { get; set; }
    public string FolderId { get; set; }
    public string ProjectId { get; set; }
    public string ProjectName { get; set; }
    public int ProjectIndex { get; set; }
    public string Name { get; set; }
    public string Path { get; set; }
    public string FileType { get; set; }
    public string SourceFileName { get; set; }
    public string Type { get; set; }
    public int VersionNumber { get; set; }
    public string DisplayName { get; set; }
    public DateTime CreateTime { get; set; }
    public string CreateUserId { get; set; }
    public int StorageSize { get; set; }
    public string CreateUserName { get; set; }
    public bool Hidden { get; set; } = false;
    public bool Reserved { get; set; } = false;
    public DateTime LastModifiedTime { get; set; }
    public string LastModifiedUserId { get; set; }
    public string LastModifiedUserName { get; set; }
    public string DownloadUrl { get; set; }
}