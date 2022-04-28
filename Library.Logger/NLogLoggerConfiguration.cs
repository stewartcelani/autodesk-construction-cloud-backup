using NLog;

namespace Library.Logger;

public class NLogLoggerConfiguration
{
    public LogLevel LogLevel { get; set; } = LogLevel.Info;
    public bool LogToConsole { get; set; } = true;
}