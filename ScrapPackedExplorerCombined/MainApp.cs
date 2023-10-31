using ch.romibi.Scrap.Packed.Explorer.Cli;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;

namespace ch.romibi.Scrap.Packed.Explorer.Combined {
    internal class MainApp {
        static readonly string[] SingleCliArgs = {
            "help",    "--help",    "-h",
            "version", "--version", "-v",
        };

        [STAThread]
        public static int Main(string[] p_Args) {
            bool isCliArgProvided = false;

            if (p_Args.Length > 0)
                isCliArgProvided = SingleCliArgs.Contains(p_Args[0]);

            if (isCliArgProvided || p_Args.Length > 1)
                return CliApp.Run(p_Args);

            HideConsoleWindow();
            GuiApp guiApp = new();

            if (p_Args.Length > 0) {
                string packedFilePath = p_Args[0];
                guiApp.LoadPackedFile(packedFilePath);
            }

            guiApp.Run();
            return 0;
        }

        private static void HideConsoleWindow() {
            IntPtr ptr = GetForegroundWindow();
            uint v = GetWindowThreadProcessId(ptr, out int u);
            if (v == 0)
                throw new Exception("Error: unable get process ID");
            Process process = Process.GetProcessById(u);

            // Check if it is console?
            List<string> consoleApplications = new(new string[] { "cmd", "wt", "powershell" });
            if (!consoleApplications.Contains(process.ProcessName)) {
                IntPtr handle = GetConsoleWindow();
                ShowWindow(handle, 0);
            }
        }

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetConsoleWindow();

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr p_HWnd, int p_NCmdShow);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint GetWindowThreadProcessId(IntPtr p_HWnd, out int p_LpdwProcessId);


    }
}
