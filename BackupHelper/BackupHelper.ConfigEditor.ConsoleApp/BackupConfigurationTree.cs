using BackupHelper.Core.FileZipping;

namespace BackupHelper.ConfigEditor.ConsoleApp
{
    internal class BackupConfigurationTree : TreeNode<BackupConfiguration>
    {
        private BackupConfigurationTree(BackupConfiguration value) : base(value)
        {
        }

        public string OriginalFilePath { get; init; }
        public IReadOnlyCollection<BackupDirectoryNode> Directories => Children.OfType<BackupDirectoryNode>().ToList();
        public IReadOnlyCollection<BackupFileNode> Files => Children.OfType<BackupFileNode>().ToList();

        public static BackupConfigurationTree FromBackupConfiguration(BackupConfiguration backupConfiguration, string filePath)
        {
            var root = new BackupConfigurationTree(backupConfiguration)
            {
                OriginalFilePath = filePath
            };

            foreach (var directory in backupConfiguration.Directories)
            {
                var directoryNode = BackupDirectoryNode.FromBackupDirectory(directory);
                root.AddChildWithoutModifyingInnards(directoryNode);
            }

            foreach (var file in backupConfiguration.Files)
            {
                var fileNode = new BackupFileNode(file);
                root.AddChildWithoutModifyingInnards(fileNode);
            }

            return root;
        }

        public override void AddChild(TreeNode child)
        {
            base.AddChild(child);

            if (child is BackupDirectoryNode directoryNode)
            {
                Value.Directories.Add(directoryNode.Value);
            }
            else if (child is BackupFileNode fileNode)
            {
                Value.Files.Add(fileNode.Value);
            }
            else
            {
                throw new InvalidOperationException("Unknown node type.");
            }
        }

        public override void RemoveChild(TreeNode child)
        {
            base.RemoveChild(child);

            if (child is BackupDirectoryNode directoryNode)
            {
                Value.Directories.Remove(directoryNode.Value);
            }
            else if (child is BackupFileNode fileNode)
            {
                Value.Files.Remove(fileNode.Value);
            }
            else
            {
                throw new InvalidOperationException("Unknown node type.");
            }
        }

        private void AddChildWithoutModifyingInnards(TreeNode child)
        {
            base.AddChild(child);
        }
    }

    internal class BackupDirectoryNode : TreeNode<BackupDirectory>
    {
        public BackupDirectoryNode(BackupDirectory value) : base(value)
        {
        }

        public IReadOnlyCollection<BackupDirectoryNode> Directories => Children.OfType<BackupDirectoryNode>().ToList();
        public IReadOnlyCollection<BackupFileNode> Files => Children.OfType<BackupFileNode>().ToList();

        public static BackupDirectoryNode FromBackupDirectory(BackupDirectory directory)
        {
            var node = new BackupDirectoryNode(directory);

            foreach (var subDirectory in directory.Directories)
            {
                var directoryNode = FromBackupDirectory(subDirectory);
                node.AddChildWithoutModifyingInnards(directoryNode);
            }

            foreach (var file in directory.Files)
            {
                var fileNode = new BackupFileNode(file);
                node.AddChildWithoutModifyingInnards(fileNode);
            }

            return node;
        }

        public override void AddChild(TreeNode child)
        {
            base.AddChild(child);

            if (child is BackupDirectoryNode directoryNode)
            {
                Value.Directories.Add(directoryNode.Value);
            }
            else if (child is BackupFileNode fileNode)
            {
                Value.Files.Add(fileNode.Value);
            }
            else
            {
                throw new InvalidOperationException("Unknown node type.");
            }
        }

        public override void RemoveChild(TreeNode child)
        {
            base.RemoveChild(child);

            if (child is BackupDirectoryNode directoryNode)
            {
                Value.Directories.Remove(directoryNode.Value);
            }
            else if (child is BackupFileNode fileNode)
            {
                Value.Files.Remove(fileNode.Value);
            }
            else
            {
                throw new InvalidOperationException("Unknown node type.");
            }
        }

        private void AddChildWithoutModifyingInnards(TreeNode child)
        {
            base.AddChild(child);
        }
    }

    internal class BackupFileNode : TreeNode<BackupFile>
    {
        public BackupFileNode(BackupFile value) : base(value)
        {
        }

        public override IReadOnlyCollection<TreeNode> Children => null;
    }

}
