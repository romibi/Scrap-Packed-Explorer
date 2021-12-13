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

            TreeEntry root = new TreeEntry() { Name = loadedPackedFile.fileName };

            foreach (string file in loadedPackedFile.GetFileNames()) {
                root.AddFilename(file);
            }

            FileTree.Items.Add(root);
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

    }

    public class TreeEntry
    {
        public TreeEntry()
        {
            this.Items = new ObservableCollection<TreeEntry>();
        }

        public string Name { get; set; }

        public void AddFilename(string p_FileName)
        {
            if (p_FileName.Contains("/"))
            {
                var nextDir = p_FileName.Split("/")[0];
                TreeEntry subDir = null;
                foreach(TreeEntry entry in Items)
                {
                    if (entry.Name.Equals(nextDir)) {
                        subDir = entry;
                        break;
                    }
                }
                if (subDir == null) {
                    subDir = new TreeEntry() { Name = nextDir };
                    Items.Add(subDir);
                }
                subDir.AddFilename(p_FileName.Substring(nextDir.Length + 1));
            } else
            {
                Items.Add(new TreeEntry() { Name = p_FileName });
            }
        }

        public ObservableCollection<TreeEntry> Items { get; set; }
    }
}
