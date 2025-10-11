using SMBLibrary;
using SMBLibrary.Client;

namespace BackupHelper.Sources.SMB.IO;

public abstract class SMBIOComponentBase : IDisposable
{
    protected SMBIOComponentBase(ISMBFileStore fileStore, object handle, FilePurpose filePurpose)
    {
        SMBFileStore = fileStore;
        Handle = handle;
        FilePurpose = filePurpose;
    }

    protected ISMBFileStore SMBFileStore { get; }
    public object Handle { get; }
    public FilePurpose FilePurpose { get; }

    public void Dispose()
    {
        SMBFileStore.CloseFile(Handle);
    }
}
