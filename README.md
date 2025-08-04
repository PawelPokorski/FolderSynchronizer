# Folder Synchronizer

## Project Description

This is a C# program that performs a one-way synchronization between two folders: a **source** folder and a **replica** folder. The goal is to ensure the replica folder's contents are always an exact copy of the source folder.

### Key Features
* **One-Way Synchronization**: The replica folder is modified to match the source folder. Any changes in the source are applied to the replica.
* **Scheduled Execution**: The synchronization process runs at a regular, user-defined interval.
* **Logging**: All file and folder operations (creation, copying, deletion) are logged to both the console and a specified log file.
* **Command-Line Configuration**: All necessary parameters: _source path_, _replica path_, _sync interval_, and _log file path_ are provided via command-line arguments.

## Technology
* .NET Core 8.0

## Libraries
* NUnit
* Moq
* Microsoft.Extensions.Hosting

## Instalation

1. Clone the repository:

  ```bash
  git clone https://github.com/PawelPokorski/FolderSynchronizer.git
  ```
2. Make sure you have the .NET Core 8.0 SDK installed.
3. Open the project.
4. Use `appsettings.json` to type input arguments in the format:
   
 ```json
  {
    "WorkerOptions": {
      "SyncInterval": 1000,                            // in milliseconds
      "LogFileFullPath": "F:/test/logs/logFile.txt",   // log file path with file name
      "CopyFromPath": "F:/test/source",                // source file path
      "CopyToPath": "F:/test/replica"                  // destination file path
    }
  }
 ```
5. Compile and run the project.
6. **If you want to use _command line_ to run the application, provide input arguments in the exact format:**
   
```bash
./FolderSynchronizer.exe 1000 F:/test/logs/logFile.txt F:/test/source F:/test/replica
```

**Note: Make sure the source directory exists before start.**
