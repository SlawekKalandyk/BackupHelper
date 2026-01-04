using BackupHelper.Abstractions.Credentials;
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
    private readonly CredentialHandlerRegistry _credentialHandlerRegistry;
    private static readonly IStatusLogger _statusLogger = new NullStatusLogger();
    private PwDatabase _database;

    public KeePassCredentialsProvider(
        KeePassCredentialsProviderConfiguration configuration,
        CredentialHandlerRegistry credentialHandlerRegistry
    )
    {
        _credentialHandlerRegistry = credentialHandlerRegistry;
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

    public T? GetCredential<T>(CredentialEntryTitle credentialEntryTitle)
        where T : ICredential
    {
        var foundEntry = _database.RootGroup.Entries.SingleOrDefault(entry =>
            CredentialEntryTitle.Parse(entry.Strings.ReadSafe(PwDefs.TitleField))
            == credentialEntryTitle
        );

        if (foundEntry == null)
            return default;

        var user = foundEntry.Strings.ReadSafe(PwDefs.UserNameField);
        var pass = foundEntry.Strings.ReadSafe(PwDefs.PasswordField);
        var entry = new CredentialEntry(credentialEntryTitle, user, pass);

        return _credentialHandlerRegistry.FromCredentialEntry<T>(entry);
    }

    public void SetCredential(CredentialEntry credentialEntry)
    {
        var title = credentialEntry.EntryTitle.ToString();

        var existingEntry = _database.RootGroup.Entries.SingleOrDefault(entry =>
            CredentialEntryTitle.Parse(entry.Strings.ReadSafe(PwDefs.TitleField))
            == credentialEntry.EntryTitle
        );

        if (existingEntry != null)
            throw new CredentialAlreadyExists(title);

        var entry = new PwEntry(true, true);
        entry.Strings.Set(PwDefs.TitleField, new ProtectedString(false, title));
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
        var title = credentialEntry.EntryTitle;

        var existingEntry = _database.RootGroup.Entries.SingleOrDefault(entry =>
            CredentialEntryTitle.Parse(entry.Strings.ReadSafe(PwDefs.TitleField)) == title
        );

        if (existingEntry == null)
            throw new CredentialNotFound(title.ToString());

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

    public void DeleteCredential(CredentialEntry credentialEntry)
    {
        var title = credentialEntry.EntryTitle;

        var existingEntry = _database.RootGroup.Entries.SingleOrDefault(entry =>
            CredentialEntryTitle.Parse(entry.Strings.ReadSafe(PwDefs.TitleField)) == title
        );

        if (existingEntry == null)
            throw new CredentialNotFound(title.ToString());

        _database.RootGroup.Entries.Remove(existingEntry);
        _database.Save(_statusLogger);
    }

    public IReadOnlyCollection<CredentialEntry> GetCredentials()
    {
        var entries = _database.RootGroup.Entries;

        return entries
            .Select(entry => new CredentialEntry(
                CredentialEntryTitle.Parse(entry.Strings.ReadSafe(PwDefs.TitleField)),
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