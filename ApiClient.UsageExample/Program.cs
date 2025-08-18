using ACC.ApiClient;
using Library.Logger;
using Library.SecretsManager;

/*
 * Required parameters
 */
var clientId = SecretsManager.GetEnvironmentVariableOrDefaultTo("acc:clientid", "InvalidClientId");
var clientSecret = SecretsManager.GetEnvironmentVariableOrDefaultTo("acc:clientsecret", "InvalidClientSecret");
var accountId = SecretsManager.GetEnvironmentVariableOrDefaultTo("acc:accountid", "InvalidAccountId");

/*
 * Building ApiClient with default parameters
 */
var defaultApiClient = TwoLeggedApiClient
    .Configure()
    .WithClientId(clientId)
    .AndClientSecret(clientSecret)
    .ForAccount(accountId)
    .Create();

/*
 * Building ApiClient with all options set
 */
var client = TwoLeggedApiClient
    .Configure()
    .WithClientId(clientId)
    .AndClientSecret(clientSecret)
    .ForAccount(accountId)
    .WithOptions(options =>
    {
        options.HubId = $"b.{accountId}";
        options.HttpClient = new HttpClient();
        options.DryRun = false;
        options.Logger = new SerilogLogger(new SerilogLoggerConfiguration
        {
            LogLevel = LogLevel.Trace,
            LogToConsole = true,
            LogToFile = true
        });
        options.RetryAttempts = 1;
        options.InitialRetryInSeconds = 2;
    })
    .Create();

/*
 * Getting all projects with above ApiClient
 */
var projects = await client.GetProjects();
foreach (var p in projects) Console.WriteLine(p.Name);

/*
 * Get project by ID
 */
var project = await client.GetProject("projectIdGoesHere");

/*
 * Load root folder contents
 */
await project.GetRootFolder();

foreach (var f in project.RootFolder.Files) Console.WriteLine(f.Name);

foreach (var f in project.RootFolder.Subfolders) Console.WriteLine(f.Name);

/*
 * GetContents or GetContentsRecursively can be called on any folder
 */
await project.RootFolder.Subfolders[0].GetContents();
await project.RootFolder.Subfolders[1].GetContentsRecursively();

/*
 * GetContentsRecursively can be called directly on a project to enumerate a projects entire directory structure.
 */
await project.GetContentsRecursively();

/*
 * Once the contents are loaded into memory they can be downloaded
 */
await project.DownloadContentsRecursively(@"C:\ExampleBackupDirectory");

/*
 * Folders can be downloaded by themselves...
 */
await project.RootFolder.Subfolders[0].DownloadContents(@"C:\ExampleBackupDirectory");

/*
 * ...or recursively. Remember to enumerate contents before recursively downloading.
 */
await project.RootFolder.Subfolders[0].GetContentsRecursively();
await project.RootFolder.Subfolders[0].DownloadContentsRecursively(@"C:\ExampleBackupDirectory");

/*
 * Example of how to get all projects then back them all up
 */
var allProjects = await client.GetProjects();
var downloadRoot = @"C:\ExampleBackupDirectory";
foreach (var p in allProjects)
{
    await p.GetContentsRecursively();
    await p.DownloadContentsRecursively(Path.Combine(downloadRoot, p.Name));
}