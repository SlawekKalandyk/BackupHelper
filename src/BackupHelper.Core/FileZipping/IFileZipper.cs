namespace BackupHelper.Core.FileZipping
{
    public interface IFileZipper : IDisposable
    {
        /// <summary>
        /// Does method <see cref="Save"/> have to be called manually
        /// </summary>
        bool HasToBeSaved { get; }

        /// <summary>
        /// Add file to zip archive
        /// </summary>
        /// <param name="filePath">Path to the file in the filesystem</param>
        /// <param name="zipPath">Path in zip archive the file should be saved under. If null or empty, save at the top level</param>
        void AddFile(string filePath, string zipPath = "");

        /// <summary>
        /// Add directory to zip archive. All files and subdirectories are saved "as is"
        /// </summary>
        /// <param name="directoryPath">Path to the directory in the filesystem</param>
        /// <param name="zipPath">Path in zip archive the directory should be saved under. If null or empty, save at the top level</param>
        void AddDirectory(string directoryPath, string zipPath = "");

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
    }

    public interface IFileZipperFactory
    {
        IFileZipper Create(string zipFilePath, bool overwriteFileIfExists);
    }
}