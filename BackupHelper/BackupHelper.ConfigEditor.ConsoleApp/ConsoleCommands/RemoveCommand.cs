using BackupHelper.ConfigEditor.ConsoleApp.ConsoleCommands.Base;

namespace BackupHelper.ConfigEditor.ConsoleApp.ConsoleCommands
{
    internal class RemoveCommand : BaseCommand
    {
        public override void Execute(IReadOnlyCollection<string> parameters, ref TreeNode currentNode)
        {
            if (parameters.Count > 1)
            {
                Console.WriteLine("Too many parameters.");
                return;
            }

            if (parameters.Count == 0)
            {
                Console.WriteLine("No index specified.");
                return;
            }

            var rawIndex = parameters.First();
            if (!int.TryParse(rawIndex, out var index))
            {
                Console.WriteLine($"Invalid index: {rawIndex}.");
                return;
            }

            if (index < 0 || index >= currentNode.Children.Count)
            {
                Console.WriteLine("Index out of range.");
                return;
            }

            var child = currentNode.Children.ElementAt(index);
            currentNode.RemoveChild(child);
        }
    }
}
