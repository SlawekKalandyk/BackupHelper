namespace BackupHelper.Abstractions.Credentials;

public interface ICredentialsProvider : IDisposable
{
    public void SetCredential(ICredential credential) =>
        SetCredential(credential.ToCredentialEntry());

    public void UpdateCredential(ICredential credential) =>
        UpdateCredential(credential.ToCredentialEntry());

    public void DeleteCredential(ICredential credential) =>
        DeleteCredential(credential.ToCredentialEntry());

    public T? GetCredential<T>(CredentialEntry credentialEntry)
        where T : ICredential => GetCredential<T>(credentialEntry.GetLocalTitle());

    /// <summary>
    /// Get credential by its local title (without provider prefix).
    /// </summary>
    T? GetCredential<T>(string credentialLocalTitle)
        where T : ICredential;
    void SetCredential(CredentialEntry credentialEntry);
    void UpdateCredential(CredentialEntry credentialEntry);
    void DeleteCredential(CredentialEntry credentialEntry);
    IReadOnlyCollection<CredentialEntry> GetCredentials();
}