using ch.romibi.Scrap.Packed.PackerLib;
using CommandLine;
using System;
using System.Collections.Generic;
using System.IO;

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
            {
                Console.WriteLine($"{options.packedFile} is empty.");
            }
            else
            {
                foreach (var fileName in fileNames)
                {
                    Console.WriteLine(fileName);
                }
            }
            
            // Todo: implement RunList output styles
            return 0;
        }

        // This just to make code "preetier".
        private int Error(Exception ex)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");
            return 1;
        }
    }
}
