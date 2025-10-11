namespace BackupHelper.Abstractions;

public record CredentialEntry(string Title, string Username, string Password)
    : IDisplayableCredentialEntry
{
    public string ToDisplayString() => $"- {Title}; {Username}";
}

public interface IDisplayableCredentialEntry
{
    string Title { get; }
    string Username { get; }

    string ToDisplayString();
}
