using BackupHelper.ConfigEditor.ConsoleApp.ConsoleCommands.Base;
using BackupHelper.Core.FileZipping;

namespace BackupHelper.ConfigEditor.ConsoleApp.ConsoleCommands
{
    internal class DirCommand : BaseCommand
    {
        public override void Execute(IReadOnlyCollection<string> parameters, ref TreeNode currentNode)
        {
            if (currentNode is BackupFileNode)
            {
                Console.WriteLine("Cannot save a directory inside a file.");
                return;
            }

            if (parameters.Count == 0)
            {
                Console.WriteLine("No directory name specified.");
                return;
            }

            if (parameters.Count > 1)
            {
                Console.WriteLine("Too many parameters.");
                return;
            }

            var name = parameters.First();

            if (currentNode is BackupDirectoryNode directoryNode && directoryNode.Directories.Any(dir => dir.Value.Name == name)
                || currentNode is BackupConfigurationTree configurationTree && configurationTree.Directories.Any(dir => dir.Value.Name == name))
            {
                Console.WriteLine($"Directory {name} already exists.");
                return;
            }

            var newDirectory = new BackupDirectory(name);
            var newDirectoryNode = new BackupDirectoryNode(newDirectory);
            currentNode.AddChild(newDirectoryNode);
        }
    }
}
