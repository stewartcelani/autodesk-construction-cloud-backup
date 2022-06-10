using NLog;
using NLog.Config;
using NLog.Targets;

namespace Library.Logger;

public class NLogLogger : ILogger
{
    public NLogLogger()
    {
        Config = new NLogLoggerConfiguration();
        ConfigureLogger();
    }

    public NLogLogger(ILoggerConfiguration config)
    {
        Config = config;
        ConfigureLogger();
    }

    private NLog.Logger Log { get; set; } = null!;

    public ILoggerConfiguration Config { get; }

    public virtual void Trace(string message)
    {
        Log.Trace(message);
    }

    public virtual void Trace(Exception ex, string message)
    {
        Log.Trace(ex, message);
    }

    public virtual void Debug(string message)
    {
        Log.Debug(message);
    }

    public virtual void Debug(Exception ex, string message)
    {
        Log.Debug(ex, message);
    }

    public virtual void Info(string message)
    {
        Log.Info(message);
    }

    public virtual void Info(Exception ex, string message)
    {
        Log.Info(ex, message);
    }

    public virtual void Warn(string message)
    {
        Log.Warn(message);
    }

    public virtual void Warn(Exception ex, string message)
    {
        Log.Warn(ex, message);
    }

    public virtual void Error(string message)
    {
        Log.Warn(message);
    }

    public virtual void Error(Exception ex, string message)
    {
        Log.Error(ex, message);
    }

    public virtual void Fatal(string message)
    {
        Log.Fatal(message);
    }

    public virtual void Fatal(Exception ex, string message)
    {
        Log.Fatal(ex, message);
    }

    private void ConfigureLogger()
    {
        NLog.LogLevel logLevel = MapILoggerConfigurationLogLevelToNLogLogLevel(Config.LogLevel);
        var config = new LoggingConfiguration();
        var layout =
            "${longdate}|${level}|${callsite:fileName=true:includeSourcePath=false:skipFrames=1}|${message}             ${all-event-properties} ${exception:format=tostring}";

        if (Config.LogToConsole)
        {
            var coloredConsoleTarget = new ColoredConsoleTarget("coloredconsole")
            {
                UseDefaultRowHighlightingRules = false,
                Layout = layout
            };
            coloredConsoleTarget.RowHighlightingRules.Add(new ConsoleRowHighlightingRule
            {
                Condition = "level == LogLevel.Fatal",
                ForegroundColor = ConsoleOutputColor.Red,
                BackgroundColor = ConsoleOutputColor.White
            });
            coloredConsoleTarget.RowHighlightingRules.Add(new ConsoleRowHighlightingRule
            {
                Condition = "level == LogLevel.Error",
                ForegroundColor = ConsoleOutputColor.Red,
                BackgroundColor = ConsoleOutputColor.NoChange
            });
            coloredConsoleTarget.RowHighlightingRules.Add(new ConsoleRowHighlightingRule
            {
                Condition = "level == LogLevel.Warn",
                ForegroundColor = ConsoleOutputColor.Yellow,
                BackgroundColor = ConsoleOutputColor.NoChange
            });
            coloredConsoleTarget.RowHighlightingRules.Add(new ConsoleRowHighlightingRule
            {
                Condition = "level == LogLevel.Info",
                ForegroundColor = ConsoleOutputColor.White,
                BackgroundColor = ConsoleOutputColor.NoChange
            });
            coloredConsoleTarget.RowHighlightingRules.Add(new ConsoleRowHighlightingRule
            {
                Condition = "level == LogLevel.Debug",
                ForegroundColor = ConsoleOutputColor.DarkGray,
                BackgroundColor = ConsoleOutputColor.NoChange
            });
            coloredConsoleTarget.RowHighlightingRules.Add(new ConsoleRowHighlightingRule
            {
                Condition = "level == LogLevel.Trace",
                ForegroundColor = ConsoleOutputColor.DarkGray,
                BackgroundColor = ConsoleOutputColor.NoChange
            });
            config.AddRule(logLevel, NLog.LogLevel.Fatal, coloredConsoleTarget);
        }

        if (Config.LogToFile)
        {
            var infoLogFileTarget = new FileTarget("info")
            {
                FileName = @"${basedir}\Logs\${date:format=yyyy-MM-dd}.log",
                Layout = layout
            };
            config.AddRule(NLog.LogLevel.Info, NLog.LogLevel.Fatal, infoLogFileTarget);

            if (logLevel == NLog.LogLevel.Trace)
            {
                var traceLogFileTarget = new FileTarget("trace")
                {
                    FileName = @"${basedir}\\Logs\${date:format=yyyy-MM-dd}.trace.log",
                    Layout = layout
                };
                config.AddRule(NLog.LogLevel.Trace, NLog.LogLevel.Fatal, traceLogFileTarget);
            }
            else if (logLevel == NLog.LogLevel.Debug)
            {
                var debugLogFileTarget = new FileTarget("debug")
                {
                    FileName = @"${basedir}\\Logs\${date:format=yyyy-MM-dd}.debug.log",
                    Layout = layout
                };
                config.AddRule(NLog.LogLevel.Debug, NLog.LogLevel.Fatal, debugLogFileTarget);
            }
        }

        LogManager.Configuration = config;
        Log = LogManager.GetCurrentClassLogger();
    }

    private static NLog.LogLevel MapILoggerConfigurationLogLevelToNLogLogLevel(LogLevel logLevel)
    {
        return logLevel switch
        {
            LogLevel.Off => NLog.LogLevel.Off,
            LogLevel.Silly => throw new ArgumentOutOfRangeException(),
            LogLevel.Trace => NLog.LogLevel.Trace,
            LogLevel.Debug => NLog.LogLevel.Debug,
            LogLevel.Info => NLog.LogLevel.Info,
            LogLevel.Warn => NLog.LogLevel.Warn,
            LogLevel.Error => NLog.LogLevel.Error,
            LogLevel.Fatal => NLog.LogLevel.Fatal,
            _ => throw new ArgumentOutOfRangeException()
        };
    }
}