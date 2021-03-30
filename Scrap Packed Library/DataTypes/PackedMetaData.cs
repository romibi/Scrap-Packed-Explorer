using System;
using System.Collections.Generic;
using System.Text;

namespace ch.romibi.Scrap.Packed.PackerLib.DataTypes
{
    class PackedMetaData
    {
        public const string fileHeader = "BFPK";
        public UInt32 packedVersion { get; set; } // always 0 (not sure if really a version number)
        public List<PackedFileMetaData> fileList { get; set; }
    }

    class PackedFileMetaData
    {
        public string filePath { get; set; }
        public UInt32 fileSize { get; set; }
        public UInt32 originalOffset { get; set; } // todo: no setter?
    }
}
