using ch.romibi.Scrap.Packed.PackerLib.DataTypes;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace ch.romibi.Scrap.Packed.PackerLib
{
    public class ScrapPackedFile
    {
        public string fileName { get; private set; }
        public PackedMetaData metaData;

        public ScrapPackedFile(string p_fileName, bool p_canCreate = false)
        {
            fileName = p_fileName;

            if (!File.Exists(fileName) && p_canCreate)
                CreateNewFile(TryMakeFile(fileName));

            ReadPackedMetaData();
        }
        
        // General functionality
        public void Add(string p_externalPath, string p_packedPath)
        {
            FileAttributes fileAttributes = File.GetAttributes(p_externalPath);
            if (fileAttributes.HasFlag(FileAttributes.Directory))
                AddDirectory(p_externalPath, p_packedPath);
            else
                AddFile(p_externalPath, p_packedPath);
        }
        public void Rename(string p_oldName, string p_newName)
        {
            if (p_oldName.EndsWith("/") || p_oldName.Length == 0)
                RenameDirectory(p_oldName, p_newName);
            else
                RenameFile(p_oldName, p_newName);
        }
        public void Remove(string p_Name)
        {
            if (p_Name.EndsWith("/"))
                RemoveDirectory(p_Name);
            else
                RemoveFile(p_Name);
        }
        public void Extract(string p_packedPath, string p_destinationPath)
        {
            if (p_packedPath.EndsWith("/") || p_packedPath.Length == 0)
                ExtractDirectory(p_packedPath, p_destinationPath);
            else
                ExtractFile(p_packedPath, p_destinationPath);
        }
        public void SaveToFile(string p_newFileName)
        {
            metaData.RecalcFileOffsets();

            string newFileName = fileName;
            if (p_newFileName.Length > 0)
                newFileName = p_newFileName;

            if (File.Exists(newFileName)) {
                MakeBackup(newFileName);
                File.Delete(newFileName);
            }

            try {
                FileStream fsPackedNew = TryMakeFile(newFileName);
                try {
                    // write file header
                    byte[] writeBytes = new byte[4];
                    writeBytes = System.Text.Encoding.Default.GetBytes(PackedMetaData.fileHeader);
                    fsPackedNew.Write(writeBytes);

                    // write packed version
                    fsPackedNew.Write(BitConverter.GetBytes(metaData.packedVersion));

                    // write number of files
                    fsPackedNew.Write(BitConverter.GetBytes((uint)metaData.fileList.Count));

                    // write the file index
                    WriteFileMetaData(fsPackedNew);
                    WriteFileData(fsPackedNew);

                    fileName = newFileName;
                }
                catch (Exception ex) {
                    if (fileName.EndsWith(".bak"))
                        RestoreBackup(newFileName);
                    throw ex;
                }
                finally {
                    fsPackedNew.Close();
                }
            }
            catch (Exception ex) {
                if (fileName.EndsWith(".bak"))
                    RestoreBackup(newFileName);
                throw ex;
            }
        }

        // Getters
        // todo: deprecate this
        public List<string> GetFileNames()
        {
            // todo refactor list output
            List<string> list = new List<string>();
            foreach (PackedFileIndexData file in metaData.fileList) {
                list.Add($"{file.FilePath}\tSize: {file.FileSize}\tOffset: {file.OriginalOffset}");
            }
            return list;
        }
        public List<IDictionary<string, string>> GetFileList()
        {
            // todo refactor list output
            List<IDictionary<string, string>> list = new List<IDictionary<string, string>>();
            foreach (PackedFileIndexData file in metaData.fileList) {
                Dictionary<string, string> FileData = new Dictionary<string, string>()
                {
                    { "FileName",   Path.GetFileName(file.FilePath) },
                    { "FilePath",   Path.GetDirectoryName(file.FilePath).Replace("\\", "/") },
                    { "FileSize",   $"{file.FileSize}" },
                    { "FileOffset", $"{file.OriginalOffset}" }
                };

                list.Add(FileData);
            }
            return list;
        }
        public List<PackedFileIndexData> GetFileIndexDataList()
        {
            return metaData.fileList;
        }
        public PackedFileIndexData GetFileIndexDataForFile(string p_packedPath)
        {
            return metaData.fileByPath[p_packedPath];
        }
        // todo: this needs to be better
        public List<PackedFileIndexData> GetFolderContent(string path)
        {
            List<PackedFileIndexData> result = new List<PackedFileIndexData>();
            foreach (PackedFileIndexData file in metaData.fileList) {
                if (file.FilePath.StartsWith(path))
                    result.Add(file);
            }
            return result;
        }

        // ---------------------------------------------------------------------------
        
        // Add
        private void AddDirectory(string p_externalPath, string p_packedPath)
        {
            string externalPath = p_externalPath.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            string packedPath = p_packedPath.TrimEnd('/') + "/";
            if (packedPath == "/")
                packedPath = "";

            foreach (string file in Directory.EnumerateFiles(externalPath, "*", SearchOption.AllDirectories)) {
                string packedFilePath = packedPath + file.Substring(externalPath.Length);
                AddFile(file, packedFilePath);
            }
        }
        private void AddFile(string p_externalPath, string p_packedPath)
        {
            FileInfo newFile = new FileInfo(p_externalPath);

            if (newFile.Length > uint.MaxValue)
                throw new InvalidDataException($"Unable to add file {p_externalPath}: file size is too big");

            string packedPath = p_packedPath;
            if (packedPath.Length == 0)
                packedPath = Path.GetFileName(p_externalPath);

            if (metaData.fileByPath.ContainsKey(packedPath))
                RemoveFile(packedPath);

            PackedFileIndexData newFileIndexData = new PackedFileIndexData(p_externalPath, packedPath, (uint)newFile.Length);
            metaData.fileList.Add(newFileIndexData);
            metaData.fileByPath.Add(packedPath, newFileIndexData);
        }
        
        // Rename
        private void RenameDirectory(string p_oldPath, string p_newPath)
        {
            if (p_oldPath == "/")
                p_oldPath = "";

            List<PackedFileIndexData> fileList = GetFolderContent(p_oldPath);
            if (fileList.Count == 0)
                throw new ArgumentException($"Unable to rename {p_oldPath}: folder does not exists in {fileName}");

            foreach (PackedFileIndexData file in fileList)
                RenameFile(file.FilePath, p_newPath + file.FilePath.Substring(p_oldPath.Length));
        }
        private void RenameFile(string p_oldFileName, string p_newFileName)
        {
            if (!metaData.fileByPath.ContainsKey(p_oldFileName))
                throw new ArgumentException($"Unable to reanme {p_oldFileName}: file does not exists in {fileName}");

            PackedFileIndexData fileMetaData = metaData.fileByPath[p_oldFileName];
            fileMetaData.FilePath = p_newFileName;
            metaData.fileByPath.Remove(p_oldFileName);
            metaData.fileByPath.Add(p_newFileName, fileMetaData);
        }
        
        // Remove
        private void RemoveDirectory(string p_Name)
        {
            if (p_Name == "/")
                p_Name = "";

            List<PackedFileIndexData> fileList = GetFolderContent(p_Name);
            if (fileList.Count == 0)
                throw new ArgumentException($"Unable to remove {p_Name}: folder does not exists in {fileName}");

            foreach (PackedFileIndexData file in fileList)
                RemoveFile(file.FilePath);
        }
        private void RemoveFile(string p_Name)
        {
            if (!metaData.fileByPath.ContainsKey(p_Name))
                throw new ArgumentException($"Unable to remove {p_Name}: file does not exists in {fileName}");

            PackedFileIndexData oldFile = metaData.fileByPath[p_Name];
            metaData.fileList.Remove(oldFile);
            metaData.fileByPath.Remove(p_Name);
        }
        
        // Extract
        private void ExtractDirectory(string p_packedPath, string p_destinationPath)
        {
            List<PackedFileIndexData> fileList = GetFolderContent(p_packedPath);
            if (fileList.Count == 0)
                throw new ArgumentException($"Unable to extract {p_packedPath}: folder does not exists in {fileName}");

            string destinationPath = p_destinationPath.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            FileStream fsPacked = new FileStream(fileName, FileMode.Open);
            try {
                foreach (PackedFileIndexData file in fileList)
                    ExtractFile(file.FilePath, destinationPath + file.FilePath.Substring(p_packedPath.Length), fsPacked);
            }
            finally {
                fsPacked.Close();
            }
        }
        private void ExtractFile(string p_packedPath, string p_destinationPath, FileStream p_PackedFileStream = null)
        {
            if (!metaData.fileByPath.ContainsKey(p_packedPath))
                throw new ArgumentException($"Unable to extract {p_packedPath}: file does not exists in {fileName}");

            PackedFileIndexData fileMetaData = metaData.fileByPath[p_packedPath];

            if (File.Exists(p_destinationPath))
                MakeBackup(p_destinationPath, true);

            // If user specified destination path as directory filename needs to be added
            if (p_destinationPath.EndsWith(Path.DirectorySeparatorChar)) {
                string[] path = p_packedPath.Split('/');
                p_destinationPath = p_destinationPath + path[path.Length - 1];
            }

            FileStream fsPacked = p_PackedFileStream;
            if (fsPacked == null)
                fsPacked = new FileStream(fileName, FileMode.Open);

            try {
                FileStream fsExtractFile = TryMakeFile(p_destinationPath);
                try {
                    byte[] readBytes = new byte[fileMetaData.FileSize];

                    fsPacked.Seek(fileMetaData.OriginalOffset, SeekOrigin.Begin);
                    fsPacked.Read(readBytes, 0, (int)fileMetaData.FileSize);

                    fsExtractFile.Write(readBytes);
                }
                catch (Exception ex) {
                    if (backups.ContainsKey(p_destinationPath))
                        RestoreBackup(p_destinationPath);
                    throw ex;
                }
                finally {
                    fsExtractFile.Close();
                }
            }
            catch (Exception ex) {
                if (backups.ContainsKey(p_destinationPath))
                    RestoreBackup(p_destinationPath);
                throw ex;
            }
            finally {
                if (p_PackedFileStream == null)
                    fsPacked.Close();
            }
            if (backups.ContainsKey(p_destinationPath))
                DeleteBackup(p_destinationPath);
        }
        
        // Packed data reading
        private void ReadPackedMetaData()
        {
            metaData = new PackedMetaData {
                fileList = new List<PackedFileIndexData>(),
                fileByPath = new Dictionary<string, PackedFileIndexData>()
            };

            FileStream fsPacked = new FileStream(fileName, FileMode.Open);
            try {

                byte[] readBytes = new byte[4];

                // read file header
                fsPacked.Read(readBytes);
                string readFileHeader = System.Text.Encoding.Default.GetString(readBytes);

                if (readFileHeader != PackedMetaData.fileHeader) {
                    throw new InvalidDataException($"Unable to open '{Path.GetFullPath(fileName)}': unsupported file type.");
                }

                // read version
                fsPacked.Read(readBytes);
                metaData.packedVersion = BitConverter.ToUInt32(readBytes);

                // read number of files
                fsPacked.Read(readBytes);
                uint numFiles = BitConverter.ToUInt32(readBytes);
                for (int i = 0; i < numFiles; i++) {
                    PackedFileIndexData fileMetaData = ReadFileMetaData(fsPacked);
                    metaData.fileList.Add(fileMetaData);
                    metaData.fileByPath.Add(fileMetaData.FilePath, fileMetaData);
                }
            }
            finally {
                fsPacked.Close();
            }
        }
        private PackedFileIndexData ReadFileMetaData(FileStream p_fsPacked)
        {
            string fileName;
            uint fileSize;
            uint fileOffset;
            byte[] readByte = new byte[4];

            // Read file name length
            p_fsPacked.Read(readByte);
            uint fileNameLength = BitConverter.ToUInt32(readByte);

            // read file name
            byte[] fileNameBytes = new byte[fileNameLength];
            p_fsPacked.Read(fileNameBytes);

            fileName = System.Text.Encoding.Default.GetString(fileNameBytes);

            // read file size
            p_fsPacked.Read(readByte);
            fileSize = BitConverter.ToUInt32(readByte);

            // read file offset
            p_fsPacked.Read(readByte);
            fileOffset = BitConverter.ToUInt32(readByte);

            return new PackedFileIndexData(fileName, fileSize, fileOffset);
        }
       
        // Packed data writing 
        private void CreateNewFile(FileStream fsPacked)
        {
            try {
                byte[] fileHeader = System.Text.Encoding.Default.GetBytes(PackedMetaData.fileHeader);
                byte[] nullBytes = new byte[8];

                fsPacked.Write(fileHeader);
                fsPacked.Write(nullBytes);
            }
            finally {
                fsPacked.Close();
            }
        }
        private void WriteFileMetaData(FileStream p_fsPackedNew)
        {
            foreach (PackedFileIndexData fileIndexEntry in metaData.fileList) {
                // write the filepath length
                p_fsPackedNew.Write(BitConverter.GetBytes((uint)fileIndexEntry.FilePath.Length));

                // write the filepath
                byte[] writeBytes = new byte[fileIndexEntry.FilePath.Length];
                writeBytes = System.Text.Encoding.Default.GetBytes(fileIndexEntry.FilePath);
                p_fsPackedNew.Write(writeBytes);

                // write the filesize
                p_fsPackedNew.Write(BitConverter.GetBytes(fileIndexEntry.FileSize));

                // write the file offset
                p_fsPackedNew.Write(BitConverter.GetBytes(fileIndexEntry.Offset));
            }
        }
        private void WriteFileData(FileStream p_fsPackedNew)
        {
            FileStream fsPackedOrig = null;
            try {
                foreach (PackedFileIndexData file in metaData.fileList) {
                    byte[] readBytes = new byte[file.FileSize];
                    if (file.UseExternalData)
                        ReadExternalFile(readBytes, file);
                    else {
                        if (fsPackedOrig == null)
                            fsPackedOrig = new FileStream(fileName, FileMode.Open);
                        fsPackedOrig.Seek(file.OriginalOffset, SeekOrigin.Begin);
                        fsPackedOrig.Read(readBytes, 0, (int)file.FileSize);
                    }
                    p_fsPackedNew.Write(readBytes);
                }
            }
            finally {
                if (fsPackedOrig != null)
                    fsPackedOrig.Close();
            }
        }
        
        // External files helpers
        private FileStream TryMakeFile(string outputPath)
        {
            Debug.Assert(!outputPath.EndsWith(Path.DirectorySeparatorChar), "Output path cannot be only folder name.");

            string dirName = Path.GetDirectoryName(outputPath);
            if (dirName == null)
                throw new IOException($"Unable to create file {outputPath}: unexpected error.");

            else if (dirName != "") // if dirName is not the same dir as the working dir.
                Directory.CreateDirectory(dirName);

            return new FileStream(outputPath, FileMode.Create);
        }
        private void ReadExternalFile(byte[] p_readByteBuffer, PackedFileIndexData fileIndexData)
        {
            FileStream externalFileStream = new FileStream(fileIndexData.ExternalFilePath, FileMode.Open);
            try {
                externalFileStream.Seek(fileIndexData.OriginalOffset, SeekOrigin.Begin);
                externalFileStream.Read(p_readByteBuffer, 0, (int)fileIndexData.FileSize);
            }
            finally {
                externalFileStream.Close();
            }
        }
        
        // Backup functionality
        private readonly Dictionary<string, string> backups = new Dictionary<string, string>();
        private void MakeBackup(string filePath, bool temp = false)
        {
            // todo: test of this
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"Unable to backup '{filePath}': file does not exists");

            string backupPath = filePath;
            if (temp) {
                string randomId;
                do {
                    randomId = $".{Guid.NewGuid().ToString().Substring(0, 5)}.tmp";
                }
                while (File.Exists(backupPath + randomId + ".bak"));
                backupPath += randomId;
            }

            backupPath += ".bak";

            if (filePath == fileName)
                fileName = backupPath;

            backups.Add(filePath, backupPath);
            File.Move(filePath, backupPath, true);
        }
        private void RestoreBackup(string filePath)
        {
            // todo: test of this
            if (!backups.ContainsKey(filePath))
                throw new FileNotFoundException($"File '{filePath}' does not have any backups to restore");

            string backupPath = backups[filePath];

            if (!File.Exists(backupPath))
                throw new FileNotFoundException($"File '{filePath}' was previously backed up to `{backupPath}` but that backup does not exist anymore.\r\n" +
                    $"Is there a bug somwhere in `MakeBackup()` or was the file deleted externally?"); // unreachble?

            if (filePath == fileName)
                fileName = backupPath.Replace(".bak", "");

            backups.Remove(filePath);
            File.Move(backupPath, filePath, true);
        }
        private void DeleteBackup(string filePath)
        {
            // todo: test of this
            if (!backups.ContainsKey(filePath))
                throw new FileNotFoundException($"File '{filePath}' does not have any backups to delete");

            string backupPath = backups[filePath];

            if (!File.Exists(backupPath))
                throw new FileNotFoundException($"File '{filePath}' have a record of backup `{backupPath}` but it is not exists as file.\r\n" +
                    $"There is a bug somwhere in `MakeBackup()`"); // unreachble

            if (filePath == fileName)
                fileName = backupPath.Replace(".bak", "");

            backups.Remove(filePath);
            File.Delete(backupPath);
        }
    }
}
