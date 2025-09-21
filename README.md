# Autodesk Construction Cloud Backup

![GitHub release (latest by date)](https://img.shields.io/github/v/release/stewartcelani/autodesk-construction-cloud-backup)
![GitHub license](https://img.shields.io/github/license/stewartcelani/autodesk-construction-cloud-backup)
![.NET](https://img.shields.io/badge/.NET-9.0-blueviolet)
![GitHub last commit](https://img.shields.io/github/last-commit/stewartcelani/autodesk-construction-cloud-backup)

ACCBackup is a C# console application built to backup all Autodesk Construction Cloud/BIM360 projects in your account
via [Autodesk Platform Services (formerly Autodesk Forge)](https://aps.autodesk.com/).

It can be run as a one-off or scheduled via a script. It was designed to fulfill a need that Veeam Backup didn't
support.
The only products on the market at the time were $6k AUD per year, took 15-20 hours to backup what ACCBackup does in 3
hours (or ~30 minutes with v1.1.0's incremental backup for mostly unchanged data), and required each project to be
manually configured as a separate task/job.

By default, ACCBackup will backup all projects in your account.

**Update 19/08/2025:**
Since [initial release](https://github.com/stewartcelani/autodesk-construction-cloud-backup/releases) in May 2022 I've
been using ACCBackup to run nightly backups of (now) 170~ projects @ 225 GB~ in 4 hours without issues.

### v1.2.0: In-Place Sync Mode

This release introduces **In-Place Sync Mode**, a new backup approach that maintains a single, continuously synchronized backup directory instead of creating timestamped versions. This mode is ideal for users who want to maintain one up-to-date backup with minimal storage usage.

#### Key Features:

**1. In-Place Sync Mode**
- Enabled by default when `--backupstorotate 1` (the new default)
- Maintains a single backup directory that stays synchronized with ACC
- Only downloads new or modified files during each sync
- Automatically removes obsolete files no longer in ACC
- Cleans up empty directories after file removal
- Dramatically reduces storage requirements for regular backups

**2. Improved Concurrency and Thread Safety**
- Thread-safe directory creation with double-check locking pattern
- Optimized concurrent file handling using ConcurrentBag collections
- Refined semaphore limits for better performance and stability
- Prevents race conditions during parallel project enumeration

**3. Enhanced Backup Reporting**
- Detailed statistics showing unchanged vs downloaded files
- Clear distinction between sync mode and versioned backup modes
- Improved progress tracking for better visibility

#### Backup Modes:

**In-Place Sync Mode** (`--backupstorotate 1`):
- Single directory that stays in sync with ACC
- Minimal storage usage
- Automatic cleanup of obsolete files
- Best for regular automated backups

**Versioned Backup Mode** (`--backupstorotate 2+`):
- Creates timestamped backup folders (e.g., 2024-01-15_10-30)
- Maintains multiple backup versions
- No cleanup of previous versions
- Best for archival and point-in-time recovery

### v1.1.0: Gotta Go Fast Edition

**Important: ACCBackup now requires .NET 9 Runtime** (upgraded from .NET 8). Please ensure you have [.NET Runtime 9.X.X](https://dotnet.microsoft.com/en-us/download/dotnet/9.0) installed before upgrading.

This release is all about **speed**. I have implemented two major performance optimizations that work together to dramatically reduce backup times:

#### 1. Incremental Backup
Instead of re-downloading every file, ACCBackup now intelligently:
- Detects unchanged files by comparing Autodesk API metadata (FileId, VersionNumber, LastModifiedTime, StorageSize)
- Copies unchanged files from the previous backup instead of downloading
- Only downloads new or modified files from Autodesk
- **Important:** Each backup folder still contains the FULL project structure with ALL files - "incremental" refers to the optimization method, not the backup contents

#### 2. Producer-Consumer Pipeline with Concurrent Enumeration
ACCBackup now uses advanced parallelization techniques:
- **Concurrent enumeration**: Up to 3 projects enumerated simultaneously (folder structure queries)
- **Overlapped operations**: While downloading one project, multiple others are being enumerated
- **Smart queueing**: Uses C# Channels to queue enumerated projects for sequential downloading
- **Rate limit safe**: Downloads remain sequential to avoid API throttling
- Automatically enabled - no configuration required

**Combined Performance Improvements:**
- **Incremental backup:** Reduces download time by up to 75% for unchanged files
- **Concurrent enumeration:** Up to 3x faster project scanning with parallel processing
- **Pipeline optimization:** Minimizes idle time between projects (the bottleneck is now the Autodesk API rate limits)
- **All together:** A 250GB backup that previously took 12 hours can now complete in 3-4 hours

**How incremental backup works:**
- Enabled by default for all backups
- Creates a `BackupManifest.json` in each backup for tracking file metadata
- Verifies file integrity after copying from previous backup
- Falls back to downloading if local copy verification fails

To force a full download (bypass incremental backup), use the `--force-full-download` flag.

### Prerequities

1. Create an app via https://aps.autodesk.com/myapps/. At the time of writing all the APIs the app we are creating will
   use are free.
   ![01.png](docs%2F01.png)
   ![02.png](docs%2F02.png)
   ![03.png](docs%2F03.png)
2. Make sure to only select the following two APIs: `Autodesk Construction Cloud API`, `Data Management API`
   ![04.png](docs%2F04.png)
3. Save the `Client ID` and `Client Secret` as you will need them for the backup.
4. Enable Custom Integrations on your Autodesk admin account (if you don't see the tab email autodesk support
   as [this article](https://knowledge.autodesk.com/support/bim-360/learn-explore/caas/CloudHelp/cloudhelp/ENU/BIM360D-Administration/files/About-Account-Admin/GUID-0C83B441-C611-4574-8DA0-45D5CFC235FA-html.html)
   describes -- just say you want to build a custom integration).
5. From Accounts Admin -> Settings -> Custom Integrations -> Add Custom Integration -> provide Client ID and App Name.
   Save the `BIM 360 Account ID` as you will need that for the backup.
   ![05.png](docs%2F05.png)
6. Once this is done you will have a forge app with `Client ID` & `Client Secret` that will be approved for your
   `BIM 360 Account ID`.
   ![06.png](docs%2F06.png)

### Install

- Install latest [.NET Runtime 9.X.X](https://dotnet.microsoft.com/en-us/download/dotnet/9.0) -- only the console
  version is required ("The .NET Runtime contains just the components needed to run a console app. Typically, you'd also
  install either the ASP.NET Core Runtime or .NET Desktop Runtime.")
- Download the .zip via [the releases tab](https://github.com/stewartcelani/autodesk-construction-cloud-backup/releases)
- Unzip and run (see examples below)

### Examples

ACCBackup.exe --help

````
--backupdirectory           Required. Backup directory.

--clientid                  Required. Client ID of Autodesk Forge app.

--clientsecret              Required. Client secret of Autodesk Forge app.

--accountid                 Required. Autodesk Construction Cloud account ID.

--hubid                     Autodesk Construction Cloud HubId, defaults to b.AccountId. See
https://forge.autodesk.com/en/docs/data/v2/reference/http/hubs-hub_id-projects-project_id-
GET/ for more information.

--maxdegreeofparallelism    (Default: 8) Number of files to download in parallel.

--retryattempts             (Default: 15) Amount of times to retry when there are errors communicating with the
Autodesk API.

--initialretryinseconds     (Default: 2) Each subsequent retry is RetryAttempt# * InitialRetryInSeconds. Default
settings of 15 RetryAttempts with InitialRetryInSeconds 2 totals 4 minutes of retrying.

--dryrun                    (Default: false) Backup will only create 0 byte placeholder files instead of downloading
them, will still create full file structure.

--backupstorotate           (Default: 1) Number of previous backups to maintain.
                            1 = In-place sync mode (single directory that stays in sync)
                            2+ = Versioned backups (timestamped folders)

--projectstobackup          Comma separated list of project names OR project ids to backup. If none given, all projects will be
backed up.

--projectstoexclude         Comma separated list of project names OR project ids to exclude from the backup. Takes priority over
'projectstobackup'.

--debuglogging              (Default: false) Enable debug logging. Verbose.

--tracelogging              (Default: false) Enable trace logging. Extremely verbose.

--smtpport                  (Default: 25) Backup summary notification email: SMTP port.

--smtphost                  Backup summary notification email: SMTP server name.

--smtpfromaddress           Backup summary notification email: SMTP from address.

--smtpfromname              Backup summary notification email: SMTP from name.

--smtptoaddress             Backup summary notification email: SMTP to address.

--smtpusername              Backup summary notification email: SMTP username.

--smtppassword              Backup summary notification email: SMTP password.

--smtpenablessl             (Default: false) Backup summary notification email: SMTP over SSL.

--force-full-download       (Default: false) Force full download of all files, bypassing incremental
                            backup optimization. Use this if you want to ensure all files are
                            re-downloaded from Autodesk rather than copied from previous backup.

--help                      Display this help screen.

--version                   Display version information.
````

Basic:

```
ACCBackup.exe --backupdirectory "C:\ACCBackup" --clientid "DRO4zxzt71HCkL34cn2tAUSRS0OQGaRT" --clientsecret "tFRHKhuIrGQUi5d3" --accountid "9b3cc923-d920-4fee-ae91-5e9c8e4040rb"
```

Basic with email summary:

```
-ACCBackup.exe -backupdirectory "C:\ACCBackup" --clientid "DRO4zxzt71HCkL34cn2tAUSRS0OQGaRT" --clientsecret "tFRHKhuIrGQUi5d3" --accountid "9b3cc923-d920-4fee-ae91-5e9c8e4040rb" --smtphost "smtp.yourlocalnetwork.dc" --smtpfromaddress "noreply@example.com" --smtpfromname "ACCBackup" --smtptoaddress "me@stewartcelani.com"
```

Example of in-place sync mode (v1.2.0+ recommended for most users):

```
ACCBackup.exe --backupdirectory "C:\ACCBackup\Sync" --clientid "DRO4zxzt71HCkL34cn2tAUSRS0OQGaRT" --clientsecret "tFRHKhuIrGQUi5d3" --accountid "9b3cc923-d920-4fee-ae91-5e9c8e4040rb"
```

This maintains a single directory at C:\ACCBackup\Sync that stays synchronized with ACC. Only changed files are downloaded, and obsolete files are automatically removed.

Example of in-place sync with explicit parameter:

```
ACCBackup.exe --backupdirectory "C:\ACCBackup\Sync" --clientid "DRO4zxzt71HCkL34cn2tAUSRS0OQGaRT" --clientsecret "tFRHKhuIrGQUi5d3" --accountid "9b3cc923-d920-4fee-ae91-5e9c8e4040rb" --backupstorotate 1
```

Example of versioned daily backup with 5 rotating folders (v1.1.0 behavior):

```
ACCBackup.exe --backupdirectory "C:\ACCBackup\Daily" --clientid "DRO4zxzt71HCkL34cn2tAUSRS0OQGaRT" --clientsecret "tFRHKhuIrGQUi5d3" --accountid "9b3cc923-d920-4fee-ae91-5e9c8e4040rb" --backupstorotate 5
```

This creates timestamped folders and maintains the 5 most recent backups.

Example forcing a full download (bypassing incremental backup):

```
ACCBackup.exe --backupdirectory "C:\ACCBackup\Daily" --clientid "DRO4zxzt71HCkL34cn2tAUSRS0OQGaRT" --clientsecret "tFRHKhuIrGQUi5d3" --accountid "9b3cc923-d920-4fee-ae91-5e9c8e4040rb" --backupstorotate 1 --force-full-download
```

Use this when you suspect local files may be corrupted or want to ensure a fresh download from Autodesk.

Example of a weekly backup with 4 weekly backup folders rotating:

```
ACCBackup.exe --backupdirectory "C:\ACCBackup\Weekly" --clientid "DRO4zxzt71HCkL34cn2tAUSRS0OQGaRT" --clientsecret "tFRHKhuIrGQUi5d3" --accountid "9b3cc923-d920-4fee-ae91-5e9c8e4040rb" --backupstorotate 4
```

Example of a monthly backup with 12 monthly backup folders rotating:

```
ACCBackup.exe --backupdirectory "C:\ACCBackup\Monthly" --clientid "DRO4zxzt71HCkL34cn2tAUSRS0OQGaRT" --clientsecret "tFRHKhuIrGQUi5d3" --accountid "9b3cc923-d920-4fee-ae91-5e9c8e4040rb" --backupstorotate 12
```

Example backing up two projects (you can use project name or project id, as project name can change it is recommended to
use project id)

```
ACCBackup.exe --projectstobackup "Test Project, 7468066c-53e6-4acf-8086-6b5fce0048d6" --backupdirectory "C:\ACCBackup" --clientid "DRO4zxzt71HCkL34cn2tAUSRS0OQGaRT" --clientsecret "tFRHKhuIrGQUi5d3" --accountid "9b3cc923-d920-4fee-ae91-5e9c8e4040rb"
```

Example backing all projects BUT two projects

```
ACCBackup.exe --projectstoexclude "Test Project, 7468066c-53e6-4acf-8086-6b5fce0048d6" --backupdirectory "C:\ACCBackup" --clientid "DRO4zxzt71HCkL34cn2tAUSRS0OQGaRT" --clientsecret "tFRHKhuIrGQUi5d3" --accountid "9b3cc923-d920-4fee-ae91-5e9c8e4040rb"
```

### Debugging

- Log files are located in ACCBackup.exe\Logs
- Try running with --hubid "YourAccoundIdHere" OR --hubid "b.YourAccountIdHere" (Autodesk is migrating from BIM360 to '
  Autodesk Construction Cloud' but internally, at the moment, the API hubid needs b.AccountId (b. = BIM360).
- First step is to run with the --debuglogging flag
- If the --debuglogging flag doesn't point you in the right direction the --tracelogging flag should be used.
- If all else fails log an issue via the repo with a link to your --tracelogginng log file and I will investigate.
- Or/and email bold.oil7762@fastmail.com with your log file attached.

### ApiClient

ACCBackup is designed around an ApiClient class library I wrote that does all the heavy lifting.
This makes it very easy for you to take the class library and do your own thing with it if needed.
Below is a copy of ApiClient.UsageExample:Program.cs

````csharp
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


