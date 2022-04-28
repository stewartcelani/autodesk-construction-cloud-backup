using Library.FileDownloader;

/*
 * If no configuration is passed to FileDownloader the default FileDownloaderConfiguration
 * will be assigned, with retryAttempts 6, secondsBetweenAttempts 5 and no logging output
 */
var config = new FileDownloaderConfiguration(
    6, 
    5, 
    s => Console.WriteLine(s)
);
var downloader = new FileDownloader(config);

const string downloadUrl = @"https://stewartcelani-public.s3.amazonaws.com/samplefiles/1.tmp";
string downloadPath = Path.Combine(Path.GetTempPath(), "ex1.tmp");

/*
 * Example 1: DownloadAsync basic usage
 */
FileInfo downloadAsyncExample1 = await downloader.DownloadAsync(downloadUrl, downloadPath);


/*
 * Example 2: DownloadAsync overload
 */
var example2File = new FileDownloaderFile()
{
    DownloadUrl = downloadUrl,
    DownloadPath = Path.Combine(Path.GetTempPath(), "ex2.tmp")
};
FileInfo downloadAsyncExample2 = await downloader.DownloadAsync(example2File);


/*
 * Example 4: DownloadParallelAsync
 */
List<FileDownloaderFile> fileList = new();
for (var i = 1; i <= 100; i++)
{
    fileList.Add(new FileDownloaderFile()
    {
        DownloadUrl = @$"https://stewartcelani-public.s3.amazonaws.com/samplefiles/{i}.tmp",
        DownloadPath = Path.Combine(Path.GetTempPath(), $"{i}.tmp")
    });
}
List<FileInfo> downloadParallelAsyncExample = await downloader.DownloadParallelAsync(fileList);