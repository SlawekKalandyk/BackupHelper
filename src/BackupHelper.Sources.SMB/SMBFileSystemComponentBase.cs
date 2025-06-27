using SMBLibrary;
using SMBLibrary.Client;

namespace BackupHelper.Sources.SMB;

public abstract class SMBFileSystemComponentBase : IDisposable
{
    protected SMBFileSystemComponentBase(ISMBFileStore fileStore, object handle, FilePurpose filePurpose)
    {
        SMBFileStore = fileStore;
        Handle = handle;
        FilePurpose = filePurpose;
    }


    protected ISMBFileStore SMBFileStore { get; }
    public object Handle { get; }
    public FilePurpose FilePurpose { get; }

    public void Delete()
    {
        if (FilePurpose != FilePurpose.Delete)
            throw new InvalidOperationException("This file cannot be deleted. It was not opened for deletion.");

        var fileDispositionInformation = new FileDispositionInformation
        {
            DeletePending = true
        };
        var status = SMBFileStore.SetFileInformation(Handle, fileDispositionInformation);
        SMBHelper.ThrowIfStatusNotSuccess(status, nameof(SMBFileStore.SetFileInformation));
    }

    public void Dispose()
    {
        SMBFileStore.CloseFile(Handle);
    }
}