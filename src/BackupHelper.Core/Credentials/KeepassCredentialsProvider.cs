using System.Security.Cryptography;
using BackupHelper.Abstractions.Credentials;
using KeePassLib;
using KeePassLib.Interfaces;
using KeePassLib.Keys;
using KeePassLib.Security;
using KeePassLib.Serialization;

namespace BackupHelper.Core.Credentials;

public record KeePassCredentialsProviderConfiguration(
    string DatabasePath,
    Func<SensitiveString> MasterPasswordFactory
) : ICredentialsProviderConfiguration;

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
            ? CreateDatabase(configuration.DatabasePath, configuration.MasterPasswordFactory)
            : OpenDatabase(configuration.DatabasePath, configuration.MasterPasswordFactory);
    }

    public static bool CanLogin(string databasePath, Func<SensitiveString> passwordFactory)
    {
        if (!File.Exists(databasePath))
            return false;

        var database = new PwDatabase();
        var compositeKey = new CompositeKey();
        using var sensitivePassword = passwordFactory();
        compositeKey.AddUserKey(GetKcpPassword(sensitivePassword));

        try
        {
            database.Open(
                new IOConnectionInfo() { Path = databasePath },
                compositeKey,
                _statusLogger
            );

            return database.IsOpen;
        }
        catch (Exception)
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
        var pass = new SensitiveString(foundEntry.Strings.Get(PwDefs.PasswordField).ReadUtf8());
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
            new ProtectedString(true, credentialEntry.Username)
        );

        if (!credentialEntry.Password.IsEmpty)
        {
            SetEntryPassword(entry, credentialEntry.Password);
        }

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
            new ProtectedString(true, credentialEntry.Username)
        );

        if (!credentialEntry.Password.IsEmpty)
        {
            SetEntryPassword(existingEntry, credentialEntry.Password);
        }

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
                new SensitiveString(entry.Strings.Get(PwDefs.PasswordField).ReadUtf8())
            ))
            .ToList();
    }

    private PwDatabase CreateDatabase(string databasePath, Func<SensitiveString> passwordFactory)
    {
        var database = new PwDatabase();
        var compositeKey = new CompositeKey();
        using var sensitivePassword = passwordFactory();
        compositeKey.AddUserKey(GetKcpPassword(sensitivePassword));
        database.New(new IOConnectionInfo() { Path = databasePath }, compositeKey);
        database.Save(_statusLogger);

        return database;
    }

    private static PwDatabase OpenDatabase(
        string databasePath,
        Func<SensitiveString> passwordFactory
    )
    {
        var database = new PwDatabase();
        var compositeKey = new CompositeKey();
        using var sensitivePassword = passwordFactory();
        compositeKey.AddUserKey(GetKcpPassword(sensitivePassword));
        database.Open(new IOConnectionInfo() { Path = databasePath }, compositeKey, _statusLogger);

        return database;
    }

    private void SetEntryPassword(PwEntry entry, SensitiveString password)
    {
        var passwordBytes = password.ExposeUtf8Bytes().ToArray();
        try
        {
            entry.Strings.Set(PwDefs.PasswordField, new ProtectedString(true, passwordBytes));
        }
        finally
        {
            CryptographicOperations.ZeroMemory(passwordBytes);
        }
    }

    private static KcpPassword GetKcpPassword(SensitiveString password)
    {
        var passwordBytes = password.ExposeUtf8Bytes().ToArray();
        try
        {
            return new KcpPassword(passwordBytes);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(passwordBytes);
        }
    }

    public void Dispose()
    {
        _database?.Close();
    }
}