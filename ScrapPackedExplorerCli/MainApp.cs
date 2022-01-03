using ch.romibi.Scrap.Packed.Explorer.Cli;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;

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
            IntPtr ptr = GetForegroundWindow();
            GetWindowThreadProcessId(ptr, out int u);
            Process process = Process.GetProcessById(u);

            // Check if it is console?
            var consoleApplications = new List<String>(new string[] { "cmd", "wt", "powershell" });
            if (!consoleApplications.Contains(process.ProcessName))
            {
                var handle = GetConsoleWindow();
                ShowWindow(handle, 0);
            }
        }

        [DllImport("kernel32.dll")]
        static extern IntPtr GetConsoleWindow();

        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll", SetLastError = true)]
        static extern uint GetWindowThreadProcessId(IntPtr hWnd, out int lpdwProcessId);


    }
}
