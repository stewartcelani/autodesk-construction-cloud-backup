using ACC.Backup;
using Library.Logger;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Debugging;
using Serilog.Events;
using Serilog.Exceptions;
using Serilog.Sinks.SystemConsole.Themes;
using ILogger = Library.Logger.ILogger;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        // Parse command line arguments first to determine log level
        var backupConfiguration = new BackupConfiguration(args);

        // Determine log level based on command line arguments
        var logLevel = backupConfiguration.TraceLogging ? LogEventLevel.Verbose :
            backupConfiguration.DebugLogging ? LogEventLevel.Debug :
            LogEventLevel.Information;

        // Serilog logging setup - code-first configuration
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Is(logLevel)
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
            .MinimumLevel.Override("System.Net.Http.HttpClient", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .Enrich.WithExceptionDetails()
            .WriteTo.Console(
                theme: AnsiConsoleTheme.Code,
                outputTemplate: "[{Timestamp:HH:mm:ss} {SourceContext} {Level:u3}] {Message:lj}{NewLine}{Exception}",
                restrictedToMinimumLevel: logLevel)
            .WriteTo.File(
                "Logs/log-.txt",
                rollingInterval: RollingInterval.Day,
                outputTemplate:
                "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff} {Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}",
                restrictedToMinimumLevel: LogEventLevel.Information)
            .CreateLogger();

        SelfLog.Enable(Console.Error);

        // Register BackupConfiguration (already parsed above)
        services.AddSingleton(backupConfiguration);

        // Register Serilog logger as Library.Logger.ILogger
        services.AddSingleton<ILogger>(provider =>
        {
            var config = provider.GetRequiredService<BackupConfiguration>();
            return new SerilogLogger(Log.Logger, new SerilogLoggerConfiguration
            {
                LogLevel = config.TraceLogging ? LogLevel.Trace :
                    config.DebugLogging ? LogLevel.Debug :
                    LogLevel.Info,
                LogToConsole = true,
                LogToFile = true
            });
        });

        // Register Backup service
        services.AddSingleton<IBackup, Backup>();
    })
    .UseSerilog()
    .Build();

// Get the backup service and run it
var backup = host.Services.GetRequiredService<IBackup>();
await backup.Run();

Log.CloseAndFlush();