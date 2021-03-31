using ch.romibi.Scrap.Packed.PackerLib;
using CommandLine;
using System;
using System.Collections.Generic;

namespace ch.romibi.Scrap.Packed.Explorer
{
    class CliApp
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
            Console.WriteLine("test add");
            // var packer = new ScrapPackedFile(options.packedFile);
            // Todo: implement RunAdd
            return 0;
        }

        private int RunRemove(RemoveOptions options)
        {
            Console.WriteLine("test remove");
            // Todo: implement RunRemove
            return 0;
        }

        private int RunRename(RenameOptions options)
        {
            var packedFile = new ScrapPackedFile(options.packedFile);
            packedFile.Rename(options.oldPackedPath, options.newPackedPath);
            // Todo: save to self?
            packedFile.SaveToFile(options.outputPackedFile);
            return 0;
        }

        private int RunExtract(ExtractOptions options)
        {
            Console.WriteLine("test extract");
            // Todo: implement RunExtract
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
