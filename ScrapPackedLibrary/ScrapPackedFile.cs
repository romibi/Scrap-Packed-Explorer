using ch.romibi.Scrap.Packed.PackerLib.DataTypes;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace ch.romibi.Scrap.Packed.PackerLib {
    public class ScrapPackedFile {
        public String FileName { get; private set; }
        public PackedMetaData MetaData;

        public ScrapPackedFile(String p_FileName, Boolean p_CanCreate = false) {
            FileName = p_FileName;

            if (!File.Exists(FileName) && p_CanCreate)
                CreateNewFile(TryMakeFile(FileName));

            ReadPackedMetaData();
        }

        // General functionality
        public void Add(String p_ExternalPath, String p_PackedPath) {
            FileAttributes fileAttributes = File.GetAttributes(p_ExternalPath);
            if (fileAttributes.HasFlag(FileAttributes.Directory))
                AddDirectory(p_ExternalPath, p_PackedPath);
            else
                AddFile(p_ExternalPath, p_PackedPath);
        }
        public void Rename(String p_OldName, String p_NewName) {
            if (p_OldName.EndsWith("/") || p_OldName.Length == 0)
                RenameDirectory(p_OldName, p_NewName);
            else
                RenameFile(p_OldName, p_NewName);
        }
        public void Remove(String p_Name) {
            if (p_Name.EndsWith("/"))
                RemoveDirectory(p_Name);
            else
                RemoveFile(p_Name);
        }
        public void Extract(String p_PackedPath, String p_DestinationPath) {
            if (p_PackedPath.EndsWith("/") || p_PackedPath.Length == 0)
                ExtractDirectory(p_PackedPath, p_DestinationPath);
            else
                ExtractFile(p_PackedPath, p_DestinationPath);
        }
        public void SaveToFile(String p_NewFileName) {
            MetaData.RecalcFileOffsets();

            String newFileName = FileName;
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
                    Byte[] writeBytes = new Byte[4];
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
            }
        }

        // Getters
        // todo: deprecate this
        public List<String> GetFileNames() {
            // todo refactor list output
            List<String> list = new();
            foreach (PackedFileIndexData file in MetaData.FileList) {
                list.Add($"{file.FilePath}\tSize: {file.FileSize}\tOffset: {file.OriginalOffset}");
            }
            return list;
        }
        public List<IDictionary<String, String>> GetFileList() {
            // todo refactor list output
            List<IDictionary<String, String>> list = new();
            foreach (PackedFileIndexData file in MetaData.FileList) {
                Dictionary<String, String> FileData = new()
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
        public PackedFileIndexData GetFileIndexDataForFile(String p_PackedPath) {
            return MetaData.FileByPath[p_PackedPath];
        }
        // todo: this needs to be better
        public List<PackedFileIndexData> GetFolderContent(String p_Path) {
            List<PackedFileIndexData> result = new();
            foreach (PackedFileIndexData file in MetaData.FileList) {
                if (file.FilePath.StartsWith(p_Path))
                    result.Add(file);
            }
            return result;
        }

        // ---------------------------------------------------------------------------

        // Add
        private void AddDirectory(String p_ExternalPath, String p_PackedPath) {
            String externalPath = p_ExternalPath.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            String packedPath = p_PackedPath.TrimEnd('/') + "/";
            if (packedPath == "/")
                packedPath = "";

            foreach (String file in Directory.EnumerateFiles(externalPath, "*", SearchOption.AllDirectories)) {
                String packedFilePath = String.Concat(packedPath, file.AsSpan(externalPath.Length));
                AddFile(file, packedFilePath);
            }
        }
        private void AddFile(String p_ExternalPath, String p_PackedPath) {
            FileInfo newFile = new(p_ExternalPath);

            if (newFile.Length > UInt32.MaxValue)
                throw new InvalidDataException($"Unable to add file {p_ExternalPath}: file size is too big");

            String packedPath = p_PackedPath;
            if (packedPath.Length == 0)
                packedPath = Path.GetFileName(p_ExternalPath);

            if (MetaData.FileByPath.ContainsKey(packedPath))
                RemoveFile(packedPath);

            PackedFileIndexData newFileIndexData = new(p_ExternalPath, packedPath, (UInt32)newFile.Length);
            MetaData.FileList.Add(newFileIndexData);
            MetaData.FileByPath.Add(packedPath, newFileIndexData);
        }

        // Rename
        private void RenameDirectory(String p_OldPath, String p_NewPath) {
            if (p_OldPath == "/")
                p_OldPath = "";

            List<PackedFileIndexData> fileList = GetFolderContent(p_OldPath);
            if (fileList.Count == 0)
                throw new ArgumentException($"Unable to rename {p_OldPath}: folder does not exists in {FileName}");

            foreach (PackedFileIndexData file in fileList)
                RenameFile(file.FilePath, String.Concat(p_NewPath, file.FilePath.AsSpan(p_OldPath.Length)));
        }
        private void RenameFile(String p_OldFileName, String p_NewFileName) {
            if (!MetaData.FileByPath.ContainsKey(p_OldFileName))
                throw new ArgumentException($"Unable to reanme {p_OldFileName}: file does not exists in {FileName}");

            PackedFileIndexData fileMetaData = MetaData.FileByPath[p_OldFileName];
            fileMetaData.FilePath = p_NewFileName;
            MetaData.FileByPath.Remove(p_OldFileName);
            MetaData.FileByPath.Add(p_NewFileName, fileMetaData);
        }

        // Remove
        private void RemoveDirectory(String p_Name) {
            if (p_Name == "/")
                p_Name = "";

            List<PackedFileIndexData> fileList = GetFolderContent(p_Name);
            if (fileList.Count == 0)
                throw new ArgumentException($"Unable to remove {p_Name}: folder does not exists in {FileName}");

            foreach (PackedFileIndexData file in fileList)
                RemoveFile(file.FilePath);
        }
        private void RemoveFile(String p_Name) {
            if (!MetaData.FileByPath.ContainsKey(p_Name))
                throw new ArgumentException($"Unable to remove {p_Name}: file does not exists in {FileName}");

            PackedFileIndexData oldFile = MetaData.FileByPath[p_Name];
            MetaData.FileList.Remove(oldFile);
            MetaData.FileByPath.Remove(p_Name);
        }

        // Extract
        private void ExtractDirectory(String p_PackedPath, String p_DestinationPath) {
            List<PackedFileIndexData> fileList = GetFolderContent(p_PackedPath);
            if (fileList.Count == 0)
                throw new ArgumentException($"Unable to extract {p_PackedPath}: folder does not exists in {FileName}");

            String destinationPath = p_DestinationPath.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            FileStream fsPacked = new(FileName, FileMode.Open);
            try {
                foreach (PackedFileIndexData file in fileList)
                    ExtractFile(file.FilePath, String.Concat(destinationPath, file.FilePath.AsSpan(p_PackedPath.Length)), fsPacked);
            } finally {
                fsPacked.Close();
            }
        }
        private void ExtractFile(String p_PackedPath, String p_DestinationPath, FileStream p_PackedFileStream = null) {
            if (!MetaData.FileByPath.ContainsKey(p_PackedPath))
                throw new ArgumentException($"Unable to extract {p_PackedPath}: file does not exists in {FileName}");

            PackedFileIndexData fileMetaData = MetaData.FileByPath[p_PackedPath];

            if (File.Exists(p_DestinationPath))
                MakeBackup(p_DestinationPath, true);

            // If user specified destination path as directory filename needs to be added
            if (p_DestinationPath.EndsWith(Path.DirectorySeparatorChar)) {
                String[] path = p_PackedPath.Split('/');
                p_DestinationPath += path[^1];
            }

            FileStream fsPacked = p_PackedFileStream;
            if (fsPacked == null)
                fsPacked = new FileStream(FileName, FileMode.Open);

            try {
                FileStream fsExtractFile = TryMakeFile(p_DestinationPath);
                try {
                    Byte[] readBytes = new Byte[fileMetaData.FileSize];

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
        private void ReadPackedMetaData() {
            MetaData = new PackedMetaData {
                FileList = new List<PackedFileIndexData>(),
                FileByPath = new Dictionary<String, PackedFileIndexData>()
            };

            FileStream fsPacked = new(FileName, FileMode.Open);
            try {
                Byte[] readBytes = new Byte[4];

                // read file header
                fsPacked.Read(readBytes);
                String readFileHeader = System.Text.Encoding.Default.GetString(readBytes);

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
        private static PackedFileIndexData ReadFileMetaData(FileStream p_FsPacked) {
            String fileName;
            UInt32 fileSize;
            UInt32 fileOffset;
            Byte[] readByte = new Byte[4];

            // Read file name length
            p_FsPacked.Read(readByte);
            UInt32 fileNameLength = BitConverter.ToUInt32(readByte);

            // read file name
            Byte[] fileNameBytes = new Byte[fileNameLength];
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
                Byte[] fileHeader = System.Text.Encoding.Default.GetBytes(PackedMetaData.FileHeader);
                Byte[] nullBytes = new Byte[8];

                p_FsPacked.Write(fileHeader);
                p_FsPacked.Write(nullBytes);
            } finally {
                p_FsPacked.Close();
            }
        }
        private void WriteFileMetaData(FileStream p_FsPackedNew) {
            foreach (PackedFileIndexData fileIndexEntry in MetaData.FileList) {
                // write the filepath length
                p_FsPackedNew.Write(BitConverter.GetBytes((UInt32)fileIndexEntry.FilePath.Length));

                // write the filepath
                Byte[] writeBytes = System.Text.Encoding.Default.GetBytes(fileIndexEntry.FilePath);
                p_FsPackedNew.Write(writeBytes);

                // write the filesize
                p_FsPackedNew.Write(BitConverter.GetBytes(fileIndexEntry.FileSize));

                // write the file offset
                p_FsPackedNew.Write(BitConverter.GetBytes(fileIndexEntry.Offset));
            }
        }
        private void WriteFileData(FileStream p_FsPackedNew) {
            FileStream fsPackedOrig = null;
            try {
                foreach (PackedFileIndexData file in MetaData.FileList) {
                    Byte[] readBytes = new Byte[file.FileSize];
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
        private static FileStream TryMakeFile(String p_OutputPath) {
            Debug.Assert(!p_OutputPath.EndsWith(Path.DirectorySeparatorChar), "Output path cannot be only folder name.");

            String dirName = Path.GetDirectoryName(p_OutputPath);
            if (dirName == null)
                throw new IOException($"Unable to create file {p_OutputPath}: unexpected error.");

            else if (dirName != "") // if dirName is not the same dir as the working dir.
                Directory.CreateDirectory(dirName);

            return new FileStream(p_OutputPath, FileMode.Create);
        }
        private static void ReadExternalFile(Byte[] p_ReadByteBuffer, PackedFileIndexData p_FileIndexData) {
            FileStream externalFileStream = new(p_FileIndexData.ExternalFilePath, FileMode.Open);
            try {
                externalFileStream.Seek(p_FileIndexData.OriginalOffset, SeekOrigin.Begin);
                externalFileStream.Read(p_ReadByteBuffer, 0, (Int32)p_FileIndexData.FileSize);
            } finally {
                externalFileStream.Close();
            }
        }

        // Backup functionality
        private readonly Dictionary<String, String> Backups = new();
        private void MakeBackup(String p_FilePath, Boolean p_Temp = false) {
            // todo: test of this
            if (!File.Exists(p_FilePath))
                throw new FileNotFoundException($"Unable to backup '{p_FilePath}': file does not exists");

            String backupPath = p_FilePath;
            if (p_Temp) {
                String randomId;
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
        private void RestoreBackup(String p_FilePath) {
            // todo: test of this
            if (!Backups.ContainsKey(p_FilePath))
                throw new FileNotFoundException($"File '{p_FilePath}' does not have any backups to restore");

            String backupPath = Backups[p_FilePath];

            if (!File.Exists(backupPath))
                throw new FileNotFoundException($"File '{p_FilePath}' was previously backed up to `{backupPath}` but that backup does not exist anymore.\r\n" +
                    $"Is there a bug somwhere in `MakeBackup()` or was the file deleted externally?"); // unreachble?

            if (p_FilePath == FileName)
                FileName = backupPath.Replace(".bak", "");

            Backups.Remove(p_FilePath);
            File.Move(backupPath, p_FilePath, true);
        }
        private void DeleteBackup(String p_FilePath) {
            // todo: test of this
            if (!Backups.ContainsKey(p_FilePath))
                throw new FileNotFoundException($"File '{p_FilePath}' does not have any backups to delete");

            String backupPath = Backups[p_FilePath];

            if (!File.Exists(backupPath))
                throw new FileNotFoundException($"File '{p_FilePath}' have a record of backup `{backupPath}` but it is not exists as file.\r\n" +
                    $"There is a bug somwhere in `MakeBackup()`"); // unreachble

            if (p_FilePath == FileName)
                FileName = backupPath.Replace(".bak", "");

            Backups.Remove(p_FilePath);
            File.Delete(backupPath);
        }
    }
}
