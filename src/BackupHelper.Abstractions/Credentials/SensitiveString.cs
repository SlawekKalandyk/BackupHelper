using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

namespace BackupHelper.Abstractions.Credentials;

/// <summary>
/// Holds a sensitive string (password/secret) as a zeroing-capable byte array.
/// Call <see cref="Dispose"/> to zero the internal bytes when the secret is no longer needed.
/// </summary>
public sealed class SensitiveString : IDisposable, IEquatable<SensitiveString>
{
    private readonly byte[] _utf8Bytes;
    private bool _disposed;

    /// <remarks>
    /// WARNING: The source <paramref name="value"/> string cannot be zeroed from managed code.
    /// Prefer the <see cref="SensitiveString(char[])"/> overload when the caller holds a mutable
    /// char array, so the source is zeroed immediately after encoding.
    /// </remarks>
    public SensitiveString(string value)
    {
        _utf8Bytes = Encoding.UTF8.GetBytes(value);
    }

    /// <summary>
    /// Accepts a mutable char array, encodes it to UTF-8 bytes, then immediately zeroes the
    /// source array so the plaintext does not linger in memory. Prefer this overload when
    /// the caller can provide a <c>char[]</c> (e.g. via <c>string.ToCharArray()</c>).
    /// </summary>
    public SensitiveString(char[] chars)
    {
        _utf8Bytes = Encoding.UTF8.GetBytes(chars);
        CryptographicOperations.ZeroMemory(MemoryMarshal.AsBytes(chars.AsSpan()));
    }

    /// <summary>
    /// Creates a new <see cref="SensitiveString"/> from a UTF-8 byte array. The byte array is copied
    /// to ensure the original array can be zeroed without affecting the new instance.
    /// </summary>
    private SensitiveString(byte[] utf8Bytes)
    {
        _utf8Bytes = new byte[utf8Bytes.Length];
        Array.Copy(utf8Bytes, _utf8Bytes, utf8Bytes.Length);
    }

    /// <summary>
    /// Returns <c>true</c> when the secret is zero-length (no content).
    /// </summary>
    public bool IsEmpty
    {
        get
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            return _utf8Bytes.Length == 0;
        }
    }

    /// <summary>
    /// Returns a managed string reconstructed from the internal bytes for APIs that require
    /// a <see cref="string"/>. The returned string cannot itself be zeroed.
    /// </summary>
    public string Expose()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return Encoding.UTF8.GetString(_utf8Bytes);
    }

    /// <summary>
    /// Returns a read-only span of the internal UTF-8 bytes. This can be used for APIs that require
    /// a byte array without converting it to a managed string.
    /// </summary>
    public ReadOnlySpan<byte> ExposeUtf8Bytes()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return _utf8Bytes;
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            CryptographicOperations.ZeroMemory(_utf8Bytes);
            _disposed = true;
        }
    }

    /// <inheritdoc/>
    public bool Equals(SensitiveString? other)
    {
        if (other is null)
            return false;
        if (ReferenceEquals(this, other))
            return true;
        return _utf8Bytes.AsSpan().SequenceEqual(other._utf8Bytes.AsSpan());
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj) => Equals(obj as SensitiveString);

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        var hash = new HashCode();
        foreach (var b in _utf8Bytes)
            hash.Add(b);
        return hash.ToHashCode();
    }

    public SensitiveString Clone() => new(_utf8Bytes);
}