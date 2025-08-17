namespace BackupHelper.Abstractions;

public record CredentialEntry(string Title, string Username, string Password)
{
    public string ToDisplayString()
        => $"- {Title}{Environment.NewLine}  Username: {Username}";
}