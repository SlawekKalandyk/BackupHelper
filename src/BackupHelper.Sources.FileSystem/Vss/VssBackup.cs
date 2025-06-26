using System.Diagnostics;
using Alphaleonis.Win32.Vss;
using Microsoft.Extensions.Logging;

namespace BackupHelper.Sources.FileSystem.Vss;

// Copied from AlphaVSS samples https://github.com/alphaleonis/AlphaVSS-Samples/blob/develop/src/VssBackup/VssBackup.cs

public class VssBackup : IDisposable
{
    private readonly ILogger<VssBackup> _logger;

    /// <summary>A reference to the VSS context.</summary>
    private IVssBackupComponents _backup;

    /// <summary>Some persistent context for the current snapshot.</summary>
    private Snapshot _snapshot;

    /// <summary>
    /// Constructs a VssBackup object and initializes some of the necessary
    /// VSS structures.
    /// </summary>
    public VssBackup(ILogger<VssBackup> logger)
    {
        _logger = logger;
        InitializeBackup();
    }

    /// <summary>
    /// Sets up a shadow copy against the specified volume.
    /// </summary>
    /// <remarks>
    /// This method is separated from the constructor because if it
    /// throws, we still want the Dispose() method to be called.
    /// </remarks>
    /// <param name="volumeName">Name of the volume to copy.</param>
    public void Setup(string volumeName)
    {
        Discovery(volumeName);
        PreBackup();
    }

    /// <summary>
    /// This simple method uses a bit of string manipulation to turn a
    /// full, local path into its corresponding snapshot path.  This
    /// method may help users perform full file copies from the snapshot.
    /// </summary>
    /// <remarks>
    /// Note that the System.IO methods are not able to access files on
    /// the snapshot.  Instead, you will need to use the AlphaFS library
    /// as shown in the example.
    /// </remarks>
    /// <example>
    /// This code creates a shadow copy and copies a single file from
    /// the new snapshot to a location on the D drive.  Here we're
    /// using the AlphaFS library to make a full-file copy of the file.
    /// <code>
    /// string source_file = @"C:\Windows\system32\config\sam";
    /// string backup_root = @"D:\Backups";
    /// string backup_path = Path.Combine(backup_root,
    ///       Path.GetFilename(source_file));
    ///
    /// // Initialize the shadow copy subsystem.
    /// using (VssBackup vss = new VssBackup())
    /// {
    ///    vss.Setup(Path.GetPathRoot(source_file));
    ///    string snap_path = vss.GetSnapshotPath(source_file);
    ///
    ///    // Here we use the AlphaFS library to make the copy.
    ///    Alphaleonis.Win32.Filesystem.File.Copy(snap_path, backup_path);
    /// }
    /// </code>
    /// </example>
    /// <seealso cref="GetStream"/>
    /// <param name="localPath">The full path of the original file.</param>
    /// <returns>A full path to the same file on the snapshot.</returns>
    public string GetSnapshotPath(string localPath)
    {
        Trace.WriteLine("New volume: " + _snapshot.Root);

        // This bit replaces the file's normal root information with root
        // info from our new shadow copy.
        if (Path.IsPathRooted(localPath))
        {
            string root = Path.GetPathRoot(localPath);
            localPath = localPath.Replace(root, String.Empty);
        }
        string slash = Path.DirectorySeparatorChar.ToString();
        if (!_snapshot.Root.EndsWith(slash) && !localPath.StartsWith(slash))
            localPath = localPath.Insert(0, slash);
        localPath = localPath.Insert(0, _snapshot.Root);

        Trace.WriteLine("Converted path: " + localPath);

        return localPath;
    }

    /// <summary>
    /// This method opens a stream over the shadow copy of the specified
    /// file.
    /// </summary>
    /// <example>
    /// This code creates a shadow copy and opens a stream over a file
    /// on the new snapshot volume.
    /// <code>
    /// string source_file = @"C:\Windows\system32\config\sam";
    ///
    /// // Initialize the shadow copy subsystem.
    /// using (VssBackup vss = new VssBackup())
    /// {
    ///    vss.Setup(Path.GetPathRoot(filename));
    ///
    ///    // We can now access the shadow copy by retrieving a stream:
    ///    using (Stream s = vss.GetStream(filename))
    ///    {
    ///       Debug.Assert(s.CanRead == true);
    ///       Debug.Assert(s.CanWrite == false);
    ///    }
    /// }
    /// </code>
    /// </example>
    public System.IO.Stream GetStream(string localPath)
    {
        // GetSnapshotPath() returns a very funky-looking path.  The
        // System.IO methods can't handle these sorts of paths, so instead
        // we're using AlphaFS, another excellent library by Alpha Leonis.
        // Note that we have no 'using System.IO' at the top of the file.
        // (The Stream it returns, however, is just a System.IO stream.)
        return File.OpenRead(GetSnapshotPath(localPath));
    }

