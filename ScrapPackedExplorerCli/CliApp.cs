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
            string pakedFilePath    = options.packedFile;
            string sourcePath       = options.sourcePath;
            string outputPackedFile = options.outputPackedFile;

            if (!File.Exists(pakedFilePath))
            {
                Console.Error.WriteLine("Error: file \"{0}\" is not exists", pakedFilePath);
                Environment.Exit(1);
            }
            if (!File.Exists(sourcePath))
            {
                Console.Error.WriteLine("Error: file \"{0}\" is not exists", sourcePath);
                Environment.Exit(1);
            }

            var packedFile = new ScrapPackedFile(pakedFilePath);
            packedFile.Add(sourcePath, pakedFilePath);

            if (!packedFile.SaveToFile(outputPackedFile))
            {
                // todo: make tests of this 
                Console.Error.WriteLine("Error: unable to save \"{0}\". Check if you provided valid -o argument", outputPackedFile);
                Environment.Exit(1);
            }
            return 0;
        }

        private int RunRemove(RemoveOptions options)
        {
            string pakedFilePath    = options.packedFile;
            string packedPath       = options.packedPath;
            string outputPackedFile = options.outputPackedFile;

            var packedFile = new ScrapPackedFile(pakedFilePath);
            packedFile.Remove(packedPath);

            if (!packedFile.SaveToFile(outputPackedFile))
            {
                // todo: make tests of this 
                Console.Error.WriteLine("Error: unable to save \"{0}\". Check if you provided valid -o argument", outputPackedFile);
                Environment.Exit(1);
            }
            return 0;
        }

        private int RunRename(RenameOptions options)
        {
            string pakedFilePath    = options.packedFile;
            string oldPackedPath    = options.oldPackedPath;
            string newPackedPath    = options.newPackedPath;
            string outputPackedFile = options.outputPackedFile;

            var packedFile = new ScrapPackedFile(pakedFilePath);
            packedFile.Rename(oldPackedPath, newPackedPath);

            if (!packedFile.SaveToFile(outputPackedFile))
            {
                // todo: make tests of this 
                Console.Error.WriteLine("Error: unable to save \"{0}\". Check if you provided valid -o argument", outputPackedFile);
                Environment.Exit(1);
            }
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
