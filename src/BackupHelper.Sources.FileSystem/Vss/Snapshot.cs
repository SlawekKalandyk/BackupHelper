using Alphaleonis.Win32.Vss;

namespace BackupHelper.Sources.FileSystem.Vss;

// Copied from AlphaVSS samples https://github.com/alphaleonis/AlphaVSS-Samples/blob/develop/src/VssBackup/Snapshot.cs

/// <summary>
/// Utility class to manage the snapshot's contents and ID.
/// </summary>
public class Snapshot : IDisposable
{
    /// <summary>A reference to the VSS context.</summary>
    private readonly IVssBackupComponents _backup;

    /// <summary>Identifier for the overall shadow copy.</summary>
    private readonly Guid _setId;

    /// <summary>Metadata about this object's snapshot.</summary>
    private VssSnapshotProperties _properties;

    /// <summary>Identifier for our single snapshot volume.</summary>
    private Guid _snapId;

    /// <summary>
    /// Initializes a snapshot.  We save the GUID of this snap in order to
    /// refer to it elsewhere in the class.
    /// </summary>
    /// <param name="backup">A VssBackupComponents implementation for the current OS.</param>
    public Snapshot(IVssBackupComponents backup)
    {
        _backup = backup;
        _setId = backup.StartSnapshotSet();
    }

    /// <summary>
    /// Gets the string that identifies the root of this snapshot.
    /// </summary>
    public string Root
    {
        get
        {
            if (_properties == null)
                _properties = _backup.GetSnapshotProperties(_snapId);

            return _properties.SnapshotDeviceObject;
        }
    }

    /// <summary>
    /// Adds a volume to the current snapshot.
    /// </summary>
    /// <param name="volumeName">Name of the volume to add (e.g. "C:\").</param>
    /// <remarks>
    /// Note the IsVolumeSupported check prior to adding each volume.
    /// </remarks>
    public void AddVolume(string volumeName)
    {
        if (_backup.IsVolumeSupported(volumeName))
            _snapId = _backup.AddToSnapshotSet(volumeName);
        else
            throw new VssVolumeNotSupportedException(volumeName);
    }

    /// <summary>
    /// Create the actual snapshot.  This process can take around 10s.
    /// </summary>
    public void Copy()
    {
        _backup.DoSnapshotSet();
    }

    /// <summary>
    /// Remove all snapshots.
    /// </summary>
    public void Delete()
    {
        _backup.DeleteSnapshotSet(_setId, false);
    }

    /// <summary>
    /// Dispose of the shadow copies created by this instance.
    /// </summary>
    public void Dispose()
    {
        try
        {
            Delete();
        }
        catch { }
    }
}
