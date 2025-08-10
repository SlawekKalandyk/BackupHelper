using BackupHelper.Abstractions;
using KeePassLib;
using KeePassLib.Interfaces;
using KeePassLib.Keys;
using KeePassLib.Security;
using KeePassLib.Serialization;

namespace BackupHelper.Core.Credentials;

public class KeePassCredentialsProvider : ICredentialsProvider
{
    private readonly IStatusLogger _statusLogger = new NullStatusLogger();
    private PwDatabase _database;

    public KeePassCredentialsProvider(string databasePath, string masterPassword)
    {
        _database = !File.Exists(databasePath)
                        ? CreateDatabase(databasePath, masterPassword)
                        : OpenDatabase(databasePath, masterPassword);
    }

    public (string Username, string Password) GetCredential(string credentialName)
    {
        var foundEntry = _database.RootGroup.Entries.SingleOrDefault(entry => entry.Strings.ReadSafe(PwDefs.TitleField) == credentialName);

        if (foundEntry == null)
            return (string.Empty, string.Empty);

        var user = foundEntry.Strings.ReadSafe(PwDefs.UserNameField);
        var pass = foundEntry.Strings.ReadSafe(PwDefs.PasswordField);

        return (user, pass);
    }

    public void SetCredential(string credentialName, string username, string password)
    {
        var existingEntry = _database.RootGroup.Entries.SingleOrDefault(entry => entry.Strings.ReadSafe(PwDefs.TitleField) == credentialName);

        if (existingEntry != null)
            throw new CredentialAlreadyExists(credentialName);

        var entry = new PwEntry(true, true);
        entry.Strings.Set(PwDefs.TitleField, new ProtectedString(false, credentialName));
        entry.Strings.Set(PwDefs.UserNameField, new ProtectedString(false, username));
        entry.Strings.Set(PwDefs.PasswordField, new ProtectedString(true, password));
        _database.RootGroup.AddEntry(entry, true);
        _database.Save(_statusLogger);
    }

    private PwDatabase CreateDatabase(string databasePath, string masterPassword)
    {
        var database = new PwDatabase();
        var compositeKey = new CompositeKey();
        compositeKey.AddUserKey(new KcpPassword(masterPassword));
        database.New(new IOConnectionInfo() { Path = databasePath }, compositeKey);
        database.Save(_statusLogger);

        return database;
    }

    private PwDatabase OpenDatabase(string databasePath, string masterPassword)
    {
        var database = new PwDatabase();
        var compositeKey = new CompositeKey();
        compositeKey.AddUserKey(new KcpPassword(masterPassword));
        database.Open(new IOConnectionInfo() { Path = databasePath }, compositeKey, _statusLogger);

        return database;
    }

    public void Dispose()
    {
        _database?.Close();
    }
}