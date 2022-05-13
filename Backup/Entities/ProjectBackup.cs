﻿using ACC.ApiClient.Entities;

namespace ACC.Backup.Entities;

public class ProjectBackup : Project
{
    public DateTime? BackupStartedAt { get; set; }
    public DateTime? BackupFinishedAt { get; set; }

    public ProjectBackup(ApiClient.ApiClient apiClient) : base(apiClient)
    {
    }
}