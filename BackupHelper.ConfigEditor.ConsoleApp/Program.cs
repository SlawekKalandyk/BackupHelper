using System.Text;
using BackupHelper.ConfigEditor.ConsoleApp.ConsoleCommands;
using BackupHelper.Core.FileZipping;

namespace BackupHelper.ConfigEditor.ConsoleApp
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var backupConfiguration = GetBackupConfiguration(args);
            if (backupConfiguration == null)
                return;

            var backupConfigurationTree = BackupConfigurationTree.FromBackupConfiguration(backupConfiguration, args.Length > 0 ? args[0] : null);
            TreeNode currentNode = backupConfigurationTree;
            string lastInput;
            var commandHandler = new CommandHandler();
            do
            {
                Console.Write(">");
                lastInput = Console.ReadLine();

                if (lastInput == "exit")
                    break;

                if (string.IsNullOrWhiteSpace(lastInput))
                {
                    Console.WriteLine("No command specified.");
                    continue;
                }

                try
                {
                    var parameters = SplitCommandInput(lastInput).ToArray();
                    commandHandler.Handle(parameters, ref currentNode);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
            } while (lastInput != "exit");
        }

        private static BackupConfiguration? GetBackupConfiguration(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("No configuration file specified. Creating new backup configuration tree.");
                return new BackupConfiguration();
            }
            var backupConfiguration = args[0];
            if (!File.Exists(backupConfiguration))
            {
                Console.WriteLine($"Configuration file '{backupConfiguration}' not found.");
                return null;
            }
            var json = File.ReadAllText(backupConfiguration);
            Console.WriteLine($"Loaded configuration file '{GetFullFilePath(backupConfiguration)}'.");
            return BackupConfiguration.FromJson(json);
        }

        private static string GetFullFilePath(string filePath)
        {
            var fileInfo = new FileInfo(filePath);
            return fileInfo.FullName;
        }

        private static IEnumerable<string> SplitCommandInput(string input)
        {
            if (input.Contains('\"') && input.Count(c => c == '"') % 2 != 0)
                throw new ArgumentException("Invalid input. Double quote marks are not closed properly.");

            var args = new List<string>();
            var currentArg = new StringBuilder();
            var insideDoubleQuoteMarks = false;
            foreach (var c in input)
            {
                if (c == '"')
                {
                    insideDoubleQuoteMarks = !insideDoubleQuoteMarks;
                    continue;
                }

                if (c == ' ' && !insideDoubleQuoteMarks)
                {
                    args.Add(currentArg.ToString());
                    currentArg.Clear();
                    continue;
                }

                currentArg.Append(c);
            }

            if (currentArg.Length > 0)
                args.Add(currentArg.ToString());

            return args;
        }
    }
}
