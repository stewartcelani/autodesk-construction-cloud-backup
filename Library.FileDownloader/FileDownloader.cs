namespace Library.FileDownloader;

public class FileDownloader
{
    private readonly FileDownloaderConfiguration _config;
    private readonly HttpClient _http = new();

    public FileDownloader()
    {
        _config = new FileDownloaderConfiguration();
    }

    public FileDownloader(FileDownloaderConfiguration fileDownloaderConfiguration)
    {
        _config = fileDownloaderConfiguration;
    }

    public async Task<FileInfo> DownloadAsync(
        string downloadUrl, string downloadPath, CancellationToken ct = default)
    {
        return await _config.RetryPolicy.Execute(async () =>
        {
            await using Stream stream = await _http.GetStreamAsync(downloadUrl, ct);
            await using FileStream fileStream = new (downloadPath, FileMode.Create);
            await stream.CopyToAsync(fileStream, ct);
            var fileInfo = new FileInfo(downloadPath);
            if (_config.LoggingMethod is not null)
            {
                var fileSizeInMb = (decimal)Math.Round((((fileInfo.Length) / 1024f) / 1024f),2);
                _config.LoggingMethod($"{fileInfo.FullName} ({fileSizeInMb} MB)");
            }
            return fileInfo;
        });
    }

    public async Task<FileInfo> DownloadAsync(FileDownloaderFile file, CancellationToken ct = default)
    {
        return await DownloadAsync(file.DownloadUrl, file.DownloadPath, ct);
    }

    public async Task<List<FileInfo>> DownloadParallelAsync(
        IEnumerable<FileDownloaderFile> fileList, int maxDegreeOfParallelism = 10)
    {
        List<FileInfo> fileInfoList = new();
        
        var parallelOptions = new ParallelOptions()
        {
            MaxDegreeOfParallelism = maxDegreeOfParallelism,
        };
        
        await Parallel.ForEachAsync(fileList, parallelOptions, async (file, ct) =>
        {
            FileInfo fileInfo = await DownloadAsync(file, ct);
            fileInfoList.Add(fileInfo);
        });

        return fileInfoList;
    }
    
}