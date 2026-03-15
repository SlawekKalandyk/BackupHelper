namespace BackupHelper.Abstractions.Credentials;

public sealed record CredentialEntry : IDisplayableCredentialEntry, IDisposable
{
    public CredentialEntry(CredentialEntryTitle entryTitle, string username, SensitiveString password)
    {
        EntryTitle = entryTitle;
        Username = username;
        Password = password.Clone();
    }

    public CredentialEntryTitle EntryTitle { get; }
    public string Username { get; }
    public SensitiveString Password { get; }

    public void Dispose() => Password.Dispose();

    public string ToDisplayString() => $"- {EntryTitle}; {Username}";
}

public interface IDisplayableCredentialEntry
{
    string ToDisplayString();
}
