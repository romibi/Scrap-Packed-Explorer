using ch.romibi.Scrap.Packed.PackerLib.DataTypes;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace ch.romibi.Scrap.Packed.PackerLib {
    public class ScrapPackedFile {
        public string FileName { get; private set; }
        public PackedMetaData MetaData;

        public ScrapPackedFile(string p_FileName, bool p_CanCreate = false) {
            FileName = p_FileName;

            if (!File.Exists(FileName) && p_CanCreate)
                CreateNewFile(TryMakeFile(FileName));

            ReadPackedMetaData();
        }

        // General functionality
        public void Add(string p_ExternalPath, string p_PackedPath) {
            FileAttributes fileAttributes = File.GetAttributes(p_ExternalPath);
            p_PackedPath = CorrectPath(p_PackedPath);
            if (fileAttributes.HasFlag(FileAttributes.Directory))
                AddDirectory(p_ExternalPath, p_PackedPath);
            else
                AddFile(p_ExternalPath, p_PackedPath);
        }
        public void Rename(string p_OldName, string p_NewName) {
            if (p_OldName.EndsWith("/") || p_OldName.Length == 0)
                RenameDirectory(p_OldName, p_NewName);
            else
                RenameFile(p_OldName, p_NewName);
        }
        public void Remove(string p_Name) {
            if (p_Name.EndsWith("/"))
                RemoveDirectory(p_Name);
            else
                RemoveFile(p_Name);
        }
        public void Extract(string p_PackedPath, string p_DestinationPath) {
            if (p_PackedPath.EndsWith("/") || p_PackedPath.Length == 0)
                ExtractDirectory(p_PackedPath, p_DestinationPath);
            else
                ExtractFile(p_PackedPath, p_DestinationPath);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0049:Simplify Names", Justification = "UInt Values written on disk have to be 32bit")]
        public void SaveToFile(string p_NewFileName, bool p_KeepBackup = false, bool p_OverrideOldBackup = true) {
            MetaData.RecalcFileOffsets();

            string newFileName = FileName;
            if (p_NewFileName.Length > 0)
                newFileName = p_NewFileName;

            if (File.Exists(newFileName)) {
                MakeBackup(newFileName);
                File.Delete(newFileName);
            }

            try {
                FileStream fsPackedNew = TryMakeFile(newFileName);
                try {
                    // write file header
                    byte[] writeBytes = new byte[4];
                    writeBytes = System.Text.Encoding.Default.GetBytes(PackedMetaData.FileHeader);
                    fsPackedNew.Write(writeBytes);

                    // write packed version
                    fsPackedNew.Write(BitConverter.GetBytes(MetaData.PackedVersion));

                    // write number of files
                    fsPackedNew.Write(BitConverter.GetBytes((UInt32)MetaData.FileList.Count));

                    // write the file index
                    WriteFileMetaData(fsPackedNew);
                    WriteFileData(fsPackedNew);

                    FileName = newFileName;
                } catch {
                    if (FileName.EndsWith(".bak"))
                        RestoreBackup(newFileName);
                    throw;
                } finally {
                    fsPackedNew.Close();
                }
            } catch {
                if (FileName.EndsWith(".bak"))
                    RestoreBackup(newFileName);
                throw;
            } finally { 
                if (!p_KeepBackup) {
                    DeleteBackup(newFileName);
                }
            }
        }

        // Getters
        // todo: deprecate this
        public List<string> GetFileNames() {
            // todo refactor list output
            List<string> list = new();
            foreach (PackedFileIndexData file in MetaData.FileList) {
                list.Add($"{file.FilePath}\tSize: {file.FileSize}\tOffset: {file.OriginalOffset}");
            }
            return list;
        }
        public List<IDictionary<string, string>> GetFileList() {
            // todo refactor list output
            List<IDictionary<string, string>> list = new();
            foreach (PackedFileIndexData file in MetaData.FileList) {
                Dictionary<string, string> FileData = new()
                {
                    { "FileName", Path.GetFileName(file.FilePath) },
                    { "FilePath", Path.GetDirectoryName(file.FilePath).Replace("\\", "/") },
                    { "FileSize", $"{file.FileSize}" },
                    { "FileOffset", $"{file.OriginalOffset}" }
                };

                list.Add(FileData);
            }
            return list;
        }
        public List<PackedFileIndexData> GetFileIndexDataList() {
            return MetaData.FileList;
        }
        public PackedFileIndexData GetFileIndexDataForFile(string p_PackedPath) {
            return MetaData.FileByPath[p_PackedPath];
        }
        // todo: this needs to be better
        public List<PackedFileIndexData> GetFolderContent(string p_Path) {
            List<PackedFileIndexData> result = new();
            foreach (PackedFileIndexData file in MetaData.FileList) {
                if (file.FilePath.StartsWith(p_Path))
                    result.Add(file);
            }
            return result;
        }

        // Add
        private void AddDirectory(string p_ExternalPath, string p_PackedPath) {
            string externalPath = p_ExternalPath.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            string packedPath = p_PackedPath.TrimEnd('/') + "/";
            if (packedPath == "/")
                packedPath = "";

            foreach (string file in Directory.EnumerateFiles(externalPath, "*", SearchOption.AllDirectories)) {
                string packedFilePath = CorrectPath(string.Concat(packedPath, file.AsSpan(externalPath.Length)));
                AddFile(file, packedFilePath);
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0049:Simplify Names", Justification = "UInt Values written on disk have to be 32bit")]
        private void AddFile(string p_ExternalPath, string p_PackedPath) {
            FileInfo newFile = new(p_ExternalPath);

            if (newFile.Length > UInt32.MaxValue)
                throw new InvalidDataException($"Unable to add file {p_ExternalPath}: file size is too big");

            string packedPath = p_PackedPath;
            if (packedPath.Length == 0)
                packedPath = CorrectPath(Path.GetFileName(p_ExternalPath));

            if (MetaData.FileByPath.ContainsKey(packedPath))
                RemoveFile(packedPath);

            PackedFileIndexData newFileIndexData = new(p_ExternalPath, packedPath, (UInt32)newFile.Length);
            MetaData.FileList.Add(newFileIndexData);
            MetaData.FileByPath.Add(packedPath, newFileIndexData);
        }

        // Rename
        private void RenameDirectory(string p_OldPath, string p_NewPath) {
            if (p_OldPath == "/")
                p_OldPath = "";

            List<PackedFileIndexData> fileList = GetFolderContent(p_OldPath);
            if (fileList.Count == 0)
                throw new ArgumentException($"Unable to rename {p_OldPath}: folder does not exists in {FileName}");

            foreach (PackedFileIndexData file in fileList)
                RenameFile(file.FilePath, string.Concat(p_NewPath, file.FilePath.AsSpan(p_OldPath.Length)));
        }
        private void RenameFile(string p_OldFileName, string p_NewFileName) {
            if (!MetaData.FileByPath.ContainsKey(p_OldFileName))
                throw new ArgumentException($"Unable to reanme {p_OldFileName}: file does not exists in {FileName}");

            PackedFileIndexData fileMetaData = MetaData.FileByPath[p_OldFileName];
            fileMetaData.FilePath = p_NewFileName;
            MetaData.FileByPath.Remove(p_OldFileName);
            MetaData.FileByPath.Add(p_NewFileName, fileMetaData);
        }

        // Remove
        private void RemoveDirectory(string p_Name) {
            if (p_Name == "/")
                p_Name = "";

            List<PackedFileIndexData> fileList = GetFolderContent(p_Name);
            if (fileList.Count == 0)
                throw new ArgumentException($"Unable to remove {p_Name}: folder does not exists in {FileName}");

            foreach (PackedFileIndexData file in fileList)
                RemoveFile(file.FilePath);
        }
        private void RemoveFile(string p_Name) {
            if (!MetaData.FileByPath.ContainsKey(p_Name))
                throw new ArgumentException($"Unable to remove {p_Name}: file does not exists in {FileName}");

            PackedFileIndexData oldFile = MetaData.FileByPath[p_Name];
            MetaData.FileList.Remove(oldFile);
            MetaData.FileByPath.Remove(p_Name);
        }

        // Extract
        private void ExtractDirectory(string p_PackedPath, string p_DestinationPath) {
            List<PackedFileIndexData> fileList = GetFolderContent(p_PackedPath);
            if (fileList.Count == 0)
                throw new ArgumentException($"Unable to extract {p_PackedPath}: folder does not exists in {FileName}");

            string destinationPath = p_DestinationPath.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            FileStream fsPacked = new(FileName, FileMode.Open);
            try {
                foreach (PackedFileIndexData file in fileList)
                    ExtractFile(file.FilePath, string.Concat(destinationPath, file.FilePath.AsSpan(p_PackedPath.Length)), fsPacked);
            } finally {
                fsPacked.Close();
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0049:Simplify Names", Justification = "UInt Values written on disk have to be 32bit")]
        private void ExtractFile(string p_PackedPath, string p_DestinationPath, FileStream p_PackedFileStream = null) {
            if (!MetaData.FileByPath.ContainsKey(p_PackedPath))
                throw new ArgumentException($"Unable to extract {p_PackedPath}: file does not exists in {FileName}");

            PackedFileIndexData fileMetaData = MetaData.FileByPath[p_PackedPath];

            if (File.Exists(p_DestinationPath))
                MakeBackup(p_DestinationPath, true);

            // If user specified destination path as directory filename needs to be added
            if (p_DestinationPath.EndsWith(Path.DirectorySeparatorChar)) {
                string[] path = p_PackedPath.Split('/');
                p_DestinationPath += path[^1];
            }

            FileStream fsPacked = p_PackedFileStream;
            if (fsPacked == null)
                fsPacked = new FileStream(FileName, FileMode.Open);

            try {
                FileStream fsExtractFile = TryMakeFile(p_DestinationPath);
                try {
                    byte[] readBytes = new byte[fileMetaData.FileSize];

                    fsPacked.Seek(fileMetaData.OriginalOffset, SeekOrigin.Begin);
                    fsPacked.Read(readBytes, 0, (Int32)fileMetaData.FileSize);

                    fsExtractFile.Write(readBytes);
                } catch {
                    if (Backups.ContainsKey(p_DestinationPath))
                        RestoreBackup(p_DestinationPath);
                    throw;
                } finally {
                    fsExtractFile.Close();
                }
            } catch {
                if (Backups.ContainsKey(p_DestinationPath))
                    RestoreBackup(p_DestinationPath);
                throw;
            } finally {
                if (p_PackedFileStream == null)
                    fsPacked.Close();
            }
            if (Backups.ContainsKey(p_DestinationPath))
                DeleteBackup(p_DestinationPath);
        }

        // Packed data reading
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0049:Simplify Names", Justification = "UInt Values written on disk have to be 32bit")]
        private void ReadPackedMetaData() {
            MetaData = new PackedMetaData {
                FileList = new List<PackedFileIndexData>(),
                FileByPath = new Dictionary<string, PackedFileIndexData>()
            };

            FileStream fsPacked = new(FileName, FileMode.Open);
            try {
                byte[] readBytes = new byte[4];

                // read file header
                fsPacked.Read(readBytes);
                string readFileHeader = System.Text.Encoding.Default.GetString(readBytes);

                if (readFileHeader != PackedMetaData.FileHeader) {
                    throw new InvalidDataException($"Unable to open '{Path.GetFullPath(FileName)}': unsupported file type.");
                }

                // read version
                fsPacked.Read(readBytes);
                MetaData.PackedVersion = BitConverter.ToUInt32(readBytes);

                // read number of files
                fsPacked.Read(readBytes);
                UInt32 numFiles = BitConverter.ToUInt32(readBytes);
                for (Int32 i = 0; i < numFiles; i++) {
                    PackedFileIndexData fileMetaData = ReadFileMetaData(fsPacked);
                    MetaData.FileList.Add(fileMetaData);
                    MetaData.FileByPath.Add(fileMetaData.FilePath, fileMetaData);
                }
            } finally {
                fsPacked.Close();
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0049:Simplify Names", Justification = "UInt Values written on disk have to be 32bit")]
        private static PackedFileIndexData ReadFileMetaData(FileStream p_FsPacked) {
            string fileName;
            UInt32 fileSize;
            UInt32 fileOffset;
            byte[] readByte = new byte[4];

            // Read file name length
            p_FsPacked.Read(readByte);
            UInt32 fileNameLength = BitConverter.ToUInt32(readByte);

            // read file name
            byte[] fileNameBytes = new byte[fileNameLength];
            p_FsPacked.Read(fileNameBytes);

            fileName = System.Text.Encoding.Default.GetString(fileNameBytes);

            // read file size
            p_FsPacked.Read(readByte);
            fileSize = BitConverter.ToUInt32(readByte);

            // read file offset
            p_FsPacked.Read(readByte);
            fileOffset = BitConverter.ToUInt32(readByte);

            return new PackedFileIndexData(fileName, fileSize, fileOffset);
        }

        // Packed data writing 
        private static void CreateNewFile(FileStream p_FsPacked) {
            try {
                byte[] fileHeader = System.Text.Encoding.Default.GetBytes(PackedMetaData.FileHeader);
                byte[] nullBytes = new byte[8];

                p_FsPacked.Write(fileHeader);
                p_FsPacked.Write(nullBytes);
            } finally {
                p_FsPacked.Close();
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0049:Simplify Names", Justification = "UInt Values written on disk have to be 32bit")]
        private void WriteFileMetaData(FileStream p_FsPackedNew) {
            foreach (PackedFileIndexData fileIndexEntry in MetaData.FileList) {
                // write the filepath length
                p_FsPackedNew.Write(BitConverter.GetBytes((UInt32)fileIndexEntry.FilePath.Length));

                // write the filepath
                byte[] writeBytes = System.Text.Encoding.Default.GetBytes(fileIndexEntry.FilePath);
                p_FsPackedNew.Write(writeBytes);

                // write the filesize
                p_FsPackedNew.Write(BitConverter.GetBytes(fileIndexEntry.FileSize));

                // write the file offset
                p_FsPackedNew.Write(BitConverter.GetBytes(fileIndexEntry.Offset));
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0049:Simplify Names", Justification = "UInt Values written on disk have to be 32bit")]
        private void WriteFileData(FileStream p_FsPackedNew) {
            FileStream fsPackedOrig = null;
            try {
                foreach (PackedFileIndexData file in MetaData.FileList) {
                    byte[] readBytes = new byte[file.FileSize];
                    if (file.UseExternalData)
                        ReadExternalFile(readBytes, file);
                    else {
                        if (fsPackedOrig == null)
                            fsPackedOrig = new FileStream(FileName, FileMode.Open);
                        fsPackedOrig.Seek(file.OriginalOffset, SeekOrigin.Begin);
                        fsPackedOrig.Read(readBytes, 0, (Int32)file.FileSize);
                    }
                    p_FsPackedNew.Write(readBytes);
                }
            } finally {
                if (fsPackedOrig != null)
                    fsPackedOrig.Close();
            }
        }

        // External files helpers
        private static FileStream TryMakeFile(string p_OutputPath) {
            Debug.Assert(!p_OutputPath.EndsWith(Path.DirectorySeparatorChar), "Output path cannot be only folder name.");

            string dirName = Path.GetDirectoryName(p_OutputPath);
            if (dirName == null)
                throw new IOException($"Unable to create file {p_OutputPath}: unexpected error.");

            else if (dirName != "") // if dirName is not the same dir as the working dir.
                Directory.CreateDirectory(dirName);

            return new FileStream(p_OutputPath, FileMode.Create);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0049:Simplify Names", Justification = "UInt Values written on disk have to be 32bit")]
        private static void ReadExternalFile(byte[] p_ReadByteBuffer, PackedFileIndexData p_FileIndexData) {
            FileStream externalFileStream = new(p_FileIndexData.ExternalFilePath, FileMode.Open);
            try {
                externalFileStream.Seek(p_FileIndexData.OriginalOffset, SeekOrigin.Begin);
                externalFileStream.Read(p_ReadByteBuffer, 0, (Int32)p_FileIndexData.FileSize);
            } finally {
                externalFileStream.Close();
            }
        }

        // Backup functionality
        private readonly Dictionary<string, string> Backups = new();
        private void MakeBackup(string p_FilePath, bool p_Temp = false) {
            // todo: test of this
            if (!File.Exists(p_FilePath))
                throw new FileNotFoundException($"Unable to backup '{p_FilePath}': file does not exists");

            string backupPath = p_FilePath;
            if (p_Temp) {
                string randomId;
                do {
                    randomId = $".{Guid.NewGuid().ToString()[..5]}.tmp";
                }
                while (File.Exists(backupPath + randomId + ".bak"));
                backupPath += randomId;
            }

            backupPath += ".bak";

            if (p_FilePath == FileName)
                FileName = backupPath;

            Backups.Add(p_FilePath, backupPath);
            File.Move(p_FilePath, backupPath, true);
        }
        private void RestoreBackup(string p_FilePath) {
            // todo: test of this
            if (!Backups.ContainsKey(p_FilePath))
                throw new FileNotFoundException($"File '{p_FilePath}' does not have any backups to restore");

            string backupPath = Backups[p_FilePath];

            if (!File.Exists(backupPath))
                throw new FileNotFoundException($"File '{p_FilePath}' was previously backed up to `{backupPath}` but that backup does not exist anymore.\r\n" +
                    $"Is there a bug somwhere in `MakeBackup()` or was the file deleted externally?"); // unreachble?

            if (p_FilePath == FileName)
                FileName = backupPath.Replace(".bak", "");

            Backups.Remove(p_FilePath);
            File.Move(backupPath, p_FilePath, true);
        }
        private void DeleteBackup(string p_FilePath) {
            // todo: test of this
            if (!Backups.ContainsKey(p_FilePath))
                // todo: we probably shouldn't throw this exception.
                //       I am not sure how to handle this so I just return from this function from now
                // throw new FileNotFoundException($"File '{p_FilePath}' does not have any backups to delete");
                return;

            string backupPath = Backups[p_FilePath];

            if (!File.Exists(backupPath))
                throw new FileNotFoundException($"File '{p_FilePath}' have a record of backup `{backupPath}` but it is not exists as file.\r\n" +
                    $"There is a bug somwhere in `MakeBackup()`"); // unreachble

            if (p_FilePath == FileName)
                FileName = backupPath.Replace(".bak", "");

            Backups.Remove(p_FilePath);
            File.Delete(backupPath);
        }

        public string CorrectPath(string p_FilePath) {
            if (p_FilePath == "")
                return p_FilePath;

            p_FilePath = p_FilePath.Replace("\\", "/");

            if (p_FilePath[0] == '/')
                p_FilePath = p_FilePath.Remove(0, 1);

            return p_FilePath;
        }
    }
}
