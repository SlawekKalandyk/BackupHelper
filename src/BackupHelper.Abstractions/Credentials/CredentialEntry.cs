namespace BackupHelper.Abstractions.Credentials;

public sealed record CredentialEntry(
    CredentialEntryTitle EntryTitle,
    string Username,
    string Password
) : IDisplayableCredentialEntry
{
    public string ToDisplayString() => $"- {EntryTitle}; {Username}";
}

public interface IDisplayableCredentialEntry
{
    string ToDisplayString();
}