using AutodeskConstructionCloud.ApiClient;
using Library.Logger;
using NLog;

ApiClient api1 = TwoLeggedApiClient
    .Configure()
    .WithClientId("AFO4tyzt71HCkL73cn2tAUSRS0OSGaRY")
    .AndClientSecret("wE3GFhuIsGJEi3d4")
    .ForAccount("f33e018a-d1f5-4ef3-ae67-606de6aeed87")
    .WithOptions(options => // .WithOptions can be skipped and default values are provided
    {
        options.RetryAttempts = 4;
        options.InitialRetryInSeconds = 15;
    })
    .CreateApiClient();

Console.WriteLine(api1.Config.ClientId); // AFO4tyzt71HCkL73cn2tAUSRS0OSGaRY
Console.WriteLine(api1.Config.RetryAttempts); // 4
Console.WriteLine(api1.Config.InitialRetryInSeconds); // 15
api1.Example(); // "Example of INFO logging via NLog."
Console.WriteLine();


ApiClient api2 = TwoLeggedApiClient
    .Configure()
    .WithClientId("AFO4tyzt71HCkL73cn2tAUSRS0OSGaRY")
    .AndClientSecret("wE3GFhuIsGJEi3d4")
    .ForAccount("f33e018a-d1f5-4ef3-ae67-606de6aeed87")
    .WithOptions(options =>
    {
        options.Logger = null; // This will turn off logging
    })
    .CreateApiClient();
api2.Example(); // [There will be no output]
Console.WriteLine();


ApiClient api3 = TwoLeggedApiClient
    .Configure()
    .WithClientId("AFO4tyzt71HCkL73cn2tAUSRS0OSGaRY")
    .AndClientSecret("wE3GFhuIsGJEi3d4")
    .ForAccount("f33e018a-d1f5-4ef3-ae67-606de6aeed87")
    .WithOptions(options =>
    {
        options.Logger = new NLogLogger(new NLogLoggerConfiguration()
        {
            LogLevel = LogLevel.Trace,
            LogToConsole = true
        });
    })
    .CreateApiClient();
api3.Example(); // "Example of TRACE logging via NLog." + "Example of INFO logging via NLog."
Console.WriteLine();
