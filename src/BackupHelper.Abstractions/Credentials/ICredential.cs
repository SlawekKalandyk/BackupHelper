namespace BackupHelper.Abstractions.Credentials;

public interface ICredential
{
    /// <summary>Credential kind identifier (e.g. "smb", "azure").</summary>
    string Kind { get; }

    /// <summary>
    /// Converts the credential to a CredentialEntry for storage or transmission.
    /// </summary>
    CredentialEntry ToCredentialEntry();
}

public abstract record CredentialBase : ICredential
{
    public abstract string Kind { get; }

    protected abstract string GetLocalTitle();

    protected abstract string GetUsername();

    protected abstract string GetPassword();

    public CredentialEntry ToCredentialEntry()
    {
        return new CredentialEntry(FullTitle, GetUsername(), GetPassword());
    }

    private string FullTitle => ComposeTitle(Kind, GetLocalTitle());

    private string ComposeTitle(string name, string localTitle) => $"{name}|{localTitle}";
}
