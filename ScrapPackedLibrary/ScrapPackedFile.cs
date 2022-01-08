using ch.romibi.Scrap.Packed.PackerLib.DataTypes;
using System;
using System.Collections.Generic;
using System.IO;

namespace ch.romibi.Scrap.Packed.PackerLib
{
    public class ScrapPackedFile
    {
        public string fileName { get; private set; }
        PackedMetaData metaData;

        public ScrapPackedFile(string p_fileName)
        {
            fileName = p_fileName;
            ReadPackedMetaData();
        }

        private void ReadPackedMetaData()
        {
            metaData = new PackedMetaData();
            metaData.fileList = new List<PackedFileIndexData>();
            metaData.fileByPath = new Dictionary<string, PackedFileIndexData>();

            if (!File.Exists(fileName))
                return;

            var fsPacked = new FileStream(fileName, FileMode.Open);
            try
            {

                byte[] readBytes = new byte[4];

                // read file header
                fsPacked.Read(readBytes);
                string readFileHeader = System.Text.Encoding.Default.GetString(readBytes);

                if (readFileHeader != PackedMetaData.fileHeader)
                {
                    throw new InvalidDataException("unsupported file type");
                }

                // read version
                fsPacked.Read(readBytes);
                metaData.packedVersion = BitConverter.ToUInt32(readBytes);

                // read number of files
                fsPacked.Read(readBytes);
                var numFiles = BitConverter.ToUInt32(readBytes);
                for (int i = 0; i < numFiles; i++)
                {
                    var fileMetaData = ReadFileMetaData(fsPacked);
                    metaData.fileList.Add(fileMetaData);
                    metaData.fileByPath.Add(fileMetaData.FilePath, fileMetaData);
                }
            }
            finally
            {
                fsPacked.Close();
            }
        }

