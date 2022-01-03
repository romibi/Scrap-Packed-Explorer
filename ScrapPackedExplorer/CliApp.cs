using ch.romibi.Scrap.Packed.PackerLib;
using CommandLine;
using System;
using System.Collections.Generic;

namespace ch.romibi.Scrap.Packed.Explorer
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
            var packedFile = new ScrapPackedFile(options.packedFile);
            packedFile.Add(options.sourcePath, options.packedPath);
            packedFile.SaveToFile(options.outputPackedFile);
            return 0;
        }

        private int RunRemove(RemoveOptions options)
        {
            var packedFile = new ScrapPackedFile(options.packedFile);
            packedFile.Remove(options.packedPath);
            packedFile.SaveToFile(options.outputPackedFile);
            return 0;
        }

        private int RunRename(RenameOptions options)
        {
            var packedFile = new ScrapPackedFile(options.packedFile);
            packedFile.Rename(options.oldPackedPath, options.newPackedPath);
            packedFile.SaveToFile(options.outputPackedFile);
            return 0;
        }

        private int RunExtract(ExtractOptions options)
        {
            var packedFile = new ScrapPackedFile(options.packedFile);
            packedFile.Extract(options.packedPath, options.destinationPath);
            return 0;
        }

        private int RunList(ListOptions options)
        {
            var packedFile = new ScrapPackedFile(options.packedFile);
            List<string> fileNames = packedFile.GetFileNames();

            foreach (var fileName in fileNames)
            {
                Console.WriteLine(fileName);
            }
            // Todo: implement RunList output styles
            return 0;
        }

    }
}
