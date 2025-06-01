using System.IO.Compression;

namespace BackupHelper.Core.FileInUseZipEntryHandler;

public class DefaultFileInUseZipEntryHandlerFactory : IFileInUseZipEntryHandlerFactory
{
    public IFileInUseZipEntryHandler Create()
    {
        return new DefaultFileInUseZipEntryHandler();
    }
}

public class DefaultFileInUseZipEntryHandler : IFileInUseZipEntryHandler
{
    public void AddEntry(ZipArchive zipArchive, string filePath, string zipPath, CompressionLevel compressionLevel)
    {
        using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        var entry = zipArchive.CreateEntry(zipPath, CompressionLevel.Optimal);
        using var entryStream = entry.Open();
        fileStream.CopyTo(entryStream);
    }

    public void Dispose()
    {

    }
}