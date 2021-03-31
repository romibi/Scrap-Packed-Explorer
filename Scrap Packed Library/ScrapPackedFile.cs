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
                metaData.fileList = new List<PackedFileIndexData>();
                metaData.fileByPath = new Dictionary<string, PackedFileIndexData>();
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
            // todo refactor
            var list = new List<string>();
            foreach (var file in metaData.fileList)
            {
                list.Add(file.FilePath + " Size: " + file.FileSize + " Offset: " + file.OriginalOffset);
            }
            return list;
        }

        public void Rename(string p_oldName, string p_newName)
        {
            if (p_oldName.EndsWith("/"))
                RenameFolder(p_oldName, p_newName);
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

        private void RenameFolder(string p_oldPath, string p_newPath)
        {
            foreach (var file in metaData.fileList)
            {
                if (file.FilePath.StartsWith(p_oldPath)) {
                    RenameFile(file.FilePath, p_newPath + file.FilePath.Substring(p_oldPath.Length)); // todo check off by 1 error
                }
            }
        }

        public void SaveToFile(string p_newFileName)
        {
            metaData.RecalcFileOffsets();

            if (File.Exists(p_newFileName))
                File.Delete(p_newFileName); // todo implement backup function

            var fsPackedNew = new FileStream(p_newFileName, FileMode.Create);
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
            finally
            {
                fsPackedNew.Close();
            }
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
            var fsPackedOrig = new FileStream(fileName, FileMode.Open);
            try
            {
                foreach (var file in metaData.fileList)
                {
                    if (file.NewFileContent)
                    {
                        // Todo: implement add
                    }
                    else
                    {
                        byte[] readBytes = new byte[file.FileSize];
                        fsPackedOrig.Seek(file.OriginalOffset, SeekOrigin.Begin);
                        fsPackedOrig.Read(readBytes, 0, (int)file.FileSize);
                        p_fsPackedNew.Write(readBytes);
                    }
                }
            }
            finally
            {
                fsPackedOrig.Close();
            }
        }
    }
}
