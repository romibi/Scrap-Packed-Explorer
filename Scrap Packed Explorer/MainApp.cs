using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace ch.romibi.Scrap.PackedExplorer
{
    class MainApp
    {
        [STAThread]
        public static void Main(string[] args)
        {
            if (args != null && args.Length > 0)
            {
                // ...
                Console.WriteLine("test");
            }
            else
            {
                HideConsoleWindow();
                var guiApp = new GuiApp();
                guiApp.InitializeComponent();
                guiApp.Run();
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
