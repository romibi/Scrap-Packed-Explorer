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

namespace ch.romibi.Scrap.Packed.Explorer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        ScrapPackedFile loadedPackedFile;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void RefreshTreeView()
        {

            TreeEntry root = new TreeEntry(null) { Name = loadedPackedFile.fileName };

            foreach (PackedFileIndexData file in loadedPackedFile.GetFileIndexDataList())
            {
                root.AddFileData(file);
            }
            root.Sort();
            FileTree.Items.Add(root);

            TreeContent.Items.Clear();
            //TreeContent.Items.Add(new TreeEntry() { Name = ".." });
            foreach (var item in root.Items) {
                TreeContent.Items.Add(item);
            }

        }

        private void TreeContent_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var clickedItem = ((FrameworkElement)e.OriginalSource).DataContext as TreeEntry;
            if (clickedItem != null)
            {
                if (!clickedItem.IsDirectory)
                    return;
                TreeContent.Items.Clear();
                if(!(clickedItem.Parent is null))
                {
                    var navigateUpItem = new TreeEntry(clickedItem.Parent.Parent) { Name = "..", Items = clickedItem.Parent.Items };
                    TreeContent.Items.Add(navigateUpItem);
                }
                foreach(var item in clickedItem.Items)
                {
                    TreeContent.Items.Add(item);
                }
            }
        }


        private void OpenButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            if (openFileDialog.ShowDialog() == true)
            {
                loadedPackedFile = new ScrapPackedFile(openFileDialog.FileName);
                RefreshTreeView();
            }
        }

        private void ExtractToButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void CreateButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {

        }
        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {

        }
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

            foreach (var item in Items) {
                if (item.IsDirectory) {
                    item.Sort();
                }
            }
        }

        public ObservableCollection<TreeEntry> Items { get; set; }
    }
}
