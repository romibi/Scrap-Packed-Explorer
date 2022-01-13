using ch.romibi.Scrap.Packed.PackerLib;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using ch.romibi.Scrap.Packed.PackerLib.DataTypes;
using System.IO;
using System.Diagnostics;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Collections;
using Microsoft.WindowsAPICodePack.Dialogs;

namespace ch.romibi.Scrap.Packed.Explorer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// 

    // todo: lots of cleanup and refactoring
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private ScrapPackedFile _loadedPackedFile;
        protected ScrapPackedFile LoadedPackedFile { 
            get { return _loadedPackedFile; } 
            set { 
                _loadedPackedFile = value;
                ArchiveLoaded = !(value is null);
            }
        }

        private bool _PendingChanges;
        public bool PendingChanges {
            get { return _PendingChanges; }
            set {
                _PendingChanges = value;
                NotifyPropertyChanged();
            }
        }

        private bool _ArchiveLoaded;
        public bool ArchiveLoaded {
            get { return _ArchiveLoaded; }
            set { 
                _ArchiveLoaded = value;
                NotifyPropertyChanged(); 
            }
        }

        // todo: make this also true if FileTree root is selected
        private bool _ContentSelected;
        public bool ContentSelected {
            get { return _ContentSelected; }
            set {
                _ContentSelected = value;
                NotifyPropertyChanged();
            }
        }

        private TreeEntry CurrentFolder = null;

        private bool _FileTreeSelectionUpdating = false;

        protected readonly string NAVIGATE_UP_NAME = "..";
        protected readonly string PACKED_FILTER_STRING = "Scrapland Archive|*.packed|All Files|*.*";

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public MainWindow()
        {
            PendingChanges = false;
            InitializeComponent();
        }

        private void RefreshTreeView()
        {

            TreeEntry root = new TreeEntry(null) { Name = LoadedPackedFile.fileName };

            foreach (PackedFileIndexData file in LoadedPackedFile.GetFileIndexDataList())
            {
                root.AddFileData(file);
            }
            root.Sort();
            FileTree.Items.Clear();
            FileTree.Items.Add(root);

            TreeContent.Items.Clear();
            foreach (var item in root.Items)
            {
                TreeContent.Items.Add(item);
            }

            CurrentFolder = root;
        }

        private void TreeContent_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var clickedItem = ((FrameworkElement)e.OriginalSource).DataContext as TreeEntry;
            TreeContent_LoadTreeEntry(clickedItem);
            var containerItem = clickedItem.GetContainerFromTree(FileTree);
            if (!(containerItem is null)) containerItem.IsExpanded = true;
        }

        private void FileTree_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            var selectedEntry = e.NewValue as TreeEntry;

            if (selectedEntry is null) return;
            if (_FileTreeSelectionUpdating) return;

            if (selectedEntry.IsDirectory)
            {
                TreeContent_LoadTreeEntry(selectedEntry);
            }
            else
            {
                TreeContent_LoadTreeEntry(selectedEntry.Parent);
                TreeContent.SelectedItem = selectedEntry;
            }
        }

        private void TreeContent_LoadTreeEntry(TreeEntry loaditem)
        {
            if (loaditem != null)
            {
                if (!loaditem.IsDirectory)
                    return;

                TreeContent.Items.Clear();
                if (!(loaditem.Parent is null))
                {
                    var navigateUpItem = new TreeEntryAlias(loaditem.Parent) { Name = NAVIGATE_UP_NAME };
                    TreeContent.Items.Add(navigateUpItem);
                }
                foreach (var item in loaditem.Items)
                {
                    TreeContent.Items.Add(item);
                }

                CurrentFolder = loaditem;
            }
        }

        private void TreeContent_ReloadCurrentFolder()
        {
            TreeContent_LoadTreeEntry(CurrentFolder);
        }

        private void TreeContent_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ContentSelected = (TreeContent.SelectedItems.Count != 0);

            if (e.AddedItems.Count == 0) return;

            TreeEntry selectedItem = e.AddedItems[0] as TreeEntry;

            if (selectedItem is TreeEntryAlias /* && selectedItem.Name.Equals(NAVIGATE_UP_NAME) */)
            {
                (TreeContent.ItemContainerGenerator.ContainerFromItem(selectedItem) as ListViewItem).IsSelected = false;
                return;
            }

            List<TreeEntry> itemPath = selectedItem.GetItemPath();

            // Update Tree Selection
            _FileTreeSelectionUpdating = true;
            try
            {
                ItemsControl currentLevel = FileTree as ItemsControl;
                // foreach folder (pathLevel) expand the correct subfolder
                foreach (TreeEntry pathLevel in itemPath)
                {
                    TreeViewItem nextLevel = null;
                    bool doUpdateLayout = false;
                    // Check each subfolder if it is the wanted one and expand/collapse accordingly
                    foreach (TreeEntry treeItem in currentLevel.Items)
                    {
                        TreeViewItem treeItemControl = currentLevel.ItemContainerGenerator.ContainerFromItem(treeItem) as TreeViewItem;
                        if (treeItemControl is null) break;

                        var isNextPathLevel = treeItem.Equals(pathLevel);
                        // update IsExpanded and note if we need to update currentLevel layout
                        if (!doUpdateLayout && treeItemControl.IsExpanded != isNextPathLevel) doUpdateLayout = true;
                        treeItemControl.IsExpanded = isNextPathLevel;
                        if (isNextPathLevel) nextLevel = treeItemControl;
                    }
                    if (doUpdateLayout) currentLevel.UpdateLayout();

                    if (nextLevel is null) break;

                    currentLevel = nextLevel;
                }
                TreeContent_SelectTreeViewItem(currentLevel as TreeViewItem);
            }
            finally
            {
                _FileTreeSelectionUpdating = false;
            }
        }

        private void TreeContent_SelectTreeViewItem(TreeViewItem p_SelectedTreeItem)
        {
            p_SelectedTreeItem.IsSelected = true;
            if (p_SelectedTreeItem.Items.Count != 0)
            {
                p_SelectedTreeItem.IsExpanded = false;
                p_SelectedTreeItem.UpdateLayout();
            }
            p_SelectedTreeItem.BringIntoView();
        }

        private void OpenButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = PACKED_FILTER_STRING;

            if (openFileDialog.ShowDialog() == true)
            {
                LoadedPackedFile = new ScrapPackedFile(openFileDialog.FileName);
                PendingChanges = false;
                RefreshTreeView();
            }
        }

        private void ExtractToButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedItems = TreeContent.SelectedItems;

            if (selectedItems.Count == 0) return; // todo make button not clickable in that case

            if (selectedItems.Count > 1)
            {
                ExtractToFolder(selectedItems);
            }
            else if ((selectedItems[0] as TreeEntry).IsDirectory)
            {
                ExtractToFolder(selectedItems);
            }
            else
            {
                ExtractToFile(selectedItems[0] as TreeEntry);
            }
        }

        private void ExtractToFile(TreeEntry treeEntry)
        {
            string packedPath = treeEntry.IndexData.FilePath;
            string defaultFilename = packedPath.Split('/').Last();

            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.FileName = defaultFilename;
            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    LoadedPackedFile.Extract(treeEntry.IndexData.FilePath, saveFileDialog.FileName);
                }
                catch (Exception ex)
                {
                    Error(ex);
                }
            }
        }

        private void ExtractToFolder(IList selectedItems)
        {
            // todo: CommonOpenFileDialog is from WindowsAPICodePack-Shell: analyse Nuget warnings
            CommonOpenFileDialog folderDialog = new CommonOpenFileDialog();
            folderDialog.IsFolderPicker = true;
            if (folderDialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                foreach (var item in selectedItems)
                {
                    Debug.Assert(item is TreeEntry); // should always be the case
                    TreeEntry entry = item as TreeEntry;
                    try
                    {
                        LoadedPackedFile.Extract(entry.GetItemPathString(), System.IO.Path.Combine(folderDialog.FileName, entry.Name));
                    }
                    catch (Exception ex)
                    {
                        Error(ex);
                    }
                }
            }
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openDialog = new OpenFileDialog();

            if (openDialog.ShowDialog() == true)
            {
                try
                {
                    string packedPathDir = CurrentFolder.GetItemPathString();
                    if (packedPathDir == "/") packedPathDir = "";

                    var packedPath = packedPathDir + System.IO.Path.GetFileName(openDialog.FileName);

                    LoadedPackedFile.Add(openDialog.FileName, packedPath);
                    (FileTree.Items[0] as TreeEntry).AddFileData(LoadedPackedFile.GetFileIndexDataForFile(packedPath));
                    TreeContent_ReloadCurrentFolder();
                } catch (Exception ex)
                {
                    Error(ex);
                }
                PendingChanges = true;
            }
        }

        private void AddFolderButton_Click(object sender, RoutedEventArgs e)
        {
            // todo: CommonOpenFileDialog is from WindowsAPICodePack-Shell: analyse Nuget warnings
            CommonOpenFileDialog openDialog = new CommonOpenFileDialog();
            openDialog.IsFolderPicker = true;

            if (openDialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                try
                {
                    string packedPathDir = CurrentFolder.GetItemPathString();
                    if (packedPathDir == "/") packedPathDir = "";

                    var packedPath = packedPathDir + System.IO.Path.GetFileName(openDialog.FileName);

                    LoadedPackedFile.Add(openDialog.FileName, packedPath);
                    //(FileTree.Items[0] as TreeEntry).AddFileData(LoadedPackedFile.GetFileIndexDataForFile(packedPath));
                    //TreeContent_ReloadCurrentFolder();
                    RefreshTreeView(); // todo: make this not need to refresh whole tree
                }
                catch (Exception ex)
                {
                    Error(ex);
                }
                PendingChanges = true;
            }
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            List<TreeEntry> selectedItems = new List<TreeEntry>();
            foreach (var item in TreeContent.SelectedItems)
                selectedItems.Add(item as TreeEntry);

            if (selectedItems.Count == 0) return;

            if (MessageBox.Show($"Do you really want delete {selectedItems.Count} elements?",
                    "Delete?",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question, MessageBoxResult.No) != MessageBoxResult.Yes) return;

            try
            {
                foreach (var item in selectedItems)
                {
                    LoadedPackedFile.Remove(item.GetItemPathString());
                    item.Parent.Items.Remove(item);
                }
            }
            catch (Exception ex)
            {
                Error(ex);
            }
            PendingChanges = true;

        }

        private void CreateButton_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveDialog = new SaveFileDialog();
            saveDialog.Filter = PACKED_FILTER_STRING;
            saveDialog.DefaultExt = ".packed";
            saveDialog.AddExtension = false; // todo: ok? or true?

            if (saveDialog.ShowDialog() == true)
            {
                if (File.Exists(saveDialog.FileName))
                {
                    MessageBox.Show($"File {saveDialog.FileName} already exists. Cannot create new Archive with that name!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                else
                {
                    LoadedPackedFile = new ScrapPackedFile(saveDialog.FileName);
                    PendingChanges = true;
                    RefreshTreeView();
                }
            }
        }

        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            SaveWithOverwriteWarning(LoadedPackedFile.fileName);
        }

        private void SaveAsButton_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveDialog = new SaveFileDialog();
            saveDialog.Filter = PACKED_FILTER_STRING;
            saveDialog.DefaultExt = ".packed";
            saveDialog.AddExtension = false; // todo: ok? or true?

            if (saveDialog.ShowDialog() == true)
            {
                SaveWithOverwriteWarning(saveDialog.FileName);
            }
        }

        private void SaveWithOverwriteWarning(string p_NewFileName)
        {
            if (File.Exists(p_NewFileName))
                if (MessageBox.Show($"Do you really want to overwrite {p_NewFileName}?",
                    "Overwrite?",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question, MessageBoxResult.No) != MessageBoxResult.Yes) return;

            try
            {

                LoadedPackedFile.SaveToFile(p_NewFileName);
                PendingChanges = false;
            }
            catch (Exception ex)
            {
                Error(ex);
            }
        }

        private void Error(Exception ex)
        {
            MessageBox.Show(
                ex.Message,
                "Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error
                );
        }

        private void OptionsButton_Click(object sender, RoutedEventArgs e)
        {

        }
    }

    public class TreeEntryAlias : TreeEntry
    {
        public TreeEntryAlias(TreeEntry p_Reference) : base(p_Reference.Parent)
        {
            ReferencedEntry = p_Reference;
            IndexData = p_Reference.IndexData;
            Items = p_Reference.Items;
        }

        public TreeEntry ReferencedEntry { get; set; }
    }

    public class TreeEntry : IComparable
    {
        public TreeEntry(TreeEntry p_Parent)
        {
            Items = new ObservableCollection<TreeEntry>();
            IndexData = null;
            Parent = p_Parent;
        }

        public string Name { get; set; }

        public TreeEntry Parent { get; set; }

        public PackedFileIndexData IndexData { get; set; }
        public bool IsFile {
            get {
                return !IsDirectory;
            }
        }

        public bool IsDirectory {
            get {
                return IndexData is null;
            }
        }

        public void AddFileData(PackedFileIndexData p_File, string p_SubdirFilename = "")
        {
            string fileName;
            if (p_SubdirFilename.Length == 0)
            {
                fileName = p_File.FilePath;
            }
            else
            {
                fileName = p_SubdirFilename;
            }

            if (fileName.Contains("/"))
            {
                var nextDir = fileName.Split("/")[0];
                TreeEntry subDir = null;
                foreach (TreeEntry entry in Items)
                {
                    if (entry.Name.Equals(nextDir))
                    {
                        subDir = entry;
                        break;
                    }
                }
                if (subDir == null)
                {
                    subDir = new TreeEntry(this) { Name = nextDir };
                    Items.Add(subDir);
                }
                subDir.AddFileData(p_File, fileName.Substring(nextDir.Length + 1));
            }
            else
            {
                Items.Add(new TreeEntry(this) { Name = fileName, IndexData = p_File });
            }
        }

        public List<TreeEntry> GetItemPath()
        {
            // get a list of TreeItems from parent to selection
            // Todo: move logic inside TreeEntry and cache
            List<TreeEntry> itemPath = new List<TreeEntry>();

            var currentItem = this;
            itemPath.Add(currentItem);
            while (!(currentItem.Parent is null))
            {
                currentItem = currentItem.Parent;
                itemPath.Insert(0, currentItem);
            }

            return itemPath;
        }

        public string GetItemPathString(bool p_IgnoreRoot = true)
        {
            List<TreeEntry> itemPath = GetItemPath();
            string pathString = "";

            for (var i = 0; i < itemPath.Count; i++)
            {
                if (p_IgnoreRoot && i == 0) continue;

                if (pathString.Length > 0)
                    pathString += "/";
                pathString += itemPath[i].Name;
            }

            if (itemPath.Last<TreeEntry>().IsDirectory)
                pathString += "/";

            return pathString;
        }

        public TreeViewItem GetContainerFromTree(TreeView p_TreeRoot)
        {
            var itemPath = GetItemPath();
            ItemsControl containerLevel = p_TreeRoot as ItemsControl;
            foreach (var pathLevel in itemPath)
            {
                foreach (var containerItem in containerLevel.Items)
                {
                    if (containerItem.Equals(pathLevel))
                    {
                        containerLevel = containerLevel.ItemContainerGenerator.ContainerFromItem(containerItem) as ItemsControl;
                        break;
                    }
                }
            }
            return containerLevel as TreeViewItem;
        }

        public int CompareTo(object o)
        {
            TreeEntry a = this;
            TreeEntry b = (TreeEntry)o;
            if (a.IsDirectory == b.IsDirectory)
            {
                return a.Name.CompareTo(b.Name);
            }
            else
            {
                if (a.IsDirectory)
                    return -1;
                else
                    return 1;
            }

        }

        public void Sort()
        {
            var sorted = Items.OrderBy(x => x).ToList();

            for (int i = 0; i < sorted.Count(); i++)
                Items.Move(Items.IndexOf(sorted[i]), i);

            foreach (var item in Items)
            {
                if (item.IsDirectory)
                {
                    item.Sort();
                }
            }
        }

        public ObservableCollection<TreeEntry> Items { get; set; }
    }
}
