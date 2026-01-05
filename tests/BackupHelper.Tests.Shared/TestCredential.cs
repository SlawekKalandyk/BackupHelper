using BackupHelper.Abstractions.Credentials;

namespace BackupHelper.Tests.Shared;

public record TestCredential(string Title, string Username, string Password)
    : CredentialBase<TestCredentialTitle>(new TestCredentialTitle(Title))
{
    public const string CredentialType = "test";

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

public record TestCredentialTitle(string Title) : CredentialTitleBase
{
    public override string Kind => TestCredential.CredentialType;

    public override IEnumerable<KeyValuePair<string, string>> GetTitleComponents() =>
        [NameValuePairHelper.ToNameValuePair(Title)];
}