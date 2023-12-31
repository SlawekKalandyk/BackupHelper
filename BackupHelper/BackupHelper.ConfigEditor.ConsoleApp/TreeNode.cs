namespace BackupHelper.ConfigEditor.ConsoleApp
{
    internal abstract class TreeNode
    {
        private readonly List<TreeNode> _children = new();

        protected TreeNode(object value)
        {
            Value = value;
        }

        public virtual object Value { get; }
        public virtual IReadOnlyCollection<TreeNode> Children => _children;
        public virtual TreeNode? Parent { get; private set; }

        public virtual void AddChild(TreeNode child)
        {
            child.Parent = this;
            _children.Add(child);
        }

        public virtual void RemoveChild(TreeNode child)
        {
            child.Parent = null;
            _children.Remove(child);
        }
    }

    internal abstract class TreeNode<T> : TreeNode
    {
        protected TreeNode(T value) : base(value)
        {
        }

        public new virtual T Value => (T)base.Value;
    }
}
