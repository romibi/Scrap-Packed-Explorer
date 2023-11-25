using ch.romibi.Scrap.Packed.PackerLib.DataTypes;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ch.romibi.Scrap.Packed.PackerLib {
    public class ScrapTreeEntry : IComparable {
        public virtual ScrapTreeEntry CreateAndAdd(ScrapTreeEntry p_Parent, string p_Name = "", PackedFileIndexData p_IndexData = null) {
            ScrapTreeEntry Result = new ScrapTreeEntry(p_Parent) { Name = p_Name, IndexData = p_IndexData };
            Items.Add(Result);
            return Result;
        }

        public ScrapTreeEntry(ScrapTreeEntry p_Parent) {
            Items = new ObservableCollection<ScrapTreeEntry>();
            IndexData = null;
            Parent = p_Parent;
        }

        public string Name { get; set; }

        public ScrapTreeEntry Parent { get; set; }

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

        public void AddFileData(PackedFileIndexData p_File, string p_SubdirFilename = "") {
            string fileName;
            if (p_SubdirFilename.Length == 0) {
                fileName = p_File.FilePath;
            } else {
                fileName = p_SubdirFilename;
            }

            if (fileName.Contains('/')) {
                var nextDir = fileName.Split("/")[0];
                ScrapTreeEntry subDir = null;
                foreach (ScrapTreeEntry entry in Items) {
                    if (entry.Name.Equals(nextDir)) {
                        subDir = entry;
                        break;
                    }
                }
                if (subDir == null) {
                    subDir = CreateAndAdd(this, nextDir);
                }
                subDir.AddFileData(p_File, fileName.Substring(nextDir.Length + 1));
            } else {
                CreateAndAdd(this, fileName, p_File);
            }
        }

        public List<ScrapTreeEntry> GetItemPath() {
            // get a list of TreeItems from parent to selection
            // Todo: move logic inside TreeEntry and cache
            List<ScrapTreeEntry> itemPath = new List<ScrapTreeEntry>();

            var currentItem = this;
            itemPath.Add(currentItem);
            while (!(currentItem.Parent is null)) {
                currentItem = currentItem.Parent;
                itemPath.Insert(0, currentItem);
            }

            return itemPath;
        }

        public string GetItemPathString(bool p_IgnoreRoot = true) {
            List<ScrapTreeEntry> itemPath = GetItemPath();
            string pathString = "";

            for (var i = 0; i < itemPath.Count; i++) {
                if (p_IgnoreRoot && i == 0) continue;

                if (pathString.Length > 0)
                    pathString += "/";
                pathString += itemPath[i].Name;
            }

            if (itemPath.Last<ScrapTreeEntry>().IsDirectory)
                pathString += "/";

            return pathString;
        }

        public int CompareTo(object p_Other) {
            ScrapTreeEntry a = this;
            ScrapTreeEntry b = (ScrapTreeEntry)p_Other;
            if (a.IsDirectory == b.IsDirectory)
                return a.Name.CompareTo(b.Name);

            if (a.IsDirectory)
                return -1;
            else
                return 1;
        }

        public void Sort() {
            var sorted = Items.OrderBy(p_X => p_X).ToList();

            for (int i = 0; i < sorted.Count; i++)
                Items.Move(Items.IndexOf(sorted[i]), i);

            foreach (var item in Items) {
                if (item.IsDirectory) {
                    item.Sort();
                }
            }
        }

        public ObservableCollection<ScrapTreeEntry> Items { get; set; }
    }
}
