using BackupHelper.ConfigEditor.ConsoleApp.ConsoleCommands.Base;

namespace BackupHelper.ConfigEditor.ConsoleApp.ConsoleCommands
{
    internal class CommandHandler
    {
        private static readonly Dictionary<string, Type> _commands = new();

        static CommandHandler()
        {
            RegisterCommands();
        }

        public void Handle(string[] args, ref TreeNode currentNode)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("No command specified.");
                return;
            }

            var separateCommands = SeparateCommands(args);

            foreach (var commandWithParams in separateCommands)
            {
                var commandName = commandWithParams.ElementAt(0);
                if (!_commands.ContainsKey(commandName))
                {
                    Console.WriteLine($"Command '{commandName}' not found.");
                    return;
                }

                var commandType = _commands[commandName];
                var command = (ICommand)Activator.CreateInstance(commandType);
                command.Execute(commandWithParams.Skip(1).ToArray(), ref currentNode);
            }
        }   

        private static void RegisterCommand(string textCommand, Type commandType)
        {
            _commands.Add(textCommand, commandType);
        }

        private static void RegisterCommands()
        {
            RegisterCommand("dir", typeof(DirCommand));
            RegisterCommand("file", typeof(FileCommand));
            RegisterCommand("in", typeof(InCommand));
            RegisterCommand("out", typeof(OutCommand));
            RegisterCommand("list", typeof(ListCommand));
            RegisterCommand("root", typeof(RootCommand));
            RegisterCommand("save", typeof(SaveCommand));
            RegisterCommand("rm", typeof(RemoveCommand));
        }

        private IEnumerable<IEnumerable<string>> SeparateCommands(string[] args)
        {
            var commands = new List<List<string>>();
            var currentCommand = new List<string>();
            foreach (var arg in args)
            {
                if (arg == "&")
                {
                    commands.Add(currentCommand);
                    currentCommand = new List<string>();
                    continue;
                }

                currentCommand.Add(arg);
            }

            commands.Add(currentCommand);
            return commands;
        }
    }
}
