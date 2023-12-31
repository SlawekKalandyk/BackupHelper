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

                var parameters = lastInput.Split(' ');
                commandHandler.Handle(parameters, ref currentNode);
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
            return BackupConfiguration.FromJson(json);
        }
    }
}
