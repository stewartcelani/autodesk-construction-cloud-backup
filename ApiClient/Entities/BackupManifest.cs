using Newtonsoft.Json;

namespace ACC.ApiClient.Entities;

public class BackupManifest
{
    public BackupManifest()
    {
        // Use case-insensitive dictionary for cross-platform compatibility
        Files = new Dictionary<string, FileManifestEntry>(StringComparer.OrdinalIgnoreCase);
        BackupDate = DateTime.Now;
        BackupDirectory = string.Empty;
    }

    [JsonProperty("backupDate")] public DateTime BackupDate { get; set; }

    [JsonProperty("backupDirectory")] public string BackupDirectory { get; set; }

    [JsonProperty("files")] public Dictionary<string, FileManifestEntry> Files { get; set; }

    public void AddFile(string relativePath, FileManifestEntry entry)
    {
        Files[relativePath] = entry;
    }

    public bool TryGetFile(string relativePath, out FileManifestEntry entry)
    {
        return Files.TryGetValue(relativePath, out entry);
    }

    public void SaveToFile(string filePath)
    {
        var json = JsonConvert.SerializeObject(this, Formatting.Indented);
        System.IO.File.WriteAllText(filePath, json);
    }

    public static BackupManifest LoadFromFile(string filePath)
    {
        if (!System.IO.File.Exists(filePath))
            return null;

        var json = System.IO.File.ReadAllText(filePath);
        var manifest = JsonConvert.DeserializeObject<BackupManifest>(json);

        // Ensure the dictionary is case-insensitive after deserialization
        if (manifest != null && manifest.Files != null)
        {
            var caseInsensitiveFiles = new Dictionary<string, FileManifestEntry>(
                manifest.Files, StringComparer.OrdinalIgnoreCase);
            manifest.Files = caseInsensitiveFiles;
        }

        return manifest;
    }
}

public class FileManifestEntry
{
    [JsonProperty("fileId")] public string FileId { get; set; }

    [JsonProperty("versionNumber")] public int VersionNumber { get; set; }

    [JsonProperty("lastModifiedTime")] public DateTime LastModifiedTime { get; set; }

    [JsonProperty("storageSize")] public long StorageSize { get; set; }

    [JsonProperty("name")] public string Name { get; set; }

    [JsonProperty("projectId")] public string ProjectId { get; set; }

    public bool IsEquivalentTo(FileManifestEntry other)
    {
        if (other == null) return false;

        return FileId == other.FileId &&
               VersionNumber == other.VersionNumber &&
               LastModifiedTime == other.LastModifiedTime &&
               StorageSize == other.StorageSize;
    }
}