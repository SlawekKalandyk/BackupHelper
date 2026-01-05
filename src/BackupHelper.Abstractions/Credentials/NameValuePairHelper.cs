using System.Runtime.CompilerServices;

namespace BackupHelper.Abstractions.Credentials;

public static class NameValuePairHelper
{
    public static KeyValuePair<string, string> ToNameValuePair(
        string? value,
        [CallerArgumentExpression("value")] string? name = null
    ) => new(name ?? string.Empty, value ?? string.Empty);
}