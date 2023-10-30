using ch.romibi.Scrap.Packed.PackerLib;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace ch.romibi.Scrap.Packed.Explorer {
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class GuiApp : Application {
        protected string PackedFilePath { get; set; }

        protected override void OnStartup(StartupEventArgs e) {
            base.OnStartup(e);
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
    }
}
