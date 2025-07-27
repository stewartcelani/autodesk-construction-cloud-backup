# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This is a C# console application built to backup all Autodesk Construction Cloud/BIM360 projects via Autodesk Platform Services (formerly Autodesk Forge). The solution consists of multiple projects organized around a core ApiClient library that handles all communication with Autodesk APIs.

## Architecture

### Core Components

1. **ApiClient** - Main library handling Autodesk API communication
   - `ApiClient.cs` - Core client implementation with retry policies and authentication
   - `TwoLeggedApiClient.cs` - Builder pattern implementation for client configuration
   - Entities: `Project.cs`, `Folder.cs`, `File.cs` - Domain models
   - RestApiResponses: DTOs for API responses

2. **Backup** - Console application that orchestrates the backup process
   - `Backup.cs` - Main backup logic with parallel file downloading
   - `Program.cs` - Entry point with command-line argument parsing
   - Uses CommandLineParser for CLI interface

3. **Library Components**
   - `Library.Logger` - NLog-based logging abstraction
   - `Library.SecretsManager` - Environment variable management
   - `Library.Extensions` - Extension methods
   - `Library.Testing` - Test helpers including MockHttpMessageHandler

## Build Commands

```bash
# Build the entire solution
dotnet build

# Build specific project
dotnet build ApiClient/ApiClient.csproj
dotnet build Backup/Backup.csproj

# Build in Release mode
dotnet build -c Release

# Run the backup application
dotnet run --project Backup/Backup.csproj -- [arguments]

# Build the executable
dotnet publish Backup/Backup.csproj -c Release -r win-x64 --self-contained false
```

## Test Commands

```bash
# Run all tests
dotnet test

# Run specific test project
dotnet test ApiClient.UnitTests/ApiClient.UnitTests.csproj
dotnet test Backup.UnitTests/Backup.UnitTests.csproj

# Run tests with detailed output
dotnet test --logger "console;verbosity=detailed"

# Run specific test by name
dotnet test --filter "FullyQualifiedName~TestMethodName"
```

## Key Technical Details

- **Target Framework**: .NET 8.0
- **Test Framework**: xUnit with FluentAssertions
- **Mocking**: NSubstitute
- **Logging**: NLog via abstraction layer
- **CLI Parsing**: CommandLineParser
- **JSON**: Newtonsoft.Json

## Authentication Flow

The application uses Autodesk's two-legged OAuth authentication:
1. Client credentials (ID + Secret) are exchanged for an access token
2. Token is cached and refreshed automatically before expiration
3. All API calls include the bearer token in Authorization header

## Parallel Processing

The backup process uses configurable parallel downloading:
- Default: 8 concurrent downloads
- Configurable via `--maxdegreeofparallelism` parameter
- Files are downloaded using `Parallel.ForEachAsync` with semaphore throttling

## Error Handling

- Comprehensive retry logic with exponential backoff
- Default: 15 retry attempts with 2-second initial delay
- All API errors are logged with detailed context
- HTTP 429 (rate limiting) is handled with appropriate delays

## Important File Paths

- Logs: `ACCBackup.exe/Logs/` (when running the compiled executable)
- Backup structure: `BackupDirectory/BackupTimestamp/ProjectName/[folder structure]`