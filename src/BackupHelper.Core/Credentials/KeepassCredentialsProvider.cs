using BackupHelper.Abstractions;
using KeePassLib;
using KeePassLib.Interfaces;
using KeePassLib.Keys;
using KeePassLib.Security;
using KeePassLib.Serialization;

namespace BackupHelper.Core.Credentials;

public record KeePassCredentialsProviderConfiguration(string DatabasePath, string MasterPassword)
    : ICredentialsProviderConfiguration;

public class KeePassCredentialsProvider : ICredentialsProvider
{
    private static readonly IStatusLogger _statusLogger = new NullStatusLogger();
    private PwDatabase _database;

    public KeePassCredentialsProvider(KeePassCredentialsProviderConfiguration configuration)
    {
        _database = !File.Exists(configuration.DatabasePath)
            ? CreateDatabase(configuration.DatabasePath, configuration.MasterPassword)
            : OpenDatabase(configuration.DatabasePath, configuration.MasterPassword);
    }

    public static bool CanLogin(string databasePath, string password)
    {
        if (!File.Exists(databasePath))
            return false;

        var database = new PwDatabase();
        var compositeKey = new CompositeKey();
        compositeKey.AddUserKey(new KcpPassword(password));

        try
        {
            database.Open(
                new IOConnectionInfo() { Path = databasePath },
                compositeKey,
                _statusLogger
            );

            return database.IsOpen;
        }
        catch
        {
            return false;
        }
        finally
        {
            database?.Close();
        }
    }

    public CredentialEntry? GetCredential(string credentialName)
    {
        var foundEntry = _database.RootGroup.Entries.SingleOrDefault(entry =>
            entry.Strings.ReadSafe(PwDefs.TitleField) == credentialName
        );

        if (foundEntry == null)
            return null;

        var user = foundEntry.Strings.ReadSafe(PwDefs.UserNameField);
        var pass = foundEntry.Strings.ReadSafe(PwDefs.PasswordField);

        return new CredentialEntry(credentialName, user, pass);
    }

    public void SetCredential(CredentialEntry credentialEntry)
    {
        var existingEntry = _database.RootGroup.Entries.SingleOrDefault(entry =>
            entry.Strings.ReadSafe(PwDefs.TitleField) == credentialEntry.Title
        );

        if (existingEntry != null)
            throw new CredentialAlreadyExists(credentialEntry.Title);

        var entry = new PwEntry(true, true);
        entry.Strings.Set(PwDefs.TitleField, new ProtectedString(false, credentialEntry.Title));
        entry.Strings.Set(
            PwDefs.UserNameField,
            new ProtectedString(false, credentialEntry.Username)
        );

        if (!string.IsNullOrWhiteSpace(credentialEntry.Password))
            entry.Strings.Set(
                PwDefs.PasswordField,
                new ProtectedString(true, credentialEntry.Password)
            );

        _database.RootGroup.AddEntry(entry, true);
        _database.Save(_statusLogger);
    }

    public void UpdateCredential(CredentialEntry credentialEntry)
    {
        var existingEntry = _database.RootGroup.Entries.SingleOrDefault(entry =>
            entry.Strings.ReadSafe(PwDefs.TitleField) == credentialEntry.Title
        );

        if (existingEntry == null)
            throw new CredentialNotFound(credentialEntry.Title);

        existingEntry.Strings.Set(
            PwDefs.UserNameField,
            new ProtectedString(false, credentialEntry.Username)
        );

        if (!string.IsNullOrWhiteSpace(credentialEntry.Password))
            existingEntry.Strings.Set(
                PwDefs.PasswordField,
                new ProtectedString(true, credentialEntry.Password)
            );

        _database.Save(_statusLogger);
    }

    public void DeleteCredential(string credentialName)
    {
        var existingEntry = _database.RootGroup.Entries.SingleOrDefault(entry =>
            entry.Strings.ReadSafe(PwDefs.TitleField) == credentialName
        );

        if (existingEntry == null)
            throw new CredentialNotFound(credentialName);

        _database.RootGroup.Entries.Remove(existingEntry);
        _database.Save(_statusLogger);
    }

    public IReadOnlyCollection<CredentialEntry> GetCredentials()
    {
        var entries = _database.RootGroup.Entries;

        return entries
            .Select(entry => new CredentialEntry(
                entry.Strings.ReadSafe(PwDefs.TitleField),
                entry.Strings.ReadSafe(PwDefs.UserNameField),
                entry.Strings.ReadSafe(PwDefs.PasswordField)
            ))
            .ToList();
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

    private static PwDatabase OpenDatabase(string databasePath, string masterPassword)
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
