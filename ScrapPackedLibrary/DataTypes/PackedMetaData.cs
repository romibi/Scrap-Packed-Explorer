using System;
using System.Collections.Generic;
using System.Text;

namespace ch.romibi.Scrap.Packed.PackerLib.DataTypes
{
    public class PackedMetaData
    {
        private const UInt32 DATA_LENGTH_STATIC = 12; // 4 bytes each: "PFBK", version (all 0s), number of files

        public const string fileHeader = "BFPK";
        public UInt32 packedVersion { get; set; } // always 0 (not sure if really a version number)
        public List<PackedFileIndexData> fileList { get; set; }
        public Dictionary<string, PackedFileIndexData> fileByPath { get; set; }

        public void RecalcFileOffsets()
        {
            UInt32 currentOffset = CalculateFirstFileOffset();
            foreach (var file in fileList)
            {
                file.Offset = currentOffset;
                currentOffset += file.FileSize;

                if (currentOffset > UInt32.MaxValue - file.FileSize)
                    throw new OverflowException("Too much data for single container. Multipart containers are not supported yet");
            }
        }

        private UInt32 CalculateFirstFileOffset()
        {
            UInt32 result = DATA_LENGTH_STATIC;
            foreach (var fileEntry in fileList)
            {
                result += fileEntry.IndexEntrySize;
            }
            return result;
        }
    }

    public class PackedFileIndexData
    {
        private const UInt32 DATA_LENGTH_STATIC = 12; // 4 bytes each: path length, file size, offset

        public PackedFileIndexData(string p_FilePath, UInt32 p_FileSize, UInt32 p_Offset) : this("", p_FilePath, p_FileSize, p_Offset) { }

        public PackedFileIndexData(string p_ExternalFilePath, string p_FilePath, UInt32 p_FileSize, UInt32 p_Offset = 0)
        {
            FilePath = p_FilePath;
            OriginalFilePath = p_FilePath;
            FileSize = p_FileSize;
            OriginalOffset = p_Offset;
            Offset = p_Offset;
            ExternalFilePath = p_ExternalFilePath;
        }

        public string FilePath { get; set; }
        public string OriginalFilePath { get; private set; }
        public UInt32 FileSize { get; set; }
        public UInt32 OriginalOffset { get; private set; }
        public UInt32 Offset { get; set; }

        public bool UseExternalData { get {
                return ExternalFilePath.Length != 0;
        } }

        public string ExternalFilePath { get; set; }

        public UInt32 IndexEntrySize {
            get {
                return (uint)(DATA_LENGTH_STATIC + FilePath.Length);
            }
        }
    }
}
