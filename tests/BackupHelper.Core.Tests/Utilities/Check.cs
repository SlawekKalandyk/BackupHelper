using System.Runtime.CompilerServices;

namespace BackupHelper.Core.Tests;

public static class Check
{
    public static void IsNull(object? obj, [CallerArgumentExpression("obj")] string? name = null)
    {
        if (obj == null)
            throw new ArgumentNullException($"'{name}' cannot be null");
    }

    public static void IsNullOrEmpty(string? value, [CallerArgumentExpression("value")] string? name = null)
    {
        if (string.IsNullOrEmpty(value))
            throw new ArgumentNullException($"'{name}' cannot be null or empty");
    }

    public static void IsGreaterThanZero(int value, [CallerArgumentExpression("value")] string? name = null)
    {
        if (value <= 0)
            throw new ArgumentOutOfRangeException($"'{name}' has to be greater than 0");
    }
}