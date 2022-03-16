using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace ch.romibi.Scrap.Packed.Explorer {
    class MainApp {
        [STAThread]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Main has args")]
        public static int Main(string[] p_Args) {
            var guiApp = new GuiApp();
            guiApp.InitializeComponent();
            guiApp.Run();
            return 0;
        }
    }
}
