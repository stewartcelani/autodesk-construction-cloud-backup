namespace Library.Logger;

public class NLogLoggerConfiguration : ILoggerConfiguration
{
    public LogLevel LogLevel { get; set; } = LogLevel.Info;
    public bool LogToConsole { get; set; } = true;
}