using System.Runtime.InteropServices;
using System.Security.Cryptography;
using BackupHelper.Abstractions.Credentials;
using Spectre.Console;

public static class SecureConsole
{
    public static SensitiveString PromptPassword(string message)
    {
        Span<char> buffer = stackalloc char[512];
        var length = Prompt(message, buffer);
        var password = buffer[..length];
        var sensitivePassword = new SensitiveString(password);
        CryptographicOperations.ZeroMemory(MemoryMarshal.AsBytes(buffer));
        return sensitivePassword;
    }

    private static int ReadPassword(
        Span<char> buffer,
        char mask = '*',
        int maxLength = int.MaxValue
    )
    {
        int length = 0;

        while (true)
        {
            var key = Console.ReadKey(intercept: true);

            // ENTER
            if (key.Key == ConsoleKey.Enter)
            {
                Console.WriteLine();
                break;
            }

            // BACKSPACE
            if (key.Key == ConsoleKey.Backspace && key.Modifiers == 0)
            {
                if (length > 0)
                {
                    length--;
                    Console.Write("\b \b");
                }
                continue;
            }

            // CTRL+BACKSPACE (delete previous word)
            if (key.Key == ConsoleKey.Backspace && key.Modifiers.HasFlag(ConsoleModifiers.Control))
            {
                while (length > 0 && buffer[length - 1] == ' ')
                {
                    length--;
                    Console.Write("\b \b");
                }

                while (length > 0 && buffer[length - 1] != ' ')
                {
                    length--;
                    Console.Write("\b \b");
                }

                continue;
            }

            // ignore control chars
            if (char.IsControl(key.KeyChar))
                continue;

            if (length >= buffer.Length || length >= maxLength)
                continue;

            buffer[length++] = key.KeyChar;
            Console.Write(mask);
        }

        return length;
    }

    private static int Prompt(string message, Span<char> buffer)
    {
        AnsiConsole.Markup($"{message} ");

        return ReadPassword(buffer);
    }
}