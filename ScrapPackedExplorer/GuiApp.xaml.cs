using ch.romibi.Scrap.Packed.PackerLib;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;

namespace ch.romibi.Scrap.Packed.Explorer {
    public enum Theme { System, Dark, Light }


    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class GuiApp : Application {
        protected string PackedFilePath { get; set; }

        private Theme _AppTheme = Theme.System;
        public Theme AppTheme {
            get { return _AppTheme; }
            set {
                _AppTheme = value;
                UpdateTheme();
            }
        }

        protected override void OnStartup(StartupEventArgs e) {
            base.OnStartup(e);

            UpdateTheme();

            MainWindow mainWindow = new MainWindow();

            if (PackedFilePath != null) {
                mainWindow.LoadPackedFileByPath(PackedFilePath);
            }

            mainWindow.InitializeComponent();
            mainWindow.Show();
        }

        public void LoadPackedFile(string p_PackedPath) {
            PackedFilePath = p_PackedPath;
        }

        private void UpdateTheme() {
            bool dark;
            if (AppTheme == Theme.System) {
                dark = ShouldSystemUseDarkMode();
            } else {
                dark = AppTheme == Theme.Dark;
            }

            Resources.Clear();
            if (dark) {
                Resources.MergedDictionaries.Add(new ResourceDictionary() { Source = new Uri("/Themes/DarkTheme.xaml", UriKind.Relative) });
            } else {
                Resources.MergedDictionaries.Add(new ResourceDictionary() { Source = new Uri("/Themes/LightTheme.xaml", UriKind.Relative) });
            }
        }

        [DllImport("UXTheme.dll", SetLastError = true, EntryPoint = "#138")]
        private static extern bool ShouldSystemUseDarkMode();
    }
}
