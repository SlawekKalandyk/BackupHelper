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
}