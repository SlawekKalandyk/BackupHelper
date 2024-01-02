using BackupHelper.ConfigEditor.ConsoleApp.ConsoleCommands.Base;

namespace BackupHelper.ConfigEditor.ConsoleApp.ConsoleCommands
{
    internal class ListCommand : BaseCommand
    {
        public override void Execute(IReadOnlyCollection<string> parameters, ref TreeNode currentNode)
        {
            if (parameters.Count > 0)
            {
                Console.WriteLine("Too many parameters.");
                return;
            }

            PrintNode(currentNode, 0, 0);
        }

        private void PrintNode(TreeNode node, int level, int index)
        {
            var indent = new string(' ', level * 2);
            if (node is BackupFileNode fileNode)
            {
                var text = fileNode.Value.FilePath;
                Console.WriteLine($"{indent}{index}. {text}");
            }
            else if (node is BackupDirectoryNode directoryNode)
            {
                var text = directoryNode.Value.Name;
                Console.WriteLine($"{indent}{index}. {text}");
                var childIndex = 0;
                foreach (var directoryNodeChild in directoryNode.Children)
                {
                    PrintNode(directoryNodeChild, level + 1, childIndex);
                    childIndex++;
                }
            }
            else if (node is BackupConfigurationTree configurationTree)
            {
                var childIndex = 0;
                foreach (var configurationTreeChild in configurationTree.Children)
                {
                    PrintNode(configurationTreeChild, level + 1, childIndex);
                    childIndex++;
                }
            }
            else
            {
                throw new InvalidOperationException("Unknown node type.");
            }
        }
    }
}
