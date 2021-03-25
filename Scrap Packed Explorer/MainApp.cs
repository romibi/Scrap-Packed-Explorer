using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace ch.romibi.Scrap.Packed.Explorer
{
    class MainApp
    {
        [STAThread]
        public static int Main(string[] args)
        {
            if (args != null && args.Length > 0)
            {
                var cliApp = new CliApp();
                return cliApp.Run(args);
            }
            else
            {
                HideConsoleWindow();
                var guiApp = new GuiApp();
                guiApp.InitializeComponent();
                guiApp.Run();
                return 0;
            }
        }

        private static void HideConsoleWindow()
        {
            var handle = GetConsoleWindow();

            ShowWindow(handle, 0);
        }

        [DllImport("kernel32.dll")]
        static extern IntPtr GetConsoleWindow();

        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
    }
}
