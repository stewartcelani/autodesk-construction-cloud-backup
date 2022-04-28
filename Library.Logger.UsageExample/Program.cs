using Library.Logger;
using NLog;
using ILogger = Library.Logger.ILogger;

ILogger logger1 = new NLogLogger();
logger1.Trace("Testing trace");
logger1.Trace(new Exception(), "Testing trace exception overload");
Console.WriteLine();


var config = new NLogLoggerConfiguration()
{
    LogLevel = LogLevel.Info,
    LogToConsole = true
};
ILogger logger2 = new NLogLogger(config);
logger2.Trace("Not visible");
logger2.Info("Is visible");


