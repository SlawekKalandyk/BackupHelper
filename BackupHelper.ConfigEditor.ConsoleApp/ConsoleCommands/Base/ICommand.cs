namespace BackupHelper.ConfigEditor.ConsoleApp.ConsoleCommands.Base
{
    internal interface ICommand
    {
        void Execute(IReadOnlyCollection<string> parameters, ref TreeNode currentNode);
    }
}
