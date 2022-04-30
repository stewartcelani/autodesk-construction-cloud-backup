using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using FluentAssertions;
using Library.Logger;

namespace Library.FileDownloader.Tests;

public class FileDownloaderIntegrationTests
{
    private readonly FileDownloader _sut;
    private readonly FileDownloaderFile _file = new()
    {
        DownloadUrl = @"https://stewartcelani-public.s3.amazonaws.com/samplefiles/1.tmp",
        DownloadPath = Path.Combine(Path.GetTempPath(), "1.tmp")
    };
    private readonly List<FileDownloaderFile> _fileList = new();

    public FileDownloaderIntegrationTests()
    {
        _sut = new FileDownloader();
        
        for (var i = 1; i <= 20; i++)
        {
            _fileList.Add(new FileDownloaderFile()
            {
                /*
                 * 1.tmp to 100.tmp are 0 bytes.
                 * 101.tmp to 1000.tmp are random sizes between 0 and 5mb.
                 */
                DownloadUrl = @$"https://stewartcelani-public.s3.amazonaws.com/samplefiles/{i}.tmp",
                DownloadPath = Path.Combine(Path.GetTempPath(), $"{i}.tmp")
            });
        }
    }

    [Fact]
    public async Task DownloadAsync_Should_DownloadFileSuccessfully()
    {
        FileInfo file = await _sut.DownloadAsync(_file.DownloadUrl, _file.DownloadPath);
        file.Exists.Should().BeTrue();
    }

    [Fact]
    public async Task DownloadAsync_WithCustomFileDownloaderConfiguration_Should_DownloadFileSuccessfully()
    {
        var config = new FileDownloaderConfiguration(
            6, 
            5, 
            new NLogLogger()
        );
        var sut = new FileDownloader(config);
        FileInfo file = await sut.DownloadAsync(_file.DownloadUrl, _file.DownloadPath);
        file.Exists.Should().BeTrue();
    }

    [Fact]
    public async Task DownloadAsync_Overload_Should_DownloadFileSuccessfully()
    {
        FileInfo file = await _sut.DownloadAsync(_file);
        file.Exists.Should().BeTrue();
    }

    [Fact]
    public async Task DownloadParallelAsync_Should_DownloadTwentyFilesSuccessfully()
    {
        List<FileInfo> files = await _sut.DownloadParallelAsync(_fileList);
        files.Count.Should().Be(20);
        files.All(x => x.Exists).Should().BeTrue();
    }
}