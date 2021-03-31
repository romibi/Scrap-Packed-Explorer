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

            fsPacked.Close();
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
    }
}