        private PackedFileIndexData ReadFileMetaData(FileStream p_fsPacked)
        {
            string fileName;
            UInt32 fileSize;
            UInt32 fileOffset;
            byte[] readByte = new byte[4];

            // Read file name length
            p_fsPacked.Read(readByte);
            UInt32 fileNameLength = BitConverter.ToUInt32(readByte);

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

        public List<string> GetFileNames()
        {
            // todo refactor list output
            var list = new List<string>();
            foreach (var file in metaData.fileList)
            {
                list.Add(file.FilePath + " Size: " + file.FileSize + " Offset: " + file.OriginalOffset);
            }
            return list;
        }

        public List<PackedFileIndexData> GetFileIndexDataList()
        {
            return metaData.fileList;
        }

        public void Add(string p_externalPath, string p_packedPath) 
        {
            FileAttributes fileAttributes = File.GetAttributes(p_externalPath);
            if (fileAttributes.HasFlag(FileAttributes.Directory))
                AddDirectory(p_externalPath, p_packedPath);
            else
                AddFile(p_externalPath, p_packedPath);
        }

        private void AddDirectory(string p_externalPath, string p_packedPath)
        {
            var externalPath = p_externalPath.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            var packedPath = p_packedPath.TrimEnd('/') + "/";
            if (packedPath == "/")
                packedPath = "";

            foreach (string file in Directory.EnumerateFiles(externalPath, "*", SearchOption.AllDirectories))
            {
                var packedFilePath = packedPath + file.Substring(externalPath.Length);
                AddFile(file, packedFilePath);
            }
        }

        private void AddFile(string p_externalPath, string p_packedPath)
        {
            if (!File.Exists(p_externalPath))
                return; // todo raise or log error

            var newFile = new FileInfo(p_externalPath);

            if (newFile.Length > UInt32.MaxValue)
                return; // todo raise or log error

            var packedPath = p_packedPath;
            if (packedPath.Length == 0)
                packedPath = Path.GetFileName(p_externalPath);

            if (metaData.fileByPath.ContainsKey(packedPath))
                RemoveFile(packedPath);

            var newFileIndexData = new PackedFileIndexData(p_externalPath, packedPath, (UInt32) newFile.Length);
            metaData.fileList.Add(newFileIndexData);
            metaData.fileByPath.Add(packedPath, newFileIndexData);
        }

        public void Rename(string p_oldName, string p_newName)
        {
            if (p_oldName.EndsWith("/") || p_oldName.Length == 0)
                RenameDirectory(p_oldName, p_newName);
            else
                RenameFile(p_oldName, p_newName);
        }

        private void RenameFile(string p_oldFileName, string p_newFileName)
        {
            var fileMetaData = metaData.fileByPath[p_oldFileName];
            fileMetaData.FilePath = p_newFileName;
            metaData.fileByPath.Remove(p_oldFileName);
            metaData.fileByPath.Add(p_newFileName, fileMetaData);
        }

        private void RenameDirectory(string p_oldPath, string p_newPath)
        {
            foreach (var file in metaData.fileList)
            {
                if (file.FilePath.StartsWith(p_oldPath)) {
                    RenameFile(file.FilePath, p_newPath + file.FilePath.Substring(p_oldPath.Length));
                }
            }
        }

        public void Remove(string p_Name)
        {
            if (p_Name.EndsWith("/"))
                RemoveDirectory(p_Name);
            else
                RemoveFile(p_Name);
        }

        private void RemoveFile(string p_Name)
        {
            if (!metaData.fileByPath.ContainsKey(p_Name))
                return; // todo raise or return error
            var oldFile = metaData.fileByPath[p_Name];
            metaData.fileList.Remove(oldFile);
            metaData.fileByPath.Remove(p_Name);
        }

        private void RemoveDirectory(string p_Name)
        {
            var fileList = metaData.fileList.ToArray();
            foreach (var file in fileList)
            {
                if (file.FilePath.StartsWith(p_Name))
                    RemoveFile(file.FilePath);
            }
        }

        public void Extract(string p_packedPath, string p_destinationPath)
        {
            if (p_packedPath.EndsWith("/") || p_packedPath.Length==0)
                ExtractDirectory(p_packedPath, p_destinationPath);
            else
                ExtractFile(p_packedPath, p_destinationPath);
        }

        private void ExtractDirectory(string p_packedPath, string p_destinationPath)
        {
            var destinationPath = p_destinationPath.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            var fsPacked = new FileStream(fileName, FileMode.Open);
            try
            {
                foreach (var file in metaData.fileList)
                {
                    if (file.FilePath.StartsWith(p_packedPath))
                    {
                        ExtractFile(file.FilePath, destinationPath + file.FilePath.Substring(p_packedPath.Length), fsPacked);
                    }
                }
            }
            finally
            {
                fsPacked.Close();
            }
        }

        private void ExtractFile(string p_packedPath, string p_destinationPath, FileStream p_PackedFileStream = null)
        {
            if (!metaData.fileByPath.ContainsKey(p_packedPath))
                return; // todo raise or return error;

            var fileMetaData = metaData.fileByPath[p_packedPath];

            if (File.Exists(p_destinationPath))
                File.Delete(p_destinationPath);

            Directory.CreateDirectory(Path.GetDirectoryName(p_destinationPath));

            var fsPacked = p_PackedFileStream;
            if(fsPacked==null)
                fsPacked = new FileStream(fileName, FileMode.Open);
            try
            {
                var fsExtractFile = new FileStream(p_destinationPath, FileMode.Create);
                try
                {
                    byte[] readBytes = new byte[fileMetaData.FileSize];

                    fsPacked.Seek(fileMetaData.OriginalOffset, SeekOrigin.Begin);
                    fsPacked.Read(readBytes, 0, (int)fileMetaData.FileSize);
                    
                    fsExtractFile.Write(readBytes);
                }
                finally
                {
                    fsExtractFile.Close();
                }
            }
            finally
            {
                if (p_PackedFileStream == null)
                    fsPacked.Close();
            }


        }

        public bool SaveToFile(string p_newFileName)
        {
            metaData.RecalcFileOffsets();

            var newFileName = fileName;
            var oldFileName = fileName;

            if (p_newFileName.Length > 0)
                newFileName = p_newFileName;
            else
            {
                if (File.Exists(fileName))
                    File.Move(fileName, fileName + ".bak", true);
                fileName = fileName + ".bak";
            }

            if (File.Exists(newFileName))
                File.Delete(newFileName);

            // todo: make backup function better

            string dirName = Path.GetDirectoryName(newFileName);
            
            if (dirName == null)
            {
                // restore backup
                if (File.Exists(fileName))
                    File.Move(fileName, oldFileName, true);
                fileName = oldFileName;

                return false;
            }
            else if (dirName != "") // if dirName is not same dir as working dir. 
            { 
                try
                {
                    // todo: when input "-o <folder_name>/<file_name>" while file (NOT DIR!)
                    // with name <folder_name> already existing couse excteption. 
                    Directory.CreateDirectory(dirName);
                }
                catch
                {
                    // restore backup
                    if (File.Exists(fileName))
                        File.Move(fileName, oldFileName, true);
                    fileName = oldFileName;

                    return false;
                }
            }

            var fsPackedNew = new FileStream(newFileName, FileMode.Create);
            try
            {
                // write file header
                byte[] writeBytes = new byte[4];
                writeBytes = System.Text.Encoding.Default.GetBytes(PackedMetaData.fileHeader);
                fsPackedNew.Write(writeBytes);

                // write packed version
                fsPackedNew.Write(BitConverter.GetBytes(metaData.packedVersion));

                // write number of files
                fsPackedNew.Write(BitConverter.GetBytes((UInt32)metaData.fileList.Count));

                // write the file index
                WriteFileMetaData(fsPackedNew);
                WriteFileData(fsPackedNew);
            }
            catch
            {
                return false;
            }
            finally
            {
                fsPackedNew.Close();
            }

            return true;
        }

        private void WriteFileMetaData(FileStream p_fsPackedNew)
        {
            foreach (var fileIndexEntry in metaData.fileList)
            {
                // write the filepath length
                p_fsPackedNew.Write(BitConverter.GetBytes((UInt32)fileIndexEntry.FilePath.Length));

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
            try
            {
                foreach (var file in metaData.fileList)
                {
                    byte[] readBytes = new byte[file.FileSize];
                    if (file.UseExternalData)
                        ReadExternalFile(readBytes, file);
                    else
                    {
                        if (fsPackedOrig == null)
                            fsPackedOrig = new FileStream(fileName, FileMode.Open);
                        fsPackedOrig.Seek(file.OriginalOffset, SeekOrigin.Begin);
                        fsPackedOrig.Read(readBytes, 0, (int)file.FileSize);
                    }
                    p_fsPackedNew.Write(readBytes);
                }
            }
            finally
            {
                if (fsPackedOrig != null)
                    fsPackedOrig.Close();
            }
        }

        private void ReadExternalFile(byte[] p_readByteBuffer, PackedFileIndexData fileIndexData)
        {
            var externalFileStream = new FileStream(fileIndexData.ExternalFilePath, FileMode.Open);
            try
            {
                externalFileStream.Seek(fileIndexData.OriginalOffset, SeekOrigin.Begin);
                externalFileStream.Read(p_readByteBuffer, 0, (int)fileIndexData.FileSize);
            }
            finally
            {
                externalFileStream.Close();
            }
        }
    }
}
