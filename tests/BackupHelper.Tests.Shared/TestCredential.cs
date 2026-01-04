using BackupHelper.Abstractions.Credentials;

namespace BackupHelper.Tests.Shared;

public record TestCredential(string Title, string Username, string Password) : CredentialBase
{
    public const string CredentialType = "test";
    public override string Kind => CredentialType;

    protected override string GetLocalTitle() => Title;

    protected override string GetUsername() => Username;

    protected override string GetPassword() => Password;

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