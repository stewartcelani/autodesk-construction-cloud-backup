namespace Library.Logger;

public interface ILoggerConfiguration
{
    public LogLevel LogLevel { get; set; }
    public bool LogToConsole { get; set; }
    public bool LogToFile { get; set; }
}