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
            string packedPath       = options.packedPath;
            string outputPackedFile = options.outputPackedFile;

            if (!File.Exists(pakedFilePath))
            {
                Console.Error.WriteLine("Error: file \"{0}\" is not exists.", pakedFilePath);
                Environment.Exit(1);
            }
            if (!File.Exists(sourcePath))
            {
                Console.Error.WriteLine("Error: file \"{0}\" is not exists.", sourcePath);
                Environment.Exit(1);
            }

            var packedFile = new ScrapPackedFile(pakedFilePath);
            if (!packedFile.Add(sourcePath, packedPath))
            {
                Console.Error.WriteLine("Error: unable to add \"{0}\" to \"{1}\" because input file is too large or it is not exist.", sourcePath, outputPackedFile);
                Environment.Exit(1);
            }

            if (!packedFile.SaveToFile(outputPackedFile))
            {
                // todo: make tests of this 
                Console.Error.WriteLine("Error: unable to save \"{0}\". Check if you provided valid -o argument.", outputPackedFile);
                Environment.Exit(1);
            }
            return 0;
        }

        private int RunRemove(RemoveOptions options)
        {
            string pakedFilePath    = options.packedFile;
            string packedPath       = options.packedPath;
            string outputPackedFile = options.outputPackedFile;

            if (!File.Exists(pakedFilePath))
            {
                Console.Error.WriteLine("Error: file \"{0}\" is not exists.", pakedFilePath);
                Environment.Exit(1);
            }

            var packedFile = new ScrapPackedFile(pakedFilePath);
            if (!packedFile.Remove(packedPath))
            {
                Console.Error.WriteLine("Error: unable to delete \"{0}\": this file is not exists in \"{1}\".", packedPath, pakedFilePath);
                Environment.Exit(1);
            }

            if (!packedFile.SaveToFile(outputPackedFile))
            {
                // todo: make tests of this 
                Console.Error.WriteLine("Error: unable to save \"{0}\". Check if you provided valid -o argument.", outputPackedFile);
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

            if (!File.Exists(pakedFilePath))
            {
                Console.Error.WriteLine("Error: file \"{0}\" is not exists.", pakedFilePath);
                Environment.Exit(1);
            }
            if (oldPackedPath == "")
            {
                Console.Error.WriteLine("Error: \"-s\" must not be empty.");
                Environment.Exit(1);
            }
            if (newPackedPath == "")
            {
                Console.Error.WriteLine("Error: \"-d\" must not be empty.");
                Environment.Exit(1);
            }

            var packedFile = new ScrapPackedFile(pakedFilePath);
            if (!packedFile.Rename(oldPackedPath, newPackedPath))
            {
                Console.Error.WriteLine("Error: unable to rename \"{0}\" to \"{1}\". \"{0}\" is not exsits in \"{2}\".", oldPackedPath, newPackedPath, pakedFilePath);
                Environment.Exit(1);
            }

            if (!packedFile.SaveToFile(outputPackedFile))
            {
                // todo: make tests of this 
                Console.Error.WriteLine("Error: unable to save \"{0}\". Check if you provided valid -o argument.", outputPackedFile);
                Environment.Exit(1);
            }
            return 0;
        }

        private int RunExtract(ExtractOptions options)
        {
            string pakedFilePath   = options.packedFile;
            string packedPath      = options.packedPath;
            string destinationPath = options.destinationPath;

            if (!File.Exists(pakedFilePath))
            {
                Console.Error.WriteLine("Error: file \"{0}\" is not exists.", pakedFilePath);
                Environment.Exit(1);
            }

            if (destinationPath == "")
            {
                Console.Error.WriteLine("Error: destination path must not be empty.");
                Environment.Exit(1);
            }

            var packedFile = new ScrapPackedFile(pakedFilePath);
            if (!packedFile.Extract(packedPath, destinationPath))
            {
                Console.Error.WriteLine("Error: unable to extract \"{0}\" from \"{1}\". \"{0}\" is not exsits in \"{1}\".", packedPath, pakedFilePath);
                Environment.Exit(1);
            }
            return 0;
        }

        private int RunList(ListOptions options)
        {
            string pakedFilePath = options.packedFile;

            if (!File.Exists(pakedFilePath))
            {
                Console.Error.WriteLine("Error: file \"{0}\" is not exists.", pakedFilePath);
                Environment.Exit(1);
            }

            var packedFile = new ScrapPackedFile(pakedFilePath);
            List<string> fileNames = packedFile.GetFileNames();

            if (fileNames.Count == 0)
            {
                Console.WriteLine("\"{0}\" is empty.", pakedFilePath);
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

    }
}