        /// <summary>
    /// This stage initializes both the requester (this program) and
    /// any writers on the system in preparation for a backup and sets
    /// up a communication channel between the two.
    /// </summary>
    private void InitializeBackup()
    {
        // Here we are retrieving an OS-dependent object that encapsulates
        // all the VSS functionality. The OS independence that this single
        // factory method provides is one of AlphaVSS's major strengths!
        IVssFactory vss = VssFactoryProvider.Default.GetVssFactory();

        // Now we create a BackupComponents object to manage the backup.
        // This object will have a one-to-one relationship with its backup
        // and must be cleaned up when the backup finishes (i.e. it cannot
        // be reused).
        //
        // Note that this object is a member of our class, as it needs to
        // stick around for the full backup.
        _backup = vss.CreateVssBackupComponents();

        // Now we must initialize the components.  We can either start a
        // fresh backup by passing null here, or we could resume a previous
        // backup operation through an earlier use of the SaveXML method.
        _backup.InitializeForBackup(null);

        // At this point, we're supposed to establish communication with
        // the writers on the system.  It is possible before this step to
        // enable or disable specific writers via the BackupComponents'
        // Enable* and Disable* methods.
        _backup.GatherWriterMetadata();
    }

    /// <summary>
    /// This stage involves the requester (us, again) processing the
    /// information it received from writers on the system to find out
    /// which volumes - if any - must be shadow copied to perform a full
    /// backup.
    /// </summary>
    private void Discovery(string fullPath)
    {
        // Once we are finished with the writer metadata, we can dispose
        // of it.
        _backup.FreeWriterMetadata();

        // Now we use our helper class to add the appropriate volume to the
        // shadow copy set.
        _snapshot = new Snapshot(_backup);
        _snapshot.AddVolume(Path.GetPathRoot(fullPath));
    }

    /// <summary>
    /// This phase of the backup is focused around creating the shadow copy.
    /// We will notify writers of the impending snapshot, after which they
    /// have a short period of time to get their on-disk data in order and
    /// then quiesce writing.
    /// </summary>
    private void PreBackup()
    {
        Debug.Assert(_snapshot != null);

        // This next bit is a way to tell writers just what sort of backup
        // they should be preparing for.  The important parts for us now
        // are the first and third arguments: we want to do a full,
        // backup and, depending on whether we are in component mode, either
        // a full-volume backup or a backup that only requires specific
        // components.
        _backup.SetBackupState(
            false,
            true,
            VssBackupType.Full,
            false);

        // From here we just need to send messages to each writer that our
        // snapshot is imminent,
        // We simply block while the writers to complete their background preparations.
        _backup.PrepareForBackup();

        // It's now time to create the snapshot.  Each writer will have to
        // freeze its I/O to the selected volumes for up to 10 seconds
        // while this process takes place.
        _snapshot.Copy();
    }

    /// <summary>
    /// The final phase of the backup involves some cleanup steps.
    /// </summary>
    private void Complete()
    {
        try
        {
            // The BackupComplete event must be sent to all the writers.
            _backup.BackupComplete();
        }
        // Not sure why, but this throws a VSS_BAD_STATE on XP and W2K3.
        // Per some forum posts about this, I'm just ignoring it.
        catch (VssBadStateException ex)
        {
            _logger.LogWarning(ex, "VSS Bad State Exception during BackupComplete.");
        }
    }

    /// <summary>
    /// The disposal of this object involves sending completion notices
    /// to the writers, removing the shadow copies from the system and
    /// finally releasing the BackupComponents object.  This method must
    /// be called when this class is no longer used.
    /// </summary>
    public void Dispose()
    {
        try
        {
            Complete();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while completing the VSS backup.");
        }

        _snapshot.Dispose();
        _backup.Dispose();
    }
}