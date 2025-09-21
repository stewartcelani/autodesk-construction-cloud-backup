# Changelog

## Version 1.2.0 (2025-09-22)

### Major Features

- **In-Place Sync Mode**: New backup approach that maintains a single synchronized directory instead of timestamped versions
    - Enabled by default with `--backupstorotate 1`
    - Automatically removes obsolete files no longer in ACC
    - Cleans up empty directories after file removal
    - Dramatically reduces storage requirements for regular backups
    - Existing files verified and only re-downloaded if changed

- **Enhanced Thread Safety**: Improved concurrent operations handling
    - Thread-safe directory creation with double-check locking pattern
    - Concurrent collections (ConcurrentBag) for parallel file operations
    - Optimized semaphore limits (3 concurrent project enumerations)
    - Prevents race conditions during parallel processing

### Breaking Changes

- **`--backupstorotate` parameter behavior changed**:
    - Value of 1 now enables In-Place Sync Mode (single synchronized directory)
    - Values of 2+ enable versioned backups with timestamped folders (v1.1.0 behavior)
    - Users wanting timestamped backups must update scripts to use `--backupstorotate 2` or higher

### Improvements

- Enhanced backup reporting with mode-specific messaging
- Detailed statistics showing unchanged vs downloaded files
- Better progress tracking and visibility
- Improved manifest handling for sync mode operations
- Automatic cleanup of obsolete files and directories in sync mode

### Technical Details

- Manifest stored and loaded from sync directory itself in sync mode
- File comparison uses FileId, VersionNumber, LastModifiedTime, and StorageSize
- Thread-safe counters using Interlocked operations
- Cleanup process handles both file deletion and empty directory removal

## Version 1.1.0 (2025-08-19)

### Major Features

- **Incremental Backup**: Dramatically reduces backup time by copying unchanged files from previous backup instead of
  re-downloading
    - 95% faster backups for mostly unchanged data (12 hours → 30 minutes for 250GB)
    - Automatic file integrity verification
    - Fallback to download if local copy fails verification
    - BackupManifest.json tracking for reliable change detection

### Bug Fixes

- **Fixed critical bug where `--backupstorotate 1` prevented incremental backup from working** - The rotation logic now
  correctly interprets this parameter as the number of previous backups to keep (in addition to current), ensuring at
  least one previous backup is always available for incremental comparison
- Fixed backup rotation timing - now rotates after backup completion instead of before
- Fixed issue where backup tool could attempt to delete its own directory during rotation
- Improved path handling for cross-platform compatibility
- Enhanced robustness for special characters in project names
- Improved error handling in manifest loading with specific exception types for better debugging

### Improvements

- Upgraded to .NET 9 for improved performance and latest runtime features
- Migrated from NLog to Serilog for improved logging capabilities and performance
- Added `--force-full-download` flag to bypass incremental backup when needed
- Better logging with incremental backup statistics
- Estimated time saved reporting
- Case-insensitive manifest lookups for cross-platform compatibility

### Technical Details

- Files are considered unchanged when FileId, VersionNumber, LastModifiedTime, and StorageSize all match
- Manifest uses normalized paths with forward slashes for consistency
- Project names are sanitized to handle special characters safely

## Version 1.0.4 (Previous Release)

- Updated to .NET 8 (Note: v1.1.0 now uses .NET 9)
- Migrated ApiClient to Autodesk's Authentication v2 endpoint as Authentication v1 goes offline April 30 2024
- Updated file download process to use the new AWS S3 signeds3download endpoint to fix authentication issues with
  certain users downloading
- Fixed download timeout bug