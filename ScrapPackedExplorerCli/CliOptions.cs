﻿using CommandLine;
using System;

namespace ch.romibi.Scrap.Packed.Explorer.Cli {
    internal abstract class BaseOptions {
        [Value(0, Required = true, MetaName = "Packed file", HelpText = "The .packed file to use as basis")]
        public string PackedFile { get; set; }
    }

    internal abstract class ModifyingOptions : BaseOptions {
        [Option('o', "output-packed-file", Required = false, Default = "", HelpText = "Where to store the new .packed file. Modify input if not provided.")]
        public string OutputPackedFile { get; set; }

        [Option('k', "keep-backup", Required = false, Default = false, HelpText = "Keep the backup file that gets created during saving even after successful processing.")]
        public bool KeepBackup { get; set; }

        [Option("overwrite-old-backup", Required = false, Default = false, HelpText = "Allow overwriting existing .bak files")]
        public bool OverwriteOldBackup { get; set; }
    }

    abstract class SearchOptions : BaseOptions {
        [Option('q', "search-string", Required = false, Default = "", HelpText = "A Search string to filter the output with")]
        public string SearchString { get; set; }

        [Option('r', "regex", Required = false, Default = false, HelpText = "Defines if the search string is a regular expression")]
        public bool IsRegex { get; set; }

        // todo: come up with better description
        // todo: change arguments style
        [Option('b', "match-beginning", Required = false, Default = false, HelpText = "Apply search query only to beginnng of the files path. By default applies everywhere")]
        public bool MatchBeginning { get; set; }

        [Option('f', "match-filename", Required = false, Default = false, HelpText = "Search only by files. By default search includes folders")]
        public bool MatchFilename { get; set; }
    }

    [Verb("add", HelpText = "Add file to the archive")]
    class AddOptions : ModifyingOptions {
        [Option('s', "source-path", Required = true, HelpText = "What file or folder to add to the .packed file")]
        public string SourcePath { get; set; }

        [Option('d', "packed-path", Required = false, Default = "", HelpText = "What path to put the source file(s) into")]
        public string PackedPath { get; set; }
    }

    [Verb("remove", HelpText = "Remove a file from the container")]
    internal class RemoveOptions : ModifyingOptions {
        [Option('d', "packed-path", Required = true, HelpText = "What path to remove from the container")]
        public string PackedPath { get; set; }
    }

    [Verb("rename", HelpText = "rename a file or folder inside the container")]
    internal class RenameOptions : ModifyingOptions {
        [Option('s', "old-packed-path", Required = true, Default = "/", HelpText = "What path to rename inside the container")]
        public string OldPackedPath { get; set; }

        [Option('d', "new-packed-path", Required = true, HelpText = "The new path to use for the files to rename")]
        public string NewPackedPath { get; set; }
    }

    [Verb("extract", HelpText = "Extract/unpack a file from the container")]
    internal class ExtractOptions : BaseOptions {
        [Option('s', "packed-path", Required = false, Default = "", HelpText = "What path to extract from the container")]
        public string PackedPath { get; set; }

        [Option('d', "destination-path", Required = true, HelpText = "The path to extract the files from the container to")]
        public string DestinationPath { get; set; }

        // todo add overwrite options
    }

    [Verb("list", HelpText = "List or search files and folders in the container")]
    internal class ListOptions : SearchOptions {
        [Option('l', "output-style", Required = false, Default = OutputStyles.List, HelpText = "Output list (default) or tree view")]
        public OutputStyles OutputStyle { get; set; }

        [Option('s', "show-file-size", Required = false, Default = false, HelpText = "Show files sizes")]
        public bool ShowFileSize { get; set; }

        [Option('o', "show-file-offset", Required = false, Default = false, HelpText = "Show files offsets")]
        public bool ShowFileOffset { get; set; }
        [Option("no-errors", Required = false, Default = false, HelpText = "Disable error messages")]
        public bool NoErrors { get; set; }
    }

    [Verb("cat", HelpText = "Print content of file inside of container")]
    class CatOptions : BaseOptions {
        [Option('s', "packed-path", Required = true, Default = "", HelpText = "What file to print")]
        public string PackedPath { get; set; }

        [Option('x', "as-hex", Required = false, Default = false, HelpText = "Display file content as hex dump")]
        public bool AsHex { get; set; }

        [Option('f', "byte-format", Required = false, Default = "X2", HelpText = "Format of printed bytes")]
        public string ByteFormat { get; set; }

        [Option('l', "line-format", Required = false, Default = "X8", HelpText = "Format of lines numbers")]
        public string LineFormat { get; set; }

        [Option('g', "bytes-per-group", Required = false, Default = (UInt16)2, HelpText = "How much bytes should print before printing space")]
        public UInt16 BytesPerGroup { get; set; }

        [Option('r', "groups-per-row", Required = false, Default = (UInt16)16, HelpText = "How much groups should print in one line")]
        public UInt16 GroupsPerRow { get; set; }

        [Option('p', "no-print-lines-numbers", Required = false, Default = false, HelpText = "Do not print lines numbers")]
        public bool NoPrintLinesNum { get; set; }
    }

    [Flags]
    public enum OutputStyles {
        List = 0x1,
        Tree = 0x2,
        Name = 0x4
    }
}
