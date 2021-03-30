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
            metaData.fileList = new List<PackedFileMetaData>();
            for (int i = 0; i < numFiles; i++)
            {
                metaData.fileList.Add(ReadFileMetaData(fsPacked));
            }

            fsPacked.Close();
        }

        private PackedFileMetaData ReadFileMetaData(FileStream p_fsPacked)
        {
            var fileMetaData = new PackedFileMetaData();
            byte[] readByte = new byte[4];

            // Read file name length
            p_fsPacked.Read(readByte);
            UInt32 fileNameLength = BitConverter.ToUInt32(readByte);

            // read file name
            byte[] fileNameBytes = new byte[fileNameLength];
            p_fsPacked.Read(fileNameBytes);

            fileMetaData.filePath = System.Text.Encoding.Default.GetString(fileNameBytes);

            // read file size
            p_fsPacked.Read(readByte);
            fileMetaData.fileSize = BitConverter.ToUInt32(readByte);

            // read file offset
            p_fsPacked.Read(readByte);
            fileMetaData.originalOffset = BitConverter.ToUInt32(readByte);

            return fileMetaData;
        }

        public List<string> GetFileNames()
        {
            // todo refactor
            var list = new List<string>();
            foreach (var file in metaData.fileList)
            {
                list.Add(file.filePath + " Size: " + file.fileSize + " Offset: " + file.originalOffset);
            }
            return list;
        }
    }
}
