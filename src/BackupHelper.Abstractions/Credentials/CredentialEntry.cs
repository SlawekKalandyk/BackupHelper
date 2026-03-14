namespace BackupHelper.Abstractions.Credentials;

public sealed record CredentialEntry(
    CredentialEntryTitle EntryTitle,
    string Username,
    SensitiveString Password
) : IDisplayableCredentialEntry, IDisposable
{
    public void Dispose() => Password.Dispose();

    public string ToDisplayString() => $"- {EntryTitle}; {Username}";
}

public interface IDisplayableCredentialEntry
{
    string ToDisplayString();
}
