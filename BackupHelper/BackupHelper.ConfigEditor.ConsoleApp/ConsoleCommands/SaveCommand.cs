using BackupHelper.ConfigEditor.ConsoleApp.ConsoleCommands.Base;

namespace BackupHelper.ConfigEditor.ConsoleApp.ConsoleCommands
{
    internal class SaveCommand : BaseCommand
    {
        public override void Execute(IReadOnlyCollection<string> parameters, ref TreeNode currentNode)
        {
            if (parameters.Count > 1)
            {
                Console.WriteLine("Too many parameters.");
                return;
            }

            var originalNode = currentNode;
            var rootCommand = new RootCommand();
            rootCommand.Execute(Array.Empty<string>(), ref currentNode);

            var configurationTree = currentNode as BackupConfigurationTree;

            if (parameters.Count == 1)
            {
                var filePath = parameters.First();
                if (File.Exists(filePath))
                {
                    Console.WriteLine($"File {filePath} already exists. Overwrite? (y/n)");

                    var key = Console.ReadKey(true);

                    if (key.Key == ConsoleKey.Y)
                    {
                        SaveFile(filePath, configurationTree, true);
                    }
                }
                else
                {
                    SaveFile(filePath, configurationTree, true);
                }
            }
            else
            {
                if (string.IsNullOrEmpty(configurationTree.SaveFilePath))
                {
                    Console.WriteLine("No save file path specified.");
                    return;
                }

                SaveFile(configurationTree.SaveFilePath, configurationTree, false);
            }

            currentNode = originalNode;
        }

        private void SaveFile(string filePath, BackupConfigurationTree configurationTree, bool setBackupConfigOriginalFilePath)
        {
            File.WriteAllText(filePath, configurationTree.Value.ToJson());
            Console.WriteLine($"Saved backup config to {filePath}.");
            if (setBackupConfigOriginalFilePath && string.IsNullOrEmpty(configurationTree.SaveFilePath))
            {
                configurationTree.SaveFilePath = filePath;
                Console.WriteLine($"Set backup config save file path to {filePath}.");
            }
        }
    }
}
