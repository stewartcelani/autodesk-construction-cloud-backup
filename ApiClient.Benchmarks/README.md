# Benchmark 1: 300 MB (136 files in 30 folders)
BenchmarkDotNet=v0.13.1, OS=Windows 10.0.19044.1645 (21H2)
Intel Core i7-5820K CPU 3.30GHz (Broadwell), 1 CPU, 12 logical and 6 physical cores
.NET SDK=6.0.202
[Host]     : .NET 6.0.4 (6.0.422.16404), X64 RyuJIT
Job-KSORIT : .NET 6.0.4 (6.0.422.16404), X64 RyuJIT

IterationCount=3 LaunchCount=1 WarmupCount=1

|          Method |    Mean |   Error | StdDev |
|---------------- |--------:|--------:|-------:|
|    ParallelLoop | 123.2 s | 61.06 s | 3.35 s |
| SynchronousLoop | 299.8 s | 74.74 s | 4.10 s |

# Benchmark 2: 26 GB (10,653 files in 973 folders)

BenchmarkDotNet=v0.13.1, OS=Windows 10.0.19044.1706 (21H2)
Intel Core i7-5820K CPU 3.30GHz (Broadwell), 1 CPU, 12 logical and 6 physical cores
.NET SDK=6.0.202
[Host]     : .NET 6.0.4 (6.0.422.16404), X64 RyuJIT
Job-QIVIZX : .NET 6.0.4 (6.0.422.16404), X64 RyuJIT

IterationCount=3  LaunchCount=1  RunStrategy=Monitoring  
WarmupCount=0

|          Method |     Mean |    Error |   StdDev |
|---------------- |---------:|---------:|---------:|
|    ParallelLoop |  98.83 m |  12.97 m |  0.711 m |
| SynchronousLoop | 300.62 m | 368.38 m | 20.192 m |
