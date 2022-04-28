namespace Library.FileDownloader;

public record FileDownloaderFile
{
    public string DownloadUrl { get; set; }
    public string DownloadPath { get; set; }
}