using System;
using System.Collections.Generic;

namespace ch.romibi.Scrap.Packed.PackerLib.DataTypes {
    public class PackedMetaData {
        // Fields
        public const String FileHeader = "BFPK";
        public UInt32 PackedVersion { get; set; } // always 0 (not sure if really a version number)
        public List<PackedFileIndexData> FileList { get; set; }
        public Dictionary<String, PackedFileIndexData> FileByPath { get; set; }

        // Methods
        public void RecalcFileOffsets() {
            UInt32 currentOffset = CalculateFirstFileOffset();
            foreach (PackedFileIndexData file in FileList) {
                file.Offset = currentOffset;
                currentOffset += file.FileSize;

                if (currentOffset > UInt32.MaxValue - file.FileSize)
                    throw new OverflowException("Too much data for single container. Multipart containers are not supported yet");
            }
        }

        //-------------------------------------------------------

        // Why this is private? 
        private const UInt32 DATA_LENGTH_STATIC = 12; // 4 bytes each: "PFBK", version (all 0s), number of files
        private UInt32 CalculateFirstFileOffset() {
            UInt32 result = DATA_LENGTH_STATIC;
            foreach (PackedFileIndexData fileEntry in FileList) {
                result += fileEntry.IndexEntrySize;
            }
            return result;
        }
    }

    public class PackedFileIndexData {
        // Fields
        public String FilePath { get; set; }
        public String OriginalFilePath { get; private set; }
        public UInt32 FileSize { get; set; }
        public UInt32 OriginalOffset { get; private set; }
        public UInt32 Offset { get; set; }
        public UInt32 IndexEntrySize => (UInt32)(DATA_LENGTH_STATIC + FilePath.Length);
        public Boolean UseExternalData => ExternalFilePath.Length != 0;
        public String ExternalFilePath { get; set; }

        // Constructors
        public PackedFileIndexData(String p_FilePath, UInt32 p_FileSize, UInt32 p_Offset) : this("", p_FilePath, p_FileSize, p_Offset) { }
        public PackedFileIndexData(String p_ExternalFilePath, String p_FilePath, UInt32 p_FileSize, UInt32 p_Offset = 0) {
            FilePath = p_FilePath;
            OriginalFilePath = p_FilePath;
            FileSize = p_FileSize;
            OriginalOffset = p_Offset;
            Offset = p_Offset;
            ExternalFilePath = p_ExternalFilePath;
        }

        //-------------------------------------------------------

        // Why this is private?
        private const UInt32 DATA_LENGTH_STATIC = 12; // 4 bytes each: path length, file size, offset
    }
}
