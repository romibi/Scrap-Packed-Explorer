using CommandLine;
using CommandLine.Text;
using System;
using System.Collections.Generic;

namespace ch.romibi.Scrap.Packed.Explorer.Cli
{
    abstract class BaseOptions
    {
        [Value(0, Required = true, MetaName = "Packed file", HelpText = "The .packed file to use as basis")]
        public string packedFile { get; set; }
    }
    abstract class ModifyingOptions : BaseOptions
    {
        [Option('o', "outputPackedFile", Required = false, Default = "", HelpText = "Where to store the new .packed file. Modify input if not provided.")]
        public string outputPackedFile { get; set; }

        [Option('k', "keepBackup", Required = false, Default = false, HelpText = "Keep the backup file that gets created during saving even after successful processing.")]
        public bool keepBackup { get; set; }

        [Option("overwriteOldBackup", Required = false, Default = false, HelpText = "Allow overwriting existing .bak files")]
        public bool overwriteOldBackup { get; set; }
    }

    abstract class SearchOptions : BaseOptions {
        [Option('q', "searchString", Required = false, Default = "", HelpText = "A Search string to filter the output with")]
        public string searchString { get; set; }

        [Option('r', "regex", Required = false, Default = false, HelpText = "Defines if the search string is a regular expression")]
        public bool isRegex { get; set; }

        // todo: come up with better description
        // todo: change arguments style
        [Option('b', "match-beginning", Required = false, Default = false, HelpText = "Apply search query only to beginnng of the files path. By default applies everywhere")]
        public bool MatchBeginning { get; set; }

        [Option('f', "match-filename", Required = false, Default = false, HelpText = "Search only by files. By default search includes folders")]
        public bool MatchFilename { get; set; }
    }

    [Verb("add", HelpText = "Add file to the archive")]
    class AddOptions : ModifyingOptions
    {
        [Option('s', "sourcePath", Required = true, HelpText = "What file or folder to add to the .packed file")]
        public string sourcePath { get; set; }

        [Option('d', "packedPath", Required = false, Default = "", HelpText = "What path to put the source file(s) into")]
        public string packedPath { get; set; }
    }

    [Verb("remove", HelpText = "Remove a file from the archive")]
    class RemoveOptions : ModifyingOptions
    {
        [Option('d', "packedPath", Required = true, HelpText = "What path to remove from the archive")]
        public string packedPath { get; set; }
    }

    [Verb("rename", HelpText = "rename a file or folder inside the archive")]
    class RenameOptions : ModifyingOptions
    {
        [Option('s', "oldPackedPath", Required = true, Default = "/", HelpText = "What path to rename inside the archive")]
        public string oldPackedPath { get; set; }

        [Option('d', "newPackedPath", Required = true, HelpText = "The new path to use for the files to rename")]
        public string newPackedPath { get; set; }
    }

    [Verb("extract", HelpText = "Extract/unpack a file from the archive")]
    class ExtractOptions : BaseOptions
    {
        [Option('s', "packedPath", Required = false, Default = "", HelpText = "What path to extract from the archive")]
        public string packedPath { get; set; }

        [Option('d', "destinationPath", Required = true, HelpText = "The path to extract the files from the archive to")]
        public string destinationPath { get; set; }

        // todo add overwrite options
    }

    [Verb("list", HelpText = "list or search files and folders in the archive")]
    class ListOptions : SearchOptions
    {
        [Option('l', "outputStyle", Required = false, Default = OutputStyles.List, HelpText = "Output list (default) or tree view")]
        public OutputStyles outputStyle { get; set; }

        // todo: change short arguments
        [Option('s', "show-file-size", Required = false, Default = false, HelpText = "Show files sizes")]
        public bool ShowFileSize { get; set; }

        [Option('o', "show-file-offset", Required = false, Default = false, HelpText = "Show files offsets")]
        public bool ShowFileOffset { get; set; }
    }

    [Verb("cat", HelpText = "Print content of file inside of container")]
    class CatOptions : BaseOptions {
        [Option('s', "packedPath", Required = true, Default = "", HelpText = "What file to print")]
        public String PackedPath { get; set; }

        [Option('x', "asHex", Required = false, Default = false, HelpText = "Display file content as hex dump")]
        public Boolean AsHex { get; set; }
    }

    [Flags]
    public enum OutputStyles
    {
        None = 0x0,
        List = 0x1,
        Tree = 0x2,
        Name = 0x3
    }
}
