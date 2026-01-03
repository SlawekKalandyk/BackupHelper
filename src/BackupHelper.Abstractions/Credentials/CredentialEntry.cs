namespace BackupHelper.Abstractions.Credentials;

public record CredentialEntry(string Title, string Username, string Password)
    : IDisplayableCredentialEntry
{
    public string ToDisplayString() => $"- {Title}; {Username}";

    public string GetLocalTitle()
    {
        var (_, localTitle) = CredentialHelper.DeconstructCredentialTitle(Title);
        return localTitle;
    }
}

public interface IDisplayableCredentialEntry
{
    string Title { get; }
    string Username { get; }

    string ToDisplayString();
}