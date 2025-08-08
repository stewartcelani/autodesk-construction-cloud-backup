using Library.Logger;

ILogger logger1 = new SerilogLogger();
logger1.Trace("Testing trace");
logger1.Trace(new Exception(), "Testing trace exception overload");
Console.WriteLine();


var config = new SerilogLoggerConfiguration
{
    LogLevel = LogLevel.Info,
    LogToConsole = true
};
ILogger logger2 = new SerilogLogger(config);
logger2.Trace("Not visible");
logger2.Info("Is visible");