using BackupHelper.ConfigEditor.ConsoleApp.ConsoleCommands.Base;

namespace BackupHelper.ConfigEditor.ConsoleApp.ConsoleCommands
{
    internal class InCommand : BaseCommand
    {
        public override void Execute(IReadOnlyCollection<string> parameters, ref TreeNode currentNode)
        {
            if (parameters.Count == 0)
            {
                Console.WriteLine("No directory specified.");
                return;
            }

            foreach (var parameter in parameters)
            {
                if (!int.TryParse(parameter, out var directoryIndex))
                {
                    Console.WriteLine($"Invalid directory index: {directoryIndex}.");
                    return;
                }

                if (directoryIndex < 0 || directoryIndex >= currentNode.Children.Count)
                {
                    Console.WriteLine("Directory index out of range.");
                    return;
                }

                var node = currentNode.Children.ElementAt(directoryIndex);

                if (node is BackupFileNode)
                {
                    Console.WriteLine("Cannot enter a file.");
                    return;
                }

                currentNode = node;
            }
        }
    }
}
