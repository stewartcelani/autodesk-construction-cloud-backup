# Autodesk Construction Cloud Backup
ACCBackup is a C# console application built to backup Autodesk Construction Cloud/BIM360 projects.

It can be run as a one-off or scheduled via a script. It was designed to fufill a need that Veeam Backup didn't support.
The end result, from an IT managmenet perspective, will be setting up 3 scheduled tasks that will enable 5x daily backups, 4x weekly backups, 12x monthly backups that will rotate accordingly.

It is consistently used to backup 70~ projects (100 GB~) in 3 hours:30 minutes.

### Prerequities
1. Create an app via https://forge.autodesk.com/
2. You will need the Client ID, Client Secret, App Name = "Backup", Descripton = "Backup", Callback URL: point to your business URL but it is not important (this is for 3 legged auth and we only use 2 legged), and "APIs this app will be able to access": Autodesk Construction Cloud API, BIM 360 API, Data Management API.
3. Enable Custom Integrations on your Autodesk admin account (if you don't see the tab email autodesk support as the article describes): https://knowledge.autodesk.com/support/bim-360/learn-explore/caas/CloudHelp/cloudhelp/ENU/BIM360D-Administration/files/About-Account-Admin/GUID-0C83B441-C611-4574-8DA0-45D5CFC235FA-html.html
4. From Accounts Admin (admin.b360.autodesk.com/) -> Settings -> Custom Integrations
5. Add Custom Integration -> you'll be asked for Client ID and App Name but that is about it.
6. Once this is done you will have a forge app with clientid & clientsecret that will be approved for your accountid.

### Examples.

Basic:
```
ACCBackup.exe --backupdirectory "C:\ACCBackup" --clientid "DRO4zxzt71HCkL34cn2tAUSRS0OQGaRT" --clientsecret "tFRHKhuIrGQUi5d3" --accountid "9b3cc923-d920-4fee-ae91-5e9c8e4040rb"
```
Basic with email summary:
```
-ACCBackup.exe -backupdirectory "C:\ACCBackup" --clientid "DRO4zxzt71HCkL34cn2tAUSRS0OQGaRT" --clientsecret "tFRHKhuIrGQUi5d3" --accountid "9b3cc923-d920-4fee-ae91-5e9c8e4040rb" --smtphost "smtp.yourlocalnetwork.dc" --smtpfromaddress "noreply@example.com" --smtpfromname "ACCBackup" --smtptoaddress "me@stewartcelani.com"
```

Example of a daily backup with 5 daily backup folders rotating:

```
ACCBackup.exe --backupdirectory "C:\ACCBackup\Daily" --clientid "DRO4zxzt71HCkL34cn2tAUSRS0OQGaRT" --clientsecret "tFRHKhuIrGQUi5d3" --accountid "9b3cc923-d920-4fee-ae91-5e9c8e4040rb" --backupstorotate 5
```
Example of a weekly backup with 4 weekly backup folders rotating:

```
ACCBackup.exe --backupdirectory "C:\ACCBackup\Weekly" --clientid "DRO4zxzt71HCkL34cn2tAUSRS0OQGaRT" --clientsecret "tFRHKhuIrGQUi5d3" --accountid "9b3cc923-d920-4fee-ae91-5e9c8e4040rb" --backupstorotate 4
```
Example of a monthly backup with 12 monthly backup folders rotating:

```
ACCBackup.exe --backupdirectory "C:\ACCBackup\Monthly" --clientid "DRO4zxzt71HCkL34cn2tAUSRS0OQGaRT" --clientsecret "tFRHKhuIrGQUi5d3" --accountid "9b3cc923-d920-4fee-ae91-5e9c8e4040rb" --backupstorotate 12
```

### ApiClient (Do It Yourself)
I designed ACCBackup around an ApiClient class library I wrote that does 99% of the heavy lifting.
This makes it very easy for you to take the class library and do your own thing with it if needed.
Below is a copy of ApitClient.UsageExample:Program.cs

````
using ACC.ApiClient;
using ACC.ApiClient.Entities;
using Library.Logger;
using Library.SecretsManager;
using File = ACC.ApiClient.Entities.File;
using LogLevel = Library.Logger.LogLevel;

/*
 * Required parameters
 */
string clientId = SecretsManager.GetEnvironmentVariableOrDefaultTo("acc:clientid", "InvalidClientId");
string clientSecret = SecretsManager.GetEnvironmentVariableOrDefaultTo("acc:clientsecret", "InvalidClientSecret");
string accountId = SecretsManager.GetEnvironmentVariableOrDefaultTo("acc:accountid", "InvalidAccountId");

/*
 * Building ApiClient with default parameters
 */
ApiClient defaultApiClient = TwoLeggedApiClient
    .Configure()
    .WithClientId(clientId)
    .AndClientSecret(clientSecret)
    .ForAccount(accountId)
    .Create();

/*
 * Building ApiClient with all options set
 */
ApiClient client = TwoLeggedApiClient
    .Configure()
    .WithClientId(clientId)
    .AndClientSecret(clientSecret)
    .ForAccount(accountId)
    .WithOptions(options =>
    {
        options.HubId = $"b.{accountId}";
        options.HttpClient = new HttpClient();
        options.DryRun = false;
        options.Logger = new NLogLogger(new NLogLoggerConfiguration()
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
List<Project> projects = await client.GetProjects();
foreach (Project p in projects)
{
  Console.WriteLine(p.Name);
}

/*
 * Get project by ID
 */
Project project = await client.GetProject("projectIdGoesHere");

/*
 * Load root folder contents
 */
await project.GetRootFolder();

foreach (File f in project.RootFolder.Files)
{
  Console.WriteLine(f.Name);
}

foreach (Folder f in project.RootFolder.Subfolders)
{
 Console.WriteLine(f.Name);
}

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
List<Project> allProjects = await client.GetProjects();
var downloadRoot = @"C:\ExampleBackupDirectory";
foreach (Project p in allProjects)
{
 await p.GetContentsRecursively();
 await p.DownloadContentsRecursively(Path.Combine(downloadRoot, p.Name));
}
````


