namespace BackupHelper.Core.FileZipping;

public interface IFileZipperFactory
{
    IFileZipper Create(string zipFilePath, bool overwriteFileIfExists, string? password = null);
}

public interface IFileZipper : IDisposable
{
    /// <summary>
    /// Does method <see cref="Save"/> have to be called manually
    /// </summary>
    bool HasToBeSaved { get; }

    /// <summary>
    /// Limit number of threads used for compression. Default is 1 (no multithreading).
    /// </summary>
    int ThreadLimit { get; set; }

    /// <summary>
    /// Limit memory usage in MB. By default, no limit is set (0).
    /// </summary>
    int MemoryLimitMB { get; set; }

    /// <summary>
    /// Compression level from 0 (no compression) to 9 (maximum compression). Default is 9.
    /// </summary>
    int DefaultCompressionLevel { get; }

    /// <summary>
    /// List of files that failed to be added to the zip archive
    /// </summary>
    IReadOnlyCollection<string> FailedFiles { get; }

    /// <summary>
    /// Add file to zip archive
    /// </summary>
    /// <param name="filePath">Path to the file in the filesystem</param>
    /// <param name="zipPath">Path in zip archive the file should be saved under. If null or empty, save at the top level</param>
    /// <param name="compressionLevel">Optional compression level for this file (0-9). If not specified, use default.</param>
    void AddFile(string filePath, string zipPath = "", int? compressionLevel = null);

    /// <summary>
    /// Add directory to zip archive. All files and subdirectories are saved "as is"
    /// </summary>
    /// <param name="directoryPath">Path to the directory in the filesystem</param>
    /// <param name="zipPath">Path in zip archive the directory should be saved under. If null or empty, save at the top level</param>
    /// <param name="compressionLevel">Optional compression level for this directory (0-9). If not specified, use default.</param>
    void AddDirectory(string directoryPath, string zipPath = "", int? compressionLevel = null);

    /// <summary>
    /// Add directory's contents to zip archive. All files and subdirectories are saved "as is"
    /// </summary>
    /// <param name="directoryPath">Path to the directory in the filesystem</param>
    /// <param name="zipPath">Path in zip archive the directory's contents should be saved under. If null or empty, save at the top level</param>
    void AddDirectoryContent(string directoryPath, string zipPath = "");

    /// <summary>
    /// Save created zip archive
    /// </summary>
    void Save();

    /// <summary>
    /// Wait for all queued operations to complete
    /// </summary>
    void Wait();
}