namespace BackupHelper.Core.Credentials;

public interface ICredentialsManager : IDisposable
{
    (string Username, string Password) GetCredential(string credentialName);
    void SetCredential(string credentialName, string username, string password);
}