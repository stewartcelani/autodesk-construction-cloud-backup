using AutodeskConstructionCloud.ApiClient;
using Library.Logger;
using NLog;

var http = new HttpClient();
Console.WriteLine(http.BaseAddress);

var http2 = new HttpClient();
http2.BaseAddress = new Uri("https://www.test.com");
Console.WriteLine(http2.BaseAddress);

/*
ApiClient api1 = TwoLeggedApiClient
    .Configure()
    .WithClientId("AFO4tyzt71HCkL73cn2tAUSRS0OSGaRY")
    .AndClientSecret("wE3GFhuIsGJEi3d4")
    .ForAccount("f33e018a-d1f5-4ef3-ae67-606de6aeed87")
    .WithOptions(options => 
    {
        options.RetryAttempts = 4;
        options.InitialRetryInSeconds = 15;
        options.Logger = new NLogLogger(); // Logger is null (no logging) by default
    })
    .Create();

Console.WriteLine(api1.Config.ClientId); // AFO4tyzt71HCkL73cn2tAUSRS0OSGaRY
Console.WriteLine(api1.Config.RetryAttempts); // 4
Console.WriteLine(api1.Config.InitialRetryInSeconds); // 15
Console.WriteLine();


ApiClient api2 = TwoLeggedApiClient
    .Configure()
    .WithClientId("AFO4tyzt71HCkL73cn2tAUSRS0OSGaRY")
    .AndClientSecret("wE3GFhuIsGJEi3d4")
    .ForAccount("f33e018a-d1f5-4ef3-ae67-606de6aeed87")
    .Create(); // .WithOptions can be skipped and default values are provided

ApiClient api3 = TwoLeggedApiClient
    .Configure()
    .WithClientId("AFO4tyzt71HCkL73cn2tAUSRS0OSGaRY")
    .AndClientSecret("wE3GFhuIsGJEi3d4")
    .ForAccount("f33e018a-d1f5-4ef3-ae67-606de6aeed87")
    .WithOptions(options =>
    {
        options.InitialRetryInSeconds = 2;
        options.RetryAttempts = 2;
        options.Logger = new NLogLogger(new NLogLoggerConfiguration()
        {
            LogLevel = LogLevel.Trace,
            LogToConsole = true
        });
    })
    .Create();

Console.WriteLine(api3.Config.InitialRetryInSeconds);
Console.WriteLine(api3.Config.RetryAttempts);

await api3.GetAllProjects();
*/