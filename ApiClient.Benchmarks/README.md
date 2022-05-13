BenchmarkDotNet=v0.13.1, OS=Windows 10.0.19044.1645 (21H2)
Intel Core i7-5820K CPU 3.30GHz (Broadwell), 1 CPU, 12 logical and 6 physical cores
.NET SDK=6.0.202
[Host]     : .NET 6.0.4 (6.0.422.16404), X64 RyuJIT
Job-KSORIT : .NET 6.0.4 (6.0.422.16404), X64 RyuJIT

IterationCount=3  LaunchCount=1  WarmupCount=1

|          Method |    Mean |   Error | StdDev |     Gen 0 |     Gen 1 |     Gen 2 | Allocated |
|---------------- |--------:|--------:|-------:|----------:|----------:|----------:|----------:|
|    ParallelLoop | 123.2 s | 61.06 s | 3.35 s | 3000.0000 |         - |         - |     32 MB |
| SynchronousLoop | 299.8 s | 74.74 s | 4.10 s | 4000.0000 | 1000.0000 | 1000.0000 |     32 MB |

The project that was used in the benchmark contained 300MB total data (136 files, 30 folders).

The ApiClient.DownloadFiles method using Parallel.ForEachAsync is **2x faster** than a synchronous loop that calls ApiClient.DownloadFile.