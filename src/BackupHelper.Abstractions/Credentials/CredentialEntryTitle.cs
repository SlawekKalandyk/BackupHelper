using System.Data.Common;
using System.Runtime.CompilerServices;

namespace BackupHelper.Abstractions.Credentials;

public sealed class CredentialEntryTitle : IEquatable<CredentialEntryTitle>
{
    private static readonly StringComparer _keyComparer = StringComparer.OrdinalIgnoreCase;
    private static readonly StringComparison _keyComparison = StringComparison.OrdinalIgnoreCase;
    private readonly int _hashCode;

    public static CredentialTitleBuilder Builder => new();
    public IReadOnlyDictionary<string, string> Pairs { get; }
    public string Kind
    {
        get
        {
            if (Pairs.TryGetValue(nameof(ICredentialTitle.Kind), out var kind))
                return kind;
            throw new InvalidOperationException(
                $"CredentialEntryTitle does not contain a '{nameof(ICredentialTitle.Kind)}' key."
            );
        }
    }

    public CredentialEntryTitle(params IEnumerable<KeyValuePair<string, string>> pairs)
    {
        ArgumentNullException.ThrowIfNull(pairs);

        // Normalize: trim keys, reject null/empty keys, store values as non-null strings.
        var dict = new Dictionary<string, string>(_keyComparer);

        foreach (var keyValuePair in pairs)
        {
            var key = keyValuePair.Key.Trim();
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Key cannot be null/empty/whitespace.", nameof(pairs));

            dict[key] = keyValuePair.Value;
        }

        Pairs = dict;

        // Precompute stable hash based on canonical ordering.
        _hashCode = ComputeHashCode(dict);
    }

    public static CredentialEntryTitle Parse(string title)
    {
        ArgumentNullException.ThrowIfNull(title);

        var builder = new DbConnectionStringBuilder { ConnectionString = title };

        var dictionary = new Dictionary<string, string>(_keyComparer);
        foreach (var keyObj in builder.Keys)
        {
            var key = Convert.ToString(keyObj)?.Trim();
            if (string.IsNullOrEmpty(key))
                continue;

            var value = builder[key];
            dictionary[key] = Convert.ToString(value) ?? string.Empty;
        }

        return new CredentialEntryTitle(dictionary);
    }

    public bool TryGet(string? key, out string value)
    {
        if (key is null)
        {
            value = string.Empty;
            return false;
        }

        return Pairs.TryGetValue(key.Trim(), out value!);
    }

    public string ToConnectionString()
    {
        var builder = new DbConnectionStringBuilder();

        foreach (var (key, value) in Pairs.OrderBy(p => p.Key, _keyComparer))
            builder[key] = value;

        return builder.ConnectionString;
    }

    public override string ToString() => ToConnectionString();

    public bool Equals(CredentialEntryTitle? other)
    {
        if (ReferenceEquals(this, other))
            return true;
        if (other is null)
            return false;
        if (Pairs.Count != other.Pairs.Count)
            return false;

        foreach (var (key, value) in Pairs)
        {
            if (!other.Pairs.TryGetValue(key, out var otherValue))
                return false;
            if (!string.Equals(value, otherValue, StringComparison.Ordinal))
                return false;
        }

        return true;
    }

    public override bool Equals(object? obj) =>
        obj is CredentialEntryTitle credentialTitle && Equals(credentialTitle);

    public override int GetHashCode() => _hashCode;

    public static bool operator ==(CredentialEntryTitle? left, CredentialEntryTitle? right) =>
        left?.Equals(right) ?? right is null;

    public static bool operator !=(CredentialEntryTitle? left, CredentialEntryTitle? right) =>
        !(left == right);

    private static int ComputeHashCode(IReadOnlyDictionary<string, string> pairs)
    {
        var hashCode = new HashCode();

        foreach (var keyValuePair in pairs.OrderBy(p => p.Key, _keyComparer))
        {
            hashCode.Add(keyValuePair.Key, StringComparer.OrdinalIgnoreCase);
            hashCode.Add(keyValuePair.Value, StringComparer.Ordinal);
        }

        return hashCode.ToHashCode();
    }

    public class CredentialTitleBuilder
    {
        private readonly Dictionary<string, string> _pairs = new(_keyComparer);

        public CredentialTitleBuilder Add(string key, string value)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Key cannot be null/empty/whitespace.", nameof(key));

            _pairs[key.Trim()] = value!;
            return this;
        }

        public CredentialTitleBuilder Add(KeyValuePair<string, string> pair) =>
            Add(pair.Key, pair.Value);

        public CredentialTitleBuilder AddRange(IEnumerable<KeyValuePair<string, string>> pairs)
        {
            foreach (var kvp in pairs)
            {
                Add(kvp.Key, kvp.Value);
            }
            return this;
        }

        public CredentialEntryTitle Build() => new(_pairs);
    }
}