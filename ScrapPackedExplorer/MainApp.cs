using ch.romibi.Scrap.Packed.PackerLib;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;

namespace ch.romibi.Scrap.Packed.Explorer {
    class MainApp {
        [STAThread]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Main has args")]
        public static int Main(string[] p_Args) {
            var guiApp = new GuiApp();

            if (p_Args.Length > 0) {
                string packedFilePath = p_Args[0];
                guiApp.LoadPackedFile(packedFilePath);
            }

            guiApp.Run();
            return 0;
        }
    }
}
