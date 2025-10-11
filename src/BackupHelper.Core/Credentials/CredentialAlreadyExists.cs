namespace BackupHelper.Core.Credentials;

public class CredentialAlreadyExists(string credentialName)
    : Exception($"An entry with the name '{credentialName}' already exists.");
