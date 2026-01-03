using BackupHelper.Abstractions.Credentials;

namespace BackupHelper.Tests.Shared;

public record TestCredential(string Title, string Username, string Password) : ICredential
{
    public const string CredentialType = "test";
    public string Kind => CredentialType;

    public CredentialEntry ToCredentialEntry()
    {
        var title = CredentialHelper.ConstructCredentialTitle(CredentialType, Title);
        return new CredentialEntry(title, Username, Password);
    }

    public static CredentialEntry CreateCredentialEntry(
        string title,
        string username,
        string password
    )
    {
        var credential = new TestCredential(title, username, password);
        return credential.ToCredentialEntry();
    }
}