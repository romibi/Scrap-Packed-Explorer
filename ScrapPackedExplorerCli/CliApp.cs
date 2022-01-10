using ch.romibi.Scrap.Packed.PackerLib;
using CommandLine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace ch.romibi.Scrap.Packed.Explorer.Cli
{
    public class CliApp
    {
        public int Run(string[] args)
        {
            return Parser.Default.ParseArguments<AddOptions, RemoveOptions, RenameOptions, ExtractOptions, ListOptions>(args)
                .MapResult(
                    (AddOptions options) => RunAdd(options),
                    (RemoveOptions options) => RunRemove(options),
                    (RenameOptions options) => RunRename(options),
                    (ExtractOptions options) => RunExtract(options),
                    (ListOptions options) => RunList(options),
                    errors => 1);
        }

        private int RunAdd(AddOptions options)
        {
            ScrapPackedFile packedFile;
            try { packedFile = new ScrapPackedFile(options.packedFile); }
            catch (Exception ex) { return Error(ex); }

            try { packedFile.Add(options.sourcePath, options.packedPath); }
            catch (Exception ex) { return Error(ex); }

            try { packedFile.SaveToFile(options.outputPackedFile); }
            catch (Exception ex) { return Error(ex); }

            return 0;
        }

        private int RunRemove(RemoveOptions options)
        {
            ScrapPackedFile packedFile;
            try { packedFile = new ScrapPackedFile(options.packedFile); }
            catch (Exception ex) { return Error(ex); }

            try { packedFile.Remove(options.packedPath); }
            catch (Exception ex) { return Error(ex); }

            try { packedFile.SaveToFile(options.outputPackedFile); }
            catch (Exception ex) { return Error(ex); }

            return 0;
        }

        private int RunRename(RenameOptions options)
        {
            ScrapPackedFile packedFile;
            try { packedFile = new ScrapPackedFile(options.packedFile); }
            catch (Exception ex) { return Error(ex); }
                        
            try { packedFile.Rename(options.oldPackedPath, options.newPackedPath); }
            catch (Exception ex) { return Error(ex); }

            try { packedFile.SaveToFile(options.outputPackedFile); }
            catch (Exception ex) { return Error(ex); }

            return 0;
        }

        private int RunExtract(ExtractOptions options)
        {
            ScrapPackedFile packedFile;
            try { packedFile = new ScrapPackedFile(options.packedFile); }
            catch (Exception ex) { return Error(ex); }

            try { packedFile.Extract(options.packedPath, options.destinationPath); }
            catch (Exception ex) { return Error(ex); }

            return 0;
        }

        private int RunList(ListOptions options)
        {
            ScrapPackedFile packedFile;
            try { packedFile = new ScrapPackedFile(options.packedFile); }
            catch (Exception ex) { return Error(ex); }

            List<string> fileNames = packedFile.GetFileNames();

            if (fileNames.Count == 0)
                Console.WriteLine($"{options.packedFile} is empty.");
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
                
                List<string> filtered = fileNames.Where( f =>
                {
                    if (options.MatchFilename)
                    {
                        var pathSplitted = f.Split('/');
                        f = pathSplitted[pathSplitted.Length - 1];
                    }

                    return rg.IsMatch(f);
                }).ToList();

                if (filtered.Count == 0)
                    Console.WriteLine($"Could not find anything by query '{options.searchString}' in '{options.packedFile}'");

                foreach (var fileName in filtered)
                    Console.WriteLine(fileName);
            }
            
            // Todo: implement RunList output styles
            return 0;
        }

        // This just to make code "prettier". Multi-line `catch` with one-line `try` is kinda ugly
        private int Error(Exception ex)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");
            return 1;
        }
    }
}
