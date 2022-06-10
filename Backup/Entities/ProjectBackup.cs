using ACC.ApiClient.Entities;

namespace ACC.Backup.Entities;

public class ProjectBackup : Project
{
    public ProjectBackup(ApiClient.ApiClient apiClient) : base(apiClient)
    {
    }

    public DateTime? BackupStartedAt { get; set; }
    public DateTime? BackupFinishedAt { get; set; }
}