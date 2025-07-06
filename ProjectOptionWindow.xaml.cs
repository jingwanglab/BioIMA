using wpf522.Models;
using MahApps.Metro.Controls;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace wpf522
{



    public partial class ProjectOptionWindow : MoveWindow
    {



        public ProjectManager ProjectManager { get; set; }



        public static ProjectOptionWindow Instance { get; set; }

        public ProjectOptionWindow()
        {
            InitializeComponent();
            Instance = this;
            ProjectManager.LoadProjectManager();
            ProjectManager = ProjectManager.Instance;
            MoveUIElement = this;
            Loaded += ProjectOptionWindow_Loaded;
        }


        private void ProjectOptionWindow_Loaded(object sender, RoutedEventArgs e)
        {
            this.DataContext = ProjectManager;
        }

        public async Task<string> OpenFolderDialogAsync()
        {
            var dialog = new System.Windows.Forms.FolderBrowserDialog
            {
                Description = "Select a folder",
                RootFolder = Environment.SpecialFolder.MyComputer,
                ShowNewFolderButton = false
            };

            var result = dialog.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK && !string.IsNullOrWhiteSpace(dialog.SelectedPath))
            {
                return dialog.SelectedPath;
            }
            return null;
        }

        private DateTime PrivouseTime = DateTime.Now;





        private void RowClickEvent(object sender, MouseButtonEventArgs e)
        {
            if ((DateTime.Now - PrivouseTime).TotalMilliseconds < 300)
            {
                ProjectManager.OpenProjectCommand.Execute(null);
            }
            PrivouseTime = DateTime.Now;
        }
    }
}

