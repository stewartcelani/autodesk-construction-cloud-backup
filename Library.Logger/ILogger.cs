namespace Library.Logger;

public interface ILogger
{
    public void Trace(string s);
    public void Trace(Exception ex, string s);
    public void Debug(string s);
    public void Debug(Exception ex, string s);
    public void Info(string s);
    public void Info(Exception ex, string s);
    public void Warn(string s);
    public void Warn(Exception ex, string s);
    public void Error(string s);
    public void Error(Exception ex, string s);
    public void Fatal(string s);
    public void Fatal(Exception ex, string s);
}