using BackupHelper.ConfigEditor.ConsoleApp.ConsoleCommands.Base;

namespace BackupHelper.ConfigEditor.ConsoleApp.ConsoleCommands
{
    internal class RootCommand : BaseCommand
    {
        public override void Execute(IReadOnlyCollection<string> parameters, ref TreeNode currentNode)
        {
            if (parameters.Count > 0)
            {
                Console.WriteLine("Too many parameters.");
                return;
            }

            while (currentNode.Parent != null)
            {
                currentNode = currentNode.Parent;
            }
        }
    }
}
