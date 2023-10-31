using ch.romibi.Scrap.Packed.PackerLib;
using ch.romibi.Scrap.Packed.PackerLib.DataTypes;
using CommandLine;
using CommandLine.Text;

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace ch.romibi.Scrap.Packed.Explorer.Cli {
    public class CliApp {
        public static int Run(string[] p_Args) {
            Parser parser = new(p_With => {
                p_With.HelpWriter = null;
                p_With.CaseInsensitiveEnumValues = true;
                p_With.CaseSensitive = false;
            });

            // Default parsing with verb as first arg
            ParserResult<object> result = parser.ParseArguments<BaseOptions, AddOptions, RemoveOptions, RenameOptions, ExtractOptions, ListOptions, CatOptions>(p_Args);
            return result.MapResult(
                (AddOptions p_Options) => RunAdd(p_Options),
                (RemoveOptions p_Options) => RunRemove(p_Options),
                (RenameOptions p_Options) => RunRename(p_Options),
                (ExtractOptions p_Options) => RunExtract(p_Options),
                (ListOptions p_Options) => RunList(p_Options),
                (CatOptions p_Options) => RunCat(p_Options),
                p_Errors => {
                    foreach (Error error in p_Errors)
                        if (error.Tag == ErrorType.BadVerbSelectedError)
                            return ParseFirstArgNotVerb(p_Args, parser); // if first arg is not verb it is must be PackedPath
                    return DisplayHelp(result);
                }
            );
        }

        // Main functionality 
        private static int RunAdd(AddOptions p_Options) {
            try {
                // TODO: sanitize input
                ScrapPackedFile packedFile = new(p_Options.PackedFile, true);
                packedFile.Add(p_Options.SourcePath, p_Options.PackedPath);
                packedFile.SaveToFile(p_Options.OutputPackedFile, p_Options.KeepBackup, p_Options.OverwriteOldBackup);
            } catch (Exception ex) {
                Console.Error.WriteLine($"Error: {ex.Message}");
                return 1;
            }

            return 0;
        }
        private static int RunRemove(RemoveOptions p_Options) {
            try {
                ScrapPackedFile packedFile = new(p_Options.PackedFile);
                packedFile.Remove(p_Options.PackedPath);
                packedFile.SaveToFile(p_Options.OutputPackedFile, p_Options.KeepBackup, p_Options.OverwriteOldBackup);
            } catch (Exception ex) {
                Console.Error.WriteLine($"Error: {ex.Message}");
                return 1;
            }

            return 0;
        }
        private static int RunRename(RenameOptions p_Options) {
            try {
                ScrapPackedFile packedFile = new(p_Options.PackedFile);
                packedFile.Rename(p_Options.OldPackedPath, p_Options.NewPackedPath);
                packedFile.SaveToFile(p_Options.OutputPackedFile, p_Options.KeepBackup, p_Options.OverwriteOldBackup);
            } catch (Exception ex) {
                Console.Error.WriteLine($"Error: {ex.Message}");
                return 1;
            }

            return 0;
        }
        private static int RunExtract(ExtractOptions p_Options) {
            try {
                ScrapPackedFile packedFile = new(p_Options.PackedFile);
                packedFile.Extract(p_Options.PackedPath, p_Options.DestinationPath);
            } catch (Exception ex) {
                Console.Error.WriteLine($"Error: {ex.Message}");
                return 1;
            }

            return 0;
        }
        private static int RunList(ListOptions p_Options) {
            try {
                ScrapPackedFile packedFile = new(p_Options.PackedFile);
                List<PackedFileIndexData> FileList = packedFile.GetFileIndexDataList();
                FileList.Sort((x, y) => x.FilePath.CompareTo(y.FilePath));

                if (FileList.Count == 0) {
                    if (!p_Options.NoErrors) {
                        Console.Error.WriteLine($"'{p_Options.PackedFile}' is empty.");
                    }
                    return 1;
                }

                List<PackedFileIndexData> SearchedList = Search(FileList, p_Options.SearchString, p_Options.IsRegex, p_Options.MatchBeginning, p_Options.MatchFilename);

                if (SearchedList.Count == 0) {
                    if (!p_Options.NoErrors) {
                        Console.Error.WriteLine($"Could not find anything by query '{p_Options.SearchString}' in '{p_Options.PackedFile}'");
                    }
                    return 1;
                }

                if (p_Options.OutputStyle == OutputStyles.Tree) {
                    OutputListAsTree(SearchedList, p_Options);
                } else {
                    OutputList(SearchedList, p_Options);
                }
                return 0;
            } catch (Exception ex) {
                if (!p_Options.NoErrors) {
                    Console.Error.WriteLine($"Error: {ex.Message}");
                }
                return 1;
            }
        }

        private static void OutputList(List<PackedFileIndexData> p_List, ListOptions p_Options) {
            foreach (var File in p_List) {
                string FilePath = Path.GetDirectoryName(File.FilePath).Replace("\\", "/");
                string FileName = Path.GetFileName(File.FilePath);

                if (FilePath != "")
                    FilePath += "/";

                string Output = FileName;

                if (p_Options.OutputStyle != OutputStyles.Name)
                    Output = FilePath + Output;

                if (p_Options.ShowFileSize)
                    Output += "\tSize: " + File.FileSize.ToString();

                if (p_Options.ShowFileOffset)
                    Output += "\tOffset: " + File.OriginalOffset.ToString();

                Console.WriteLine(Output);
            }
        }
        private static void OutputListAsTree(List<PackedFileIndexData> p_List, ListOptions p_Options) {
            ScrapTreeEntry root = new ScrapTreeEntry(null) { Name = p_Options.PackedFile };

            foreach (PackedFileIndexData file in p_List) {
                root.AddFileData(file);
            }
            root.Sort();

            OutputTreeListEntry("", root);
        }

        private static void OutputTreeListEntry(string p_CurrentTreePrefix, ScrapTreeEntry p_Entry) {
            const string TREE_PREFIX_FILE = "│   ";
            const string TREE_PREFIX_FOLDER = "├───";
            const string TREE_PREFIX_FOLDER_LAST = "└───";
            const string TREE_PREFIX_FILE_IN_LAST_FOLDER = "    ";
            const int TREE_PREFIX_LENGTH = 4;

            var printPrefix = p_CurrentTreePrefix;
            if (p_Entry.IsDirectory) {
                // Add "empty" line before directories and convert file prefix to tree prefix
                if (printPrefix.EndsWith(TREE_PREFIX_FILE)) {
                    // after this directory there will be another directory: we can just print the current tree prefix once
                    // and replace the last TREE_PREFIX_FILE with TREE_PREFIX_FOLDER for printing the entry name
                    Console.WriteLine(p_CurrentTreePrefix);
                    printPrefix = printPrefix.Substring(0, printPrefix.Length - TREE_PREFIX_LENGTH) + TREE_PREFIX_FOLDER;
                } else if (printPrefix.EndsWith(TREE_PREFIX_FILE_IN_LAST_FOLDER)) {
                    // after this directory there will be no other directory in this nexting level: we have to convert TREE_PREFIX_FILE_IN_LAST_FOLDER to TREE_PREFIX_FILE
                    // for printing an "empty line"
                    // and replace the last TREE_PREFIX_FILE_IN_LAST_FOLDER with TREE_PREFIX_FOLDER_LAST for printing the entry name
                    Console.WriteLine(printPrefix.Substring(0, printPrefix.Length - TREE_PREFIX_LENGTH) + TREE_PREFIX_FILE);
                    printPrefix = printPrefix.Substring(0, printPrefix.Length - 4) + TREE_PREFIX_FOLDER_LAST;
                }
            }
            Console.WriteLine(printPrefix + p_Entry.Name);

            List<ScrapTreeEntry> files = new();
            List<ScrapTreeEntry> folders = new();

            // split files and folders
            foreach (var child in p_Entry.Items) {
                if (child.IsFile) {
                    files.Add(child);
                } else {
                    folders.Add(child);
                }
            }

            var newFilePrefix = folders.Count == 0 ? p_CurrentTreePrefix + TREE_PREFIX_FILE_IN_LAST_FOLDER : p_CurrentTreePrefix + TREE_PREFIX_FILE;

            // first output files
            foreach (var file in files) {
                OutputTreeListEntry(newFilePrefix, file);
            }

            // then output folder, but use different tree prefix for the last one
            if (folders.Count > 0) {
                for (int i = 0; i < folders.Count; i++) {
                    if (i < folders.Count - 1) {
                        OutputTreeListEntry(p_CurrentTreePrefix + TREE_PREFIX_FILE, folders[i]);
                    } else {
                        OutputTreeListEntry(p_CurrentTreePrefix + TREE_PREFIX_FILE_IN_LAST_FOLDER, folders[i]);
                    }
                }
            }
        }

        private static int RunCat(CatOptions p_Options) {
            try {
                ScrapPackedFile packedFile = new(p_Options.PackedFile);
                PackedFileIndexData fileData = null;
                try {
                    fileData = packedFile.GetFileIndexDataForFile(p_Options.PackedPath);
                } catch {
                    throw new FileNotFoundException($"File '{p_Options.PackedPath}' dose not exsits in '{p_Options.PackedFile}'");
                }

                FileStream fsPacked = new(p_Options.PackedFile, FileMode.Open);
                try {
                    byte[] readBytes = new byte[fileData.FileSize];

                    fsPacked.Seek(fileData.OriginalOffset, SeekOrigin.Begin);
                    fsPacked.Read(readBytes, 0, (int)fileData.FileSize);

                    if (p_Options.AsHex)
                        PrintAsHex(readBytes, p_Options.ByteFormat, p_Options.LineFormat, p_Options.BytesPerGroup, p_Options.GroupsPerRow, p_Options.NoPrintLinesNum);
                    else {
                        var fileContnet = System.Text.Encoding.Default.GetString(readBytes);
                        Console.WriteLine(fileContnet);
                    }
                } finally {
                    fsPacked.Close();
                }
            } catch (Exception ex) {
                Console.Error.WriteLine($"Error: {ex.Message}");
                return 1;
            }

            return 0;
        }

        private static List<PackedFileIndexData> Search(List<PackedFileIndexData> p_FileList, string p_Query, bool p_IsRegex, bool p_MatchBeginning, bool p_MatchFilename) {
            List<PackedFileIndexData> result = new();

            var query = p_Query;
            if (!p_IsRegex)
                query = Regex.Escape(query);

            query = query.Replace("/", @"\/");
            query = query.Replace("\\*", ".*");
            query = query.Replace("\\?", ".");

            if (p_MatchBeginning)
                query = "^" + query;

            Regex rg = new(query);

            foreach (var File in p_FileList) {
                var FilePath = Path.GetDirectoryName(File.FilePath).Replace("\\", "/");
                var FileName = Path.GetFileName(File.FilePath);

                if (FilePath != "")
                    FilePath += "/";

                if (rg.IsMatch(p_MatchFilename ? FileName : FilePath + FileName))
                    result.Add(File);
            }

            return result;
        }

        private static void PrintAsHex(byte[] p_Bytes, string p_ByteFormat = "X2", string p_LineFormat = "X8", ushort p_BytesPerGroup = 2, ushort p_GroupsPerLine = 16, bool p_NoPrintLinesNum = false) {
            for (ushort i = 0; i < p_Bytes.Length; i++) {
                if (!p_NoPrintLinesNum && i % p_GroupsPerLine == 0)
                    Console.Write(i.ToString(p_LineFormat) + " ");

                Console.Write(p_Bytes[i].ToString(p_ByteFormat));

                if ((i + 1) % p_BytesPerGroup == 0)
                    Console.Write(" ");

                if ((i + 1) % p_GroupsPerLine == 0)
                    Console.Write("\r\n");
            }
            Console.Write("\r\n");
        }

        // Arguments processing stuff
        private static readonly Comparison<ComparableOption> OrderOnValues = (ComparableOption p_Attr1, ComparableOption p_Attr2) => {
            if (p_Attr1.IsValue)
                return -1;
            else
                return 1;
        };
        private static int ParseFirstArgNotVerb(string[] p_Args, Parser p_Parser) {
            // if no verb specified print help message
            if (p_Args.Length == 1) {
                List<string> _args = new(p_Args) {
                    "help"
                };
                p_Args = _args.ToArray();
            }

            // Just make verb firts lol
            if (!new List<string>() { "help", "--help", "version", "--version" }.Contains(p_Args[0])) {
                string temp = p_Args[0];
                p_Args[0] = p_Args[1];
                p_Args[1] = temp;
            }

            ParserResult<object> result = p_Parser.ParseArguments<BaseOptions, AddOptions, RemoveOptions, RenameOptions, ExtractOptions, ListOptions, CatOptions>(p_Args);
            return result.MapResult(
                (AddOptions p_Options) => RunAdd(p_Options),
                (RemoveOptions p_Options) => RunRemove(p_Options),
                (RenameOptions p_Options) => RunRename(p_Options),
                (ExtractOptions p_Options) => RunExtract(p_Options),
                (ListOptions p_Options) => RunList(p_Options),
                (CatOptions p_Options) => RunCat(p_Options),
                p_Errors => DisplayHelp(result)
            );
        }
        private static int DisplayHelp<T>(ParserResult<T> p_Result) {
            string usage = "USAGE: " +
                "\r\n  ScrapPackedExplorerCli.exe <path-to-packed-file> <subcommand> <options>\r\n" +
                "EXAMPLE: " +
                "\r\n  ScrapPackedExplorerCli.exe example.packed list -osq filename.txt -l tree";

            HelpText helpText = HelpText.AutoBuild(p_Result, p_HelpText => {
                p_HelpText.AddEnumValuesToHelpText = true;
                p_HelpText.AutoHelp = true;
                p_HelpText.AddPreOptionsText(usage);
                p_HelpText.OptionComparison = OrderOnValues;
                p_HelpText.MaximumDisplayWidth = 250;
                p_HelpText.AdditionalNewLineAfterOption = false;
                return p_HelpText;
            });

            Console.Error.WriteLine(helpText);
            return 1;
        }
    }
}
