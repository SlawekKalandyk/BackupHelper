using System.ComponentModel.DataAnnotations;

namespace BackupHelper.ConsoleApp.Utilities;

public static class ValidatorsHelper
{
    public static ValidationResult? FileExists(object? value)
    {
        if (value is string strValue && File.Exists(strValue))
        {
            return ValidationResult.Success;
        }

        return new ValidationResult("File does not exist.");
    }

    public static ValidationResult? DirectoryExists(object? value)
    {
        if (value is string strValue && Directory.Exists(strValue))
        {
            return ValidationResult.Success;
        }

        return new ValidationResult("Directory does not exist.");
    }

    public static ValidationResult? DirectoryExistsIfNotEmpty(object? value)
    {
        if (value is string strValue && string.IsNullOrWhiteSpace(strValue))
        {
            return ValidationResult.Success;
        }

        return DirectoryExists(value);
    }

    public static ValidationResult? HasNoInvalidChars(object? value)
    {
        if (value is string strValue)
        {
            var invalidChars = Path.GetInvalidPathChars();
            if (strValue.IndexOfAny(invalidChars) > -1)
            {
                return new ValidationResult("Path contains invalid characters.");
            }
        }

        return ValidationResult.Success;
    }

    public static ValidationResult? IPAddressOrHostname(object? value)
    {
        if (value is string strValue)
        {
            if (
                System.Net.IPAddress.TryParse(strValue, out _)
                || Uri.CheckHostName(strValue) != UriHostNameType.Unknown
            )
            {
                return ValidationResult.Success;
            }
        }

        return new ValidationResult("Value is not a valid IP address or hostname.");
    }
}