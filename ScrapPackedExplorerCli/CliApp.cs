using ch.romibi.Scrap.Packed.PackerLib;
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
            ParserResult<object> result = parser.ParseArguments<BaseOptions, AddOptions, RemoveOptions, RenameOptions, ExtractOptions, ListOptions>(p_Args);
            return result.MapResult(
                (AddOptions p_Options) => RunAdd(p_Options),
                (RemoveOptions p_Options) => RunRemove(p_Options),
                (RenameOptions p_Options) => RunRename(p_Options),
                (ExtractOptions p_Options) => RunExtract(p_Options),
                (ListOptions p_Options) => RunList(p_Options),
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
                    string query = p_Options.SearchString;
                    if (!p_Options.IsRegex)
                        query = Regex.Escape(query);

                    query = query.Replace("/", @"\/");
                    query = query.Replace("\\*", ".*");
                    query = query.Replace("\\?", ".");

                    if (p_Options.MatchBeginning)
                        query = "^" + query;

                    Regex rg = new(query);

                    bool found = false;
                    foreach (string File in FileList) {
                        OutputStyles Styles = p_Options.OutputStyle;

                        string[] FileData = File.Split("\t");
                        string FilePath = Path.GetDirectoryName(FileData[0]).Replace("\\", "/");
                        string FileName = Path.GetFileName(FileData[0]);
                        string FileSize = FileData[1];
                        string FileOffset = FileData[2];

                        if (FilePath != "")
                            FilePath += "/";

                        if (!rg.IsMatch(p_Options.MatchFilename ? FileName : FilePath + FileName))
                            continue;
                        found = true;

                        string Output = FileName;

                        if (Styles != OutputStyles.Name)
                            Output = FilePath + Output;

                        if (p_Options.ShowFileSize)
                            Output += "\t" + FileSize;

                        if (p_Options.ShowFileOffset)
                            Output += "\t" + FileOffset;

                        Console.WriteLine(Output);
                    }

                    if (!found)
                        Console.WriteLine($"Could not find anything by query '{p_Options.SearchString}' in '{p_Options.PackedFile}'");
                }
                return 0;
            } catch (Exception ex) {
                Console.Error.WriteLine($"Error: {ex.Message}");
                return 1;
            }
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

            ParserResult<object> result = p_Parser.ParseArguments<BaseOptions, AddOptions, RemoveOptions, RenameOptions, ExtractOptions, ListOptions>(p_Args);
            return result.MapResult(
                (AddOptions p_Options) => RunAdd(p_Options),
                (RemoveOptions p_Options) => RunRemove(p_Options),
                (RenameOptions p_Options) => RunRename(p_Options),
                (ExtractOptions p_Options) => RunExtract(p_Options),
                (ListOptions p_Options) => RunList(p_Options),
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
