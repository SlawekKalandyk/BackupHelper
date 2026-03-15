using BackupHelper.Abstractions.Credentials;

namespace BackupHelper.Tests.Shared;

public record TestCredential : CredentialBase<TestCredentialTitle>
{
    public TestCredential(string title, string username, SensitiveString password)
        : base(new TestCredentialTitle(title), password)
    {
        Title = title;
        Username = username;
    }

    public const string CredentialType = "test";

    public string Title { get; }
    public string Username { get; }
    public SensitiveString Password => CredentialPassword;

    protected override string GetUsername() => Username;

    public static CredentialEntry CreateCredentialEntry(
        string title,
        string username,
        string password
    )
    {
        using var sensitivePassword = new SensitiveString(password);
        using var credential = new TestCredential(title, username, sensitivePassword);
        return credential.ToCredentialEntry();
    }
}

public record TestCredentialTitle(string Title) : CredentialTitleBase
{
    public override string Kind => TestCredential.CredentialType;

    public override IEnumerable<KeyValuePair<string, string>> GetTitleComponents() =>
        [NameValuePairHelper.ToNameValuePair(Title)];
}
