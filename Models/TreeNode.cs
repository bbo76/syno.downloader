namespace SynoDownloader.Models
{
    using GalaSoft.MvvmLight;
    using System.Collections.ObjectModel;
    using System.Threading.Tasks;

    public class TreeNode : ObservableObject
    {
        public TreeNode(string displayName, string path, TreeNode parent, bool isRoot = false, bool isExpanded = false)
        {
            DisplayName = displayName;
            Path = path;
            Childs = new ObservableCollection<TreeNode>();
            Parent = parent;
            IsRoot = isRoot;
            IsExpanded = isExpanded;
        }

        private string _displayName;
        public string DisplayName
        {
            get { return _displayName; }
            set { Set(ref _displayName, value); }
        }

        private string _path;
        public string Path
        {
            get { return _path; }
            set { Set(ref _path, value); }
        }

        private TreeNode _parent;
        public TreeNode Parent
        {
            get { return _parent; }
            set { Set(ref _parent, value); }
        }

        private ObservableCollection<TreeNode> _childs;
        public ObservableCollection<TreeNode> Childs
        {
            get { return _childs; }
            set { Set(ref _childs, value); }
        }

        private bool _isExpanded;
        public bool IsExpanded
        {
            get { return _isExpanded; }
            set { Set(ref _isExpanded, value); }
        }

        private bool _isSelected;
        public bool IsSelected
        {
            get { return _isSelected; }
            set { Set(ref _isSelected, value); }
        }

        public bool IsRoot { get; set; }

        public async Task Select()
        {
            var current = this;
            while (current.Parent != null)
            {
                current = current.Parent;
                current.IsSelected = false;
                current.IsExpanded = true;
            }

            await Task.Delay(50);
            IsSelected = true;
        }
    }
}
