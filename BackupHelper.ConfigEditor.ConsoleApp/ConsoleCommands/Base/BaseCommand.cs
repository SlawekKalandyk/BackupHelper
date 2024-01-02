namespace BackupHelper.ConfigEditor.ConsoleApp.ConsoleCommands.Base
{
    internal abstract class BaseCommand : ICommand
    {
        public abstract void Execute(IReadOnlyCollection<string> parameters, ref TreeNode currentNode);
    }
}
