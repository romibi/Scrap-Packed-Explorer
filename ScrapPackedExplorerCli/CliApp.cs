using ch.romibi.Scrap.Packed.PackerLib;
using CommandLine;
using CommandLine.Text;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace ch.romibi.Scrap.Packed.Explorer.Cli
{
    public class CliApp
    {
        public int Run(string[] args)
        {
            var parser = new Parser(with =>
            {
                with.HelpWriter = null;
                with.CaseInsensitiveEnumValues = true;
                with.CaseSensitive = false;
            });

            // Default parsing with verb as first arg
            var result = parser.ParseArguments<BaseOptions, AddOptions, RemoveOptions, RenameOptions, ExtractOptions, ListOptions>(args);
            return result.MapResult(
                (AddOptions     options) => RunAdd(options),
                (RemoveOptions  options) => RunRemove(options),
                (RenameOptions  options) => RunRename(options),
                (ExtractOptions options) => RunExtract(options),
                (ListOptions    options) => RunList(options),
                errors => {
                    foreach (Error error in errors)
                        if (error.Tag == ErrorType.BadVerbSelectedError)
                            return ParseFirstArgNotVerb(args, parser); // if first arg is not verb it is must be PackedPath
                    return DisplayHelp(result, errors);
                }
            );
        }

        private int ParseFirstArgNotVerb(string[] args, Parser parser)
        {
            // if no verb specified print help message
            if (args.Length == 1)
            {
                List<string> _args = new List<string>(args);
                _args.Add("help");
                args = _args.ToArray();
            }

            // Just make verb firts lol
            if (!new List<string>() { "help", "--help", "version", "--version" }.Contains(args[0]))
            {
                var temp = args[0];
                args[0] = args[1];
                args[1] = temp;
            }

            var result = parser.ParseArguments<BaseOptions, AddOptions, RemoveOptions, RenameOptions, ExtractOptions, ListOptions>(args);
            return result.MapResult(
                (AddOptions options) => RunAdd(options),
                (RemoveOptions options) => RunRemove(options),
                (RenameOptions options) => RunRename(options),
                (ExtractOptions options) => RunExtract(options),
                (ListOptions options) => RunList(options),
                errors => DisplayHelp(result, errors)
            );
        }

        private static int DisplayHelp<T>(ParserResult<T> result, IEnumerable<Error> errors)
        {
            string usage = "\r\nUSAGE: " +
                "\r\n  ScrapPackedExplorerCli.exe <path-to-packed-file> <subcommand> <options>\r\n" +
                "\r\nEXAMPLE: " +
                "\r\n  ScrapPackedExplorerCli.exe example.packed list -osq filename.txt -l tree\r\n" +
                "\r\nOPTIONS:";

            var helpText = HelpText.AutoBuild(result, h =>
            {
                h.AddEnumValuesToHelpText = true;
                h.AutoHelp = true;
                h.AddPreOptionsText(usage);
                h.OptionComparison = orderOnValues;
                h.MaximumDisplayWidth = 250;
                h.AdditionalNewLineAfterOption = false;
                return h;
            });

            Console.Error.WriteLine(helpText);
            return 1;
        }

        private static Comparison<ComparableOption> orderOnValues = (ComparableOption attr1, ComparableOption attr2) =>
        {
            if (attr1.IsValue)
                return -1;
            else
                return 1;
        };

        private int RunAdd(AddOptions options)
        {
            try {
                // TODO: sanitize input
                ScrapPackedFile packedFile = new ScrapPackedFile(options.packedFile); 
                packedFile.Add(options.sourcePath, options.packedPath);
                packedFile.SaveToFile(options.outputPackedFile);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error: {ex.Message}");
                return 1;
            }

            return 0;
        }

        private int RunRemove(RemoveOptions options)
        {
            try {
                ScrapPackedFile packedFile = new ScrapPackedFile(options.packedFile);
                packedFile.Remove(options.packedPath); 
                packedFile.SaveToFile(options.outputPackedFile); 
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error: {ex.Message}");
                return 1;
            }

            return 0;
        }

        private int RunRename(RenameOptions options)
        {
            try
            {
                ScrapPackedFile packedFile = new ScrapPackedFile(options.packedFile);
                packedFile.Rename(options.oldPackedPath, options.newPackedPath);
                packedFile.SaveToFile(options.outputPackedFile);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error: {ex.Message}");
                return 1;
            }

            return 0;
        }

        private int RunExtract(ExtractOptions options)
        {
            try {
                ScrapPackedFile packedFile = new ScrapPackedFile(options.packedFile);
                packedFile.Extract(options.packedPath, options.destinationPath);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error: {ex.Message}");
                return 1;
            }

            return 0;
        }

        private int RunList(ListOptions options)
        {
            try
            {
                ScrapPackedFile packedFile = new ScrapPackedFile(options.packedFile);
                List<string> FileList = packedFile.GetFileNames();
                FileList.Sort();

                if (FileList.Count == 0)
                    Console.WriteLine($"'{options.packedFile}' is empty.");
                else
                {
                    string query = options.searchString;
                    if (!options.isRegex)
                        query = Regex.Escape(query);

                    query = query.Replace("/", @"\/");
                    query = query.Replace("\\*", ".*");
                    query = query.Replace("\\?", ".");

                    if (options.MatchBeginning)
                        query = "^" + query;

                    Regex rg = new Regex(query);

                    bool found = false;
                    foreach (var File in FileList)
                    {
                        OutputStyles Styles = options.outputStyle;

                        var FileData = File.Split("\t");
                        string FilePath = Path.GetDirectoryName(FileData[0]).Replace("\\", "/");
                        string FileName = Path.GetFileName(FileData[0]);
                        string FileSize = FileData[1];
                        string FileOffset = FileData[2];

                        if (FilePath != "")
                            FilePath += "/";

                        if (!rg.IsMatch(options.MatchFilename ? FileName : FilePath + FileName))
                            continue;
                        found = true;

                        string Output = FileName;

                        if (Styles != OutputStyles.Name)
                            Output = FilePath + Output;

                        if (options.ShowFileSize)
                            Output += "\t" + FileSize;

                        if (options.ShowFileOffset)
                            Output += "\t" + FileOffset;

                        Console.WriteLine(Output);
                    }

                    if (!found)
                        Console.WriteLine($"Could not find anything by query '{options.searchString}' in '{options.packedFile}'");
                }
                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error: {ex.Message}");
                return 1;
            }
        }
    }
}
