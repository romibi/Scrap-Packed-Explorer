using ch.romibi.Scrap.Packed.PackerLib;
using ch.romibi.Scrap.Packed.PackerLib.DataTypes;
using CommandLine;
using CommandLine.Text;
using MS.WindowsAPICodePack.Internal;

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
                packedFile.SaveToFile(p_Options.OutputPackedFile);
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
                packedFile.SaveToFile(p_Options.OutputPackedFile);
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
                packedFile.SaveToFile(p_Options.OutputPackedFile);
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
                List<string> FileList = packedFile.GetFileNames();
                FileList.Sort();

                if (FileList.Count == 0)
                    Console.WriteLine($"'{p_Options.PackedFile}' is empty.");
                else {
                    List<string> SearchedList = Search(FileList, p_Options.SearchString, p_Options.IsRegex, p_Options.MatchBeginning, p_Options.MatchFilename);

                    if (SearchedList.Count == 0)
                        Console.WriteLine($"Could not find anything by query '{p_Options.SearchString}' in '{p_Options.PackedFile}'");

                    foreach (var File in SearchedList) {
                        OutputStyles Styles = p_Options.OutputStyle;

                        string[] FileData = File.Split("\t");
                        string FilePath = Path.GetDirectoryName(FileData[0]).Replace("\\", "/");
                        string FileName = Path.GetFileName(FileData[0]);
                        string FileSize = FileData[1];
                        string FileOffset = FileData[2];

                        if (FilePath != "")
                            FilePath += "/";

                        string Output = FileName;

                        if (Styles != OutputStyles.Name)
                            Output = FilePath + Output;

                        if (p_Options.ShowFileSize)
                            Output += "\t" + FileSize;

                        if (p_Options.ShowFileOffset)
                            Output += "\t" + FileOffset;

                        Console.WriteLine(Output);
                    }
                }
                return 0;
            } catch (Exception ex) {
                Console.Error.WriteLine($"Error: {ex.Message}");
                return 1;
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
                    fsPacked.Read(readBytes, 0, (Int32)fileData.FileSize);

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

        private static List<string> Search(List<string> p_FileList, string p_Query, bool p_IsRegex, bool p_MatchBeginning, bool p_MatchFilename) {
            List<string> result = new();

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
                string[] FileData = File.Split("\t");
                var FilePath = Path.GetDirectoryName(FileData[0]).Replace("\\", "/");
                var FileName = Path.GetFileName(FileData[0]);
                var FileSize = FileData[1];
                var FileOffset = FileData[2];

                if (FilePath != "")
                    FilePath += "/";

                if (rg.IsMatch(p_MatchFilename ? FileName : FilePath + FileName))
                    result.Add($"{FilePath}{FileName}\t{FileSize}\t{FileOffset}");
            }

            return result;
        }

        private static void PrintAsHex(byte[] p_Bytes, string p_ByteFormat = "X2", string p_LineFormat = "X8", UInt16 p_BytesPerGroup = 2, UInt16 p_GroupsPerLine = 16, bool p_NoPrintLinesNum = false) {
            for (UInt32 i = 0; i < p_Bytes.Length; i++) {
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
