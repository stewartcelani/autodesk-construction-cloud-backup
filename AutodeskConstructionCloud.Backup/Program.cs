using ACC.Backup;

var backupConfiguration = new BackupConfiguration(args);
var backup = new Backup(backupConfiguration);
await backup.Run();