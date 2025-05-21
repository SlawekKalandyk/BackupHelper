using System.Runtime.CompilerServices;

namespace BackupHelper.Tests;

public static class Check
{
    public static void IsNull(object? obj, [CallerArgumentExpression("obj")] string? name = null)
    {
        switch (obj)
        {
            case string stringObj when string.IsNullOrEmpty(stringObj):
                throw new ArgumentNullException($"'{name}' cannot be null or empty");
            case null:
                throw new ArgumentNullException($"'{name}' cannot be null");
        }
    }

    public static void IsGreaterThanZero(int value, [CallerArgumentExpression("value")] string? name = null)
    {
        if (value <= 0)
            throw new ArgumentOutOfRangeException($"'{name}' has to be greater than 0");
    }
}