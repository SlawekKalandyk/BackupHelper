using BackupHelper.ConfigEditor.ConsoleApp.ConsoleCommands.Base;

namespace BackupHelper.ConfigEditor.ConsoleApp.ConsoleCommands
{
    internal class OutCommand : BaseCommand
    {
        // out without parameters is equal to out 1
        // out 2 moves 2 levels up etc.
        public override void Execute(IReadOnlyCollection<string> parameters, ref TreeNode currentNode)
        {
            var levelCount = 1;

            if (parameters.Count > 1)
            {
                Console.WriteLine("Too many parameters.");
                return;
            }

            if (parameters.Count == 1 && !int.TryParse(parameters.First(), out levelCount))
            {
                Console.WriteLine("Invalid level count.");
                return;
            }

            if (levelCount < 0)
            {
                Console.WriteLine("Level count cannot be negative.");
                return;
            }

            while (currentNode.Parent != null && levelCount > 0)
            {
                currentNode = currentNode.Parent;
                levelCount--;
            }
        }
    }
}
