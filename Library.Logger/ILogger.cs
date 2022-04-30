namespace Library.Logger;

public interface ILogger
{
    public ILoggerConfiguration Config { get; }
    public void Trace(string message);
    public void Trace(Exception ex, string message);
    public void Debug(string message);
    public void Debug(Exception ex, string message);
    public void Info(string message);
    public void Info(Exception ex, string message);
    public void Warn(string message);
    public void Warn(Exception ex, string message);
    public void Error(string message);
    public void Error(Exception ex, string message);
    public void Fatal(string message);
    public void Fatal(Exception ex, string message);
}