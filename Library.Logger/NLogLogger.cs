using NLog;
using NLog.Config;
using NLog.Targets;

namespace Library.Logger;

public class NLogLogger : ILogger
{
    private readonly NLogLoggerConfiguration _config;

    private NLog.Logger Log { get; set; } = null!;

    public NLogLogger()
    {
        _config = new NLogLoggerConfiguration();
        ConfigureLogger();
    }
    public NLogLogger(NLogLoggerConfiguration config)
    {
        _config = config;
        ConfigureLogger();
    }

    private void ConfigureLogger()
    {
        var config = new LoggingConfiguration();

        if (_config.LogToConsole)
        {
            var coloredConsoleTarget = new ColoredConsoleTarget("coloredconsole")
            {
                
                UseDefaultRowHighlightingRules = false,                
                Layout = "${longdate}|${level}|${callsite:fileName=true:includeSourcePath=false:skipFrames=1}|Line:${callsite-linenumber:skipFrames=1}|${message}             ${all-event-properties} ${exception:format=tostring}"
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
            config.AddRule(minLevel: _config.LogLevel, maxLevel: LogLevel.Fatal, target: coloredConsoleTarget);
        }
        
        LogManager.Configuration = config;
        Log = LogManager.GetCurrentClassLogger();
    }
    
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
}