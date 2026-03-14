namespace BackupHelper.Abstractions.Credentials;

public interface ICredentialsProvider : IDisposable
{
    public void SetCredential(ICredential credential)
    {
        using var credentialEntry = credential.ToCredentialEntry();
        SetCredential(credentialEntry);
    }

    public void UpdateCredential(ICredential credential)
    {
        using var credentialEntry = credential.ToCredentialEntry();
        UpdateCredential(credentialEntry);
    }

    public void DeleteCredential(ICredential credential)
    {
        using var credentialEntry = credential.ToCredentialEntry();
        DeleteCredential(credentialEntry);
    }

    public T? GetCredential<T>(ICredentialTitle credentialTitle)
        where T : ICredential => GetCredential<T>(credentialTitle.ToCredentialEntryTitle());

    public T? GetCredential<T>(CredentialEntry credentialEntry)
        where T : ICredential => GetCredential<T>(credentialEntry.EntryTitle);

    T? GetCredential<T>(CredentialEntryTitle credentialEntryTitle)
        where T : ICredential;
    void SetCredential(CredentialEntry credentialEntry);
    void UpdateCredential(CredentialEntry credentialEntry);
    void DeleteCredential(CredentialEntry credentialEntry);
    IReadOnlyCollection<CredentialEntry> GetCredentials();
}