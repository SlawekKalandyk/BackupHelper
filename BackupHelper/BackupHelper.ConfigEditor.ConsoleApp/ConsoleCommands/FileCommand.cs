using BackupHelper.ConfigEditor.ConsoleApp.ConsoleCommands.Base;
using BackupHelper.Core.FileZipping;

namespace BackupHelper.ConfigEditor.ConsoleApp.ConsoleCommands
{
    internal class FileCommand : BaseCommand
    {
        public override void Execute(IReadOnlyCollection<string> parameters, ref TreeNode currentNode)
        {
            if (currentNode is BackupFileNode)
            {
                Console.WriteLine("Cannot save a file inside a file.");
                return;
            }

            if (parameters.Count == 0)
            {
                Console.WriteLine("No file (or directory) path specified.");
                return;
            }

            if (parameters.Count > 1)
            {
                Console.WriteLine("Too many parameters.");
                return;
            }

            var filePath = parameters.First();
            if (!File.Exists(filePath) && !Directory.Exists(filePath))
            {
                Console.WriteLine("File (or directory) does not exist.");
                return;
            }

            var backupFile = new BackupFile(filePath);
            var backupFileNode = new BackupFileNode(backupFile);
            currentNode.AddChild(backupFileNode);
        }
    }
}
