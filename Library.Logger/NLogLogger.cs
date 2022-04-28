using NLog;
using NLog.Config;
using NLog.Targets;

namespace Library.Logger;

public class NLogLogger : ILogger
{
    private readonly NLogLoggerConfiguration _config;

    private NLog.Logger Log { get; set; }

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
    
    public void Trace(string s)
    {
        Log.Trace(s);
    }

    public void Trace(Exception ex, string s)
    {
        Log.Trace(ex, s);
    }

    public void Debug(string s)
    {
        Log.Debug(s);
    }

    public void Debug(Exception ex, string s)
    {
        Log.Debug(ex, s);
    }

    public void Info(string s)
    {
        Log.Info(s);
    }

    public void Info(Exception ex, string s)
    {
        Log.Info(ex, s);
    }

    public void Warn(string s)
    {
        Log.Warn(s);
    }

    public void Warn(Exception ex, string s)
    {
        Log.Warn(ex, s);
    }

    public void Error(string s)
    {
        Log.Warn(s);
    }

    public void Error(Exception ex, string s)
    {
        Log.Error(ex, s);
    }

    public void Fatal(string s)
    {
        Log.Fatal(s);
    }

    public void Fatal(Exception ex, string s)
    {
        Log.Fatal(ex, s);
    }
}