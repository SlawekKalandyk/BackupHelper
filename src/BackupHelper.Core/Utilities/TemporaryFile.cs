using System;
using System.IO;

namespace BackupHelper.Core.Utilities;

/// <summary>
/// Represents a temporary file that is automatically deleted when disposed.
/// </summary>
internal class TemporaryFile : IDisposable
{
    private readonly string _filePath;
    private bool _disposed;

    /// <summary>
    /// Creates a temporary file with a random name in the system temp folder.
    /// </summary>
    public TemporaryFile()
        : this(Path.Combine(Path.GetTempPath(), Path.GetRandomFileName())) { }

    /// <summary>
    /// Creates a temporary file at the specified path.
    /// </summary>
    public TemporaryFile(string filePath)
    {
        _filePath = filePath;
    }

    /// <summary>
    /// Gets the file path of the temporary file.
    /// </summary>
    public string FilePath => _filePath;

    public void Dispose()
    {
        if (!_disposed)
        {
            if (File.Exists(_filePath))
            {
                try
                {
                    File.Delete(_filePath);
                }
                catch
                {
                    // Ignore exceptions during cleanup
                }
            }
            _disposed = true;
        }
        GC.SuppressFinalize(this);
    }

    ~TemporaryFile()
    {
        Dispose();
    }
}
