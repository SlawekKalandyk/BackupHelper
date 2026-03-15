using BackupHelper.Abstractions.Credentials;

namespace BackupHelper.Tests.Shared;

public record TestCredential(string Title, string Username, SensitiveString Password)
    : CredentialBase<TestCredentialTitle>(new TestCredentialTitle(Title), Password)
{
    public const string CredentialType = "test";

    protected override string GetUsername() => Username;

    public static CredentialEntry CreateCredentialEntry(
        string title,
        string username,
        string password
    )
    {
        using var credential = new TestCredential(title, username, new SensitiveString(password));
        return credential.ToCredentialEntry();
    }
}

public record TestCredentialTitle(string Title) : CredentialTitleBase
{
    public override string Kind => TestCredential.CredentialType;

    public override IEnumerable<KeyValuePair<string, string>> GetTitleComponents() =>
        [NameValuePairHelper.ToNameValuePair(Title)];
}
