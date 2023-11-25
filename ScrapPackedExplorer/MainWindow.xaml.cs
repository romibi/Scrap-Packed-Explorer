using ch.romibi.Scrap.Packed.PackerLib;
using ch.romibi.Scrap.Packed.PackerLib.DataTypes;
using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
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

namespace ch.romibi.Scrap.Packed.Explorer {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// 

    // todo: lots of cleanup and refactoring
    public partial class MainWindow : Window, INotifyPropertyChanged {
        private ScrapPackedFile _loadedPackedFile;
        protected ScrapPackedFile LoadedPackedFile {
            get { return _loadedPackedFile; }
            set {
                _loadedPackedFile = value;
                ContainerLoaded = !(value is null);
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

        private bool _ContainerLoaded;
        public bool ContainerLoaded {
            get { return _ContainerLoaded; }
            set {
                _ContainerLoaded = value;
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

        private TreeViewTreeEntry CurrentFolder = null;

        private bool _FileTreeSelectionUpdating = false;

        protected readonly string NAVIGATE_UP_NAME = "..";
        protected readonly string PACKED_FILTER_STRING = "Scrapland Container|*.packed|All Files|*.*";

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged([CallerMemberName] string p_PropertyName = "") {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(p_PropertyName));
        }

        public MainWindow() {
            PendingChanges = false;
            InitializeComponent();
        }

        // TODO: Handle exceptions. Everywhere.
        public void LoadPackedFileByPath(string p_Packed) {
            try {
                LoadedPackedFile = new ScrapPackedFile(p_Packed);
            } catch (Exception ex) {
                Error(ex);
            }

            PendingChanges = true;
            RefreshTreeView();
        }

        private void RefreshTreeView() {

            TreeViewTreeEntry root = new TreeViewTreeEntry(null) { Name = LoadedPackedFile.FileName };

            foreach (PackedFileIndexData file in LoadedPackedFile.GetFileIndexDataList()) {
                root.AddFileData(file);
            }
            root.Sort();
            FileTree.Items.Clear();
            FileTree.Items.Add(root);

            TreeContent.Items.Clear();
            foreach (var item in root.Items) {
                TreeContent.Items.Add(item);
            }

            CurrentFolder = root;
        }

        private void TreeContent_MouseDoubleClick(object p_Sender, MouseButtonEventArgs p_EventArgs) {
            var clickedItem = ((FrameworkElement)p_EventArgs.OriginalSource).DataContext as TreeViewTreeEntry;
            TreeContent_LoadTreeEntry(clickedItem);
            var containerItem = clickedItem.GetContainerFromTree(FileTree);
            if (!(containerItem is null)) containerItem.IsExpanded = true;
        }

        private void FileTree_SelectedItemChanged(object p_Sender, RoutedPropertyChangedEventArgs<object> p_EventArgs) {
            if (p_EventArgs.NewValue is not TreeViewTreeEntry selectedEntry) return;
            if (_FileTreeSelectionUpdating) return;

            if (selectedEntry.IsDirectory) {
                TreeContent_LoadTreeEntry(selectedEntry);
            } else {
                TreeContent_LoadTreeEntry(selectedEntry.Parent as TreeViewTreeEntry);
                TreeContent.SelectedItem = selectedEntry;
            }
        }

        private void TreeContent_LoadTreeEntry(TreeViewTreeEntry p_Loaditem) {
            if (p_Loaditem != null) {
                if (!p_Loaditem.IsDirectory)
                    return;

                TreeContent.Items.Clear();
                if (!(p_Loaditem.Parent is null)) {
                    var navigateUpItem = new TreeViewTreeEntryAlias(p_Loaditem.Parent) { Name = NAVIGATE_UP_NAME };
                    TreeContent.Items.Add(navigateUpItem);
                }
                foreach (var item in p_Loaditem.Items) {
                    TreeContent.Items.Add(item);
                }

                CurrentFolder = p_Loaditem;
            }
        }

        private void TreeContent_ReloadCurrentFolder() {
            TreeContent_LoadTreeEntry(CurrentFolder);
        }

        private void TreeContent_SelectionChanged(object p_Sender, SelectionChangedEventArgs p_EventArgs) {
            ContentSelected = (TreeContent.SelectedItems.Count != 0);

            if (p_EventArgs.AddedItems.Count == 0) return;

            TreeViewTreeEntry selectedItem = p_EventArgs.AddedItems[0] as TreeViewTreeEntry;

            if (selectedItem is TreeViewTreeEntryAlias /* && selectedItem.Name.Equals(NAVIGATE_UP_NAME) */) {
                (TreeContent.ItemContainerGenerator.ContainerFromItem(selectedItem) as ListViewItem).IsSelected = false;
                return;
            }

            List<ScrapTreeEntry> itemPath = selectedItem.GetItemPath();

            // Update Tree Selection
            _FileTreeSelectionUpdating = true;
            try {
                ItemsControl currentLevel = FileTree as ItemsControl;
                // foreach folder (pathLevel) expand the correct subfolder
                foreach (TreeViewTreeEntry pathLevel in itemPath) {
                    TreeViewItem nextLevel = null;
                    bool doUpdateLayout = false;
                    // Check each subfolder if it is the wanted one and expand/collapse accordingly
                    foreach (TreeViewTreeEntry treeItem in currentLevel.Items) {
                        if (currentLevel.ItemContainerGenerator.ContainerFromItem(treeItem) is not TreeViewItem treeItemControl) break;

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
            } finally {
                _FileTreeSelectionUpdating = false;
            }
        }

        private static void TreeContent_SelectTreeViewItem(TreeViewItem p_SelectedTreeItem) {
            p_SelectedTreeItem.IsSelected = true;
            if (p_SelectedTreeItem.Items.Count != 0) {
                p_SelectedTreeItem.IsExpanded = false;
                p_SelectedTreeItem.UpdateLayout();
            }
            p_SelectedTreeItem.BringIntoView();
        }

        private void OpenButton_Click(object p_Sender, RoutedEventArgs p_EventArgs) {
            OpenFileDialog openFileDialog = new OpenFileDialog {
                Filter = PACKED_FILTER_STRING
            };

            if (openFileDialog.ShowDialog() == true) {
                LoadPackedFileByPath(openFileDialog.FileName);
            }
        }

        private void ExtractToButton_Click(object p_Sender, RoutedEventArgs p_EventArgs) {
            var selectedItems = TreeContent.SelectedItems;

            if (selectedItems.Count == 0) return; // todo make button not clickable in that case

            if (selectedItems.Count > 1) {
                ExtractToFolder(selectedItems);
            } else if ((selectedItems[0] as TreeViewTreeEntry).IsDirectory) {
                ExtractToFolder(selectedItems);
            } else {
                ExtractToFile(selectedItems[0] as TreeViewTreeEntry);
            }
        }

        private void ExtractToFile(TreeViewTreeEntry p_TreeEntry) {
            string packedPath = p_TreeEntry.IndexData.FilePath;
            string defaultFilename = packedPath.Split('/').Last();

            SaveFileDialog saveFileDialog = new SaveFileDialog {
                FileName = defaultFilename
            };
            if (saveFileDialog.ShowDialog() == true) {
                try {
                    LoadedPackedFile.Extract(p_TreeEntry.IndexData.FilePath, saveFileDialog.FileName);
                } catch (Exception ex) {
                    Error(ex);
                }
            }
        }

        private void ExtractToFolder(IList p_SelectedItems) {
            // todo: CommonOpenFileDialog is from WindowsAPICodePack-Shell: analyse Nuget warnings
            CommonOpenFileDialog folderDialog = new CommonOpenFileDialog {
                IsFolderPicker = true
            };
            if (folderDialog.ShowDialog() == CommonFileDialogResult.Ok) {
                foreach (var item in p_SelectedItems) {
                    Debug.Assert(item is TreeViewTreeEntry); // should always be the case
                    TreeViewTreeEntry entry = item as TreeViewTreeEntry;
                    try {
                        LoadedPackedFile.Extract(entry.GetItemPathString(), System.IO.Path.Combine(folderDialog.FileName, entry.Name));
                    } catch (Exception ex) {
                        Error(ex);
                    }
                }
            }
        }

        private void AddButton_Click(object p_Sender, RoutedEventArgs p_EventArgs) {
            OpenFileDialog openDialog = new OpenFileDialog();

            if (openDialog.ShowDialog() == true) {
                try {
                    string packedPathDir = CurrentFolder.GetItemPathString();
                    if (packedPathDir == "/") packedPathDir = "";

                    var packedPath = packedPathDir + System.IO.Path.GetFileName(openDialog.FileName);

                    LoadedPackedFile.Add(openDialog.FileName, packedPath);
                    (FileTree.Items[0] as TreeViewTreeEntry).AddFileData(LoadedPackedFile.GetFileIndexDataForFile(packedPath));
                    TreeContent_ReloadCurrentFolder();
                } catch (Exception ex) {
                    Error(ex);
                }
                PendingChanges = true;
            }
        }

        private void AddFolderButton_Click(object p_Sender, RoutedEventArgs p_EventArgs) {
            // todo: CommonOpenFileDialog is from WindowsAPICodePack-Shell: analyse Nuget warnings
            CommonOpenFileDialog openDialog = new CommonOpenFileDialog {
                IsFolderPicker = true
            };

            if (openDialog.ShowDialog() == CommonFileDialogResult.Ok) {
                try {
                    string packedPathDir = CurrentFolder.GetItemPathString();
                    if (packedPathDir == "/") packedPathDir = "";

                    var packedPath = packedPathDir + System.IO.Path.GetFileName(openDialog.FileName);

                    LoadedPackedFile.Add(openDialog.FileName, packedPath);
                    //(FileTree.Items[0] as TreeViewTreeEntry).AddFileData(LoadedPackedFile.GetFileIndexDataForFile(packedPath));
                    //TreeContent_ReloadCurrentFolder();
                    RefreshTreeView(); // todo: make this not need to refresh whole tree
                } catch (Exception ex) {
                    Error(ex);
                }
                PendingChanges = true;
            }
        }

        private void DeleteButton_Click(object p_Sender, RoutedEventArgs p_EventArgs) {
            List<TreeViewTreeEntry> selectedItems = new List<TreeViewTreeEntry>();
            foreach (var item in TreeContent.SelectedItems)
                selectedItems.Add(item as TreeViewTreeEntry);

            if (selectedItems.Count == 0) return;

            if (MessageBox.Show($"Do you really want delete {selectedItems.Count} elements?",
                    "Delete?",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question, MessageBoxResult.No) != MessageBoxResult.Yes) return;

            try {
                foreach (var item in selectedItems) {
                    LoadedPackedFile.Remove(item.GetItemPathString());
                    item.Parent.Items.Remove(item);
                }
            } catch (Exception ex) {
                Error(ex);
            }
            PendingChanges = true;

        }

        private void CreateButton_Click(object p_Sender, RoutedEventArgs p_EventArgs) {
            SaveFileDialog saveDialog = new SaveFileDialog {
                Filter = PACKED_FILTER_STRING,
                DefaultExt = ".packed",
                AddExtension = false // todo: ok? or true?
            };

            if (saveDialog.ShowDialog() == true) {
                if (File.Exists(saveDialog.FileName)) {
                    MessageBox.Show($"File {saveDialog.FileName} already exists. Cannot create new Container with that name!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                } else {
                    LoadedPackedFile = new ScrapPackedFile(saveDialog.FileName);
                    PendingChanges = true;
                    RefreshTreeView();
                }
            }
        }

        private void SearchButton_Click(object p_Sender, RoutedEventArgs p_EventArgs) {

        }

        private void SaveButton_Click(object p_Sender, RoutedEventArgs p_EventArgs) {
            SaveWithOverwriteWarning(LoadedPackedFile.FileName);
        }

        private void SaveAsButton_Click(object p_Sender, RoutedEventArgs p_EventArgs) {
            SaveFileDialog saveDialog = new SaveFileDialog {
                Filter = PACKED_FILTER_STRING,
                DefaultExt = ".packed",
                AddExtension = false // todo: ok? or true?
            };

            if (saveDialog.ShowDialog() == true) {
                SaveWithOverwriteWarning(saveDialog.FileName);
            }
        }

        private void SaveWithOverwriteWarning(string p_NewFileName) {
            if (File.Exists(p_NewFileName))
                if (MessageBox.Show($"Do you really want to overwrite {p_NewFileName}?",
                    "Overwrite?",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question, MessageBoxResult.No) != MessageBoxResult.Yes) return;

            try {

                LoadedPackedFile.SaveToFile(p_NewFileName);
                PendingChanges = false;
            } catch (Exception ex) {
                Error(ex);
            }
        }

        private static void Error(Exception p_Exception) {
            MessageBox.Show(
                p_Exception.Message,
                "Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error
                );
        }

        private void OptionsButton_Click(object p_Sender, RoutedEventArgs p_EventArgs) {

        }
    }

    public class TreeViewTreeEntryAlias : TreeViewTreeEntry {
        public TreeViewTreeEntryAlias(ScrapTreeEntry p_Reference) : base(p_Reference.Parent) {
            ReferencedEntry = p_Reference;
            IndexData = p_Reference.IndexData;
            Items = p_Reference.Items;
        }

        public ScrapTreeEntry ReferencedEntry { get; set; }
    }

    public class TreeViewTreeEntry : ScrapTreeEntry {
        public override ScrapTreeEntry CreateAndAdd(ScrapTreeEntry p_Parent, string p_Name = "", PackedFileIndexData p_IndexData = null) {
            TreeViewTreeEntry Result = new TreeViewTreeEntry(p_Parent) { Name = p_Name, IndexData = p_IndexData };
            Items.Add(Result);
            return Result;
        }

        public TreeViewTreeEntry(ScrapTreeEntry p_Parent) : base(p_Parent) {
        }

        public TreeViewItem GetContainerFromTree(TreeView p_TreeRoot) {
            var itemPath = GetItemPath();
            ItemsControl containerLevel = p_TreeRoot as ItemsControl;
            foreach (var pathLevel in itemPath) {
                foreach (var containerItem in containerLevel.Items) {
                    if (containerItem.Equals(pathLevel)) {
                        containerLevel = containerLevel.ItemContainerGenerator.ContainerFromItem(containerItem) as ItemsControl;
                        break;
                    }
                }
            }
            return containerLevel as TreeViewItem;
        }
    }
}
