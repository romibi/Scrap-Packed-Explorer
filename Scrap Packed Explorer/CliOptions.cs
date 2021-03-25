using CommandLine;
using System;
using System.Collections.Generic;
using System.Text;

namespace ch.romibi.Scrap.PackedExplorer
{
    [Verb("add", HelpText = "Add file to the archive")]
    class AddOptions
    {
        [Option("packedName")]
        public string packedName { get; set; }
    }

    [Verb("remove", HelpText = "Remove a file from the archive")]
    class RemoveOptions
    { 
        [Option("packedName")]
        public string packedName { get; set; }
    }
}
