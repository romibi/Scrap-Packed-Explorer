using System;
using System.Collections.Generic;

namespace ch.romibi.Scrap.Packed.PackerLib.DataTypes
{
    public class PackedMetaData
    {
        // Fields
        public const string FileHeader = "BFPK";
        public uint PackedVersion { get; set; } // always 0 (not sure if really a version number)
        public List<PackedFileIndexData> FileList { get; set; }
        public Dictionary<string, PackedFileIndexData> FileByPath { get; set; }

        // Methods
        public void RecalcFileOffsets()
        {
            uint currentOffset = CalculateFirstFileOffset();
            foreach (PackedFileIndexData file in FileList) {
                file.Offset = currentOffset;
                currentOffset += file.FileSize;

                if (currentOffset > uint.MaxValue - file.FileSize)
                    throw new OverflowException("Too much data for single container. Multipart containers are not supported yet");
            }
        }

        //-------------------------------------------------------

        // Why this is private? 
        private const uint DATA_LENGTH_STATIC = 12; // 4 bytes each: "PFBK", version (all 0s), number of files
        private uint CalculateFirstFileOffset()
        {
            uint result = DATA_LENGTH_STATIC;
            foreach (PackedFileIndexData fileEntry in FileList) {
                result += fileEntry.IndexEntrySize;
            }
            return result;
        }
    }

    public class PackedFileIndexData
    {
        // Fields
        public string FilePath { get; set; }
        public string OriginalFilePath { get; private set; }
        public uint FileSize { get; set; }
        public uint OriginalOffset { get; private set; }
        public uint Offset { get; set; }
        public uint IndexEntrySize => (uint)(DATA_LENGTH_STATIC + FilePath.Length);
        public bool UseExternalData => ExternalFilePath.Length != 0;
        public string ExternalFilePath { get; set; }

        // Constructors
        public PackedFileIndexData(string p_FilePath, uint p_FileSize, uint p_Offset) : this("", p_FilePath, p_FileSize, p_Offset) { }
        public PackedFileIndexData(string p_ExternalFilePath, string p_FilePath, uint p_FileSize, uint p_Offset = 0)
        {
            FilePath = p_FilePath;
            OriginalFilePath = p_FilePath;
            FileSize = p_FileSize;
            OriginalOffset = p_Offset;
            Offset = p_Offset;
            ExternalFilePath = p_ExternalFilePath;
        }

        //-------------------------------------------------------

        // Why this is private?
        private const uint DATA_LENGTH_STATIC = 12; // 4 bytes each: path length, file size, offset
    }
}
