using ch.romibi.Scrap.Packed.PackerLib;
using ch.romibi.Scrap.Packed.PackerLib.DataTypes;
using CommandLine;
using CommandLine.Text;
using MS.WindowsAPICodePack.Internal;

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
            });

            // Default parsing with verb as first arg
            var result = parser.ParseArguments<BaseOptions, AddOptions, RemoveOptions, RenameOptions, ExtractOptions, ListOptions, CatOptions>(args);
            return result.MapResult(
                (AddOptions     options) => RunAdd(options),
                (RemoveOptions  options) => RunRemove(options),
                (RenameOptions  options) => RunRename(options),
                (ExtractOptions options) => RunExtract(options),
                (ListOptions    options) => RunList(options),
                (CatOptions     p_options) => RunCat(p_options),
                errors => ParseFirstArgNotVerb(args, parser) // if first arg is not verb it is must be PackedPath
            );
        }

        // todo: refactor this. Kinda ugly 
        private int ParseFirstArgNotVerb(string[] args, Parser parser)
        {
            // if no verb specified print help message
            if (args.Length == 1) {
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

            var result = parser.ParseArguments<BaseOptions, AddOptions, RemoveOptions, RenameOptions, ExtractOptions, ListOptions, CatOptions>(args);
            return result.MapResult(
               (AddOptions options) => RunAdd(options),
               (RemoveOptions options) => RunRemove(options),
               (RenameOptions options) => RunRename(options),
               (ExtractOptions options) => RunExtract(options),
               (ListOptions options) => RunList(options),
               (CatOptions p_options) => RunCat(p_options),
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
                ScrapPackedFile packedFile = new ScrapPackedFile(options.packedFile, true); 
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
    
        private static Int32 RunCat(CatOptions p_Options) {
            try {
                ScrapPackedFile packedFile = new(p_Options.packedFile);
                PackedFileIndexData fileData = packedFile.GetFileIndexDataForFile(p_Options.PackedPath);

                FileStream fsPacked = new(p_Options.packedFile, FileMode.Open);
                try {
                    Byte[] readBytes = new Byte[fileData.FileSize];

                    fsPacked.Seek(fileData.OriginalOffset, SeekOrigin.Begin);
                    fsPacked.Read(readBytes, 0, (Int32)fileData.FileSize);

                    if (p_Options.AsHex) 
                        PrintAsHex(readBytes);
                    else {
                        String fileContnet = System.Text.Encoding.Default.GetString(readBytes);
                        Console.WriteLine(fileContnet);
                    }
                }
                finally {
                    fsPacked.Close();
                }
            }
            catch (Exception ex) {
                Console.Error.WriteLine($"Error: {ex.Message}");
                return 1;
            }

            return 0;
        }

        private static void PrintAsHex(Byte[] p_Bytes, String p_Format = "X", UInt16 p_BytesPerGroup = 2, UInt16 p_BytesPerLine = 16, Boolean p_PrintLinesNum = true) {
            for (UInt32 i = 0; i < p_Bytes.Length; i++) {
                if (p_PrintLinesNum && i % p_BytesPerLine == 0)
                    Console.Write(i.ToString(p_Format + "8") + " ");

                Console.Write(p_Bytes[i].ToString(p_Format + p_BytesPerGroup));

                if ((i + 1) % p_BytesPerGroup == 0)
                    Console.Write(" ");

                if ((i + 1) % p_BytesPerLine == 0)
                    Console.Write("\r\n");
            }
            Console.Write("\r\n");
        }
    }
}
