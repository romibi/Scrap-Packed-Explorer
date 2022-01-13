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
            var result = new Parser(with =>
            {
                with.HelpWriter = null;
                with.CaseInsensitiveEnumValues = true;
            }).ParseArguments<AddOptions, RemoveOptions, RenameOptions, ExtractOptions, ListOptions>(args);

            var helpText = HelpText.AutoBuild(result, h =>
            {
                h.AddEnumValuesToHelpText = true;
                return h;
            }, e => e);

            return result.MapResult(
                    (AddOptions options) => RunAdd(options),
                    (RemoveOptions options) => RunRemove(options),
                    (RenameOptions options) => RunRename(options),
                    (ExtractOptions options) => RunExtract(options),
                    (ListOptions options) => RunList(options),
                    errors => {
                        Console.Error.WriteLine(helpText);
                        return 1;
                    });
        }

        private int RunAdd(AddOptions options)
        {
            try {
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

                    if (options.StartsWith)
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
                            Output += "   " + FileSize;

                        if (options.ShowFileOffset)
                            Output += "   " + FileOffset;

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
