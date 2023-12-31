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
                    Console.WriteLine("File already exists. Overwrite? (y/n)");

                    var key = Console.ReadKey(true);

                    if (key.Key == ConsoleKey.Y)
                    {
                        File.WriteAllText(filePath, configurationTree.Value.ToJson());
                    }
                }
            }
            else
            {
                File.WriteAllText(configurationTree.OriginalFilePath, configurationTree.Value.ToJson());
            }
            
            currentNode = originalNode;
        }
    }
}
