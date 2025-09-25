using System.IO;
using Serilog;
using Serilog.Events;
using Serilog.Exceptions;
using Serilog.Sinks.SystemConsole.Themes;

namespace Library.Logger;

public class SerilogLogger : ILogger
{
    private readonly Serilog.ILogger _log;

    public SerilogLogger()
    {
        Config = new SerilogLoggerConfiguration();
        _log = ConfigureLogger();
    }

    public SerilogLogger(ILoggerConfiguration config)
    {
        Config = config;
        _log = ConfigureLogger();
    }

    public SerilogLogger(Serilog.ILogger logger, ILoggerConfiguration config)
    {
        Config = config;
        _log = logger;
    }

    public ILoggerConfiguration Config { get; }

    public virtual void Trace(string message)
    {
        _log.Verbose(message);
    }

    public virtual void Trace(Exception ex, string message)
    {
        _log.Verbose(ex, message);
    }

    public virtual void Debug(string message)
    {
        _log.Debug(message);
    }

    public virtual void Debug(Exception ex, string message)
    {
        _log.Debug(ex, message);
    }

    public virtual void Info(string message)
    {
        _log.Information(message);
    }

    public virtual void Info(Exception ex, string message)
    {
        _log.Information(ex, message);
    }

    public virtual void Warn(string message)
    {
        _log.Warning(message);
    }

    public virtual void Warn(Exception ex, string message)
    {
        _log.Warning(ex, message);
    }

    public virtual void Error(string message)
    {
        _log.Error(message);
    }

    public virtual void Error(Exception ex, string message)
    {
        _log.Error(ex, message);
    }

    public virtual void Fatal(string message)
    {
        _log.Fatal(message);
    }

    public virtual void Fatal(Exception ex, string message)
    {
        _log.Fatal(ex, message);
    }

    private Serilog.ILogger ConfigureLogger()
    {
        var logLevel = MapILoggerConfigurationLogLevelToSerilogLogLevel(Config.LogLevel);
        var logDirectory = Path.Combine(AppContext.BaseDirectory, "Logs");
        Directory.CreateDirectory(logDirectory);
        var loggerConfig = new LoggerConfiguration()
            .MinimumLevel.Is(logLevel)
            .Enrich.FromLogContext()
            .Enrich.WithExceptionDetails();

        var outputTemplate = "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}";

        if (Config.LogToConsole)
            loggerConfig.WriteTo.Console(
                theme: AnsiConsoleTheme.Code,
                outputTemplate: outputTemplate,
                restrictedToMinimumLevel: logLevel);

        if (Config.LogToFile)
        {
            loggerConfig.WriteTo.File(
                Path.Combine(logDirectory, "log.log"),
                rollingInterval: RollingInterval.Day,
                outputTemplate:
                "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff}|{Level:u3}|{SourceContext}] {Message:lj}{NewLine}{Exception}",
                restrictedToMinimumLevel: LogEventLevel.Information);

            if (logLevel == LogEventLevel.Verbose)
                loggerConfig.WriteTo.File(
                    Path.Combine(logDirectory, "log.trace.log"),
                    rollingInterval: RollingInterval.Day,
                    outputTemplate:
                    "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff}|{Level:u3}|{SourceContext}] {Message:lj}{NewLine}{Exception}",
                    restrictedToMinimumLevel: LogEventLevel.Verbose);
            else if (logLevel == LogEventLevel.Debug)
                loggerConfig.WriteTo.File(
                    Path.Combine(logDirectory, "log.debug.log"),
                    rollingInterval: RollingInterval.Day,
                    outputTemplate:
                    "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff}|{Level:u3}|{SourceContext}] {Message:lj}{NewLine}{Exception}",
                    restrictedToMinimumLevel: LogEventLevel.Debug);
        }

        return loggerConfig.CreateLogger();
    }

    private static LogEventLevel MapILoggerConfigurationLogLevelToSerilogLogLevel(LogLevel logLevel)
    {
        return logLevel switch
        {
            LogLevel.Off => LogEventLevel.Fatal + 1,
            LogLevel.Silly => throw new ArgumentOutOfRangeException(),
            LogLevel.Trace => LogEventLevel.Verbose,
            LogLevel.Debug => LogEventLevel.Debug,
            LogLevel.Info => LogEventLevel.Information,
            LogLevel.Warn => LogEventLevel.Warning,
            LogLevel.Error => LogEventLevel.Error,
            LogLevel.Fatal => LogEventLevel.Fatal,
            _ => throw new ArgumentOutOfRangeException()
        };
    }
}
