namespace BackupHelper.Core.Credentials;

[AttributeUsage(AttributeTargets.Class)]
public sealed class CredentialsProviderAttribute(Type providerType) : Attribute
{
    public Type ProviderType { get; } = providerType;
}
