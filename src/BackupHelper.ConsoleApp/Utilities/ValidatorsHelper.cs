using Spectre.Console;

namespace BackupHelper.ConsoleApp.Utilities;

public static class ValidatorsHelper
{
    public static ValidationResult FileExists(string value)
    {
        return File.Exists(value)
            ? ValidationResult.Success()
            : ValidationResult.Error("File does not exist.");
    }

    public static ValidationResult DirectoryExists(string value)
    {
        return Directory.Exists(value)
            ? ValidationResult.Success()
            : ValidationResult.Error("Directory does not exist.");
    }

    public static ValidationResult DirectoryExistsIfNotEmpty(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return ValidationResult.Success();
        }

        return DirectoryExists(value);
    }

    public static ValidationResult HasNoInvalidChars(string value)
    {
        var invalidChars = Path.GetInvalidPathChars();
        if (value.IndexOfAny(invalidChars) > -1)
        {
            return ValidationResult.Error("Path contains invalid characters.");
        }

        return ValidationResult.Success();
    }

    public static ValidationResult IPAddressOrHostname(string value)
    {
        if (
            System.Net.IPAddress.TryParse(value, out _)
            || Uri.CheckHostName(value) != UriHostNameType.Unknown
        )
        {
            return ValidationResult.Success();
        }

        return ValidationResult.Error("Value is not a valid IP address or hostname.");
    }
}