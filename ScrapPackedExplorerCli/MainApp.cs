using ch.romibi.Scrap.Packed.Explorer.Cli;
using System;

namespace ch.romibi.Scrap.Packed.Explorer.Cli {
    internal class MainApp {
        [STAThread]
        public static int Main(string[] p_Args) {
            return CliApp.Run(p_Args);
        }
    }
}
