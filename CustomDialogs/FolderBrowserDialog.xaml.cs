using System;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
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
using System.Windows.Navigation;
using System.Windows.Shapes;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;


namespace wpf522.CustomDialogs
{



    public partial class FolderBrowserDialog : CustomDialog, INotifyPropertyChanged
    {



        public ObservableCollection<DirectoryItem> RootDirs { get; set; } = new ObservableCollection<DirectoryItem>();



        public DirectoryItem CurrentSelectedPath { get; set; } = null;



        private int Option { get; set; } = DirectoryItem.All;



        public string CurrentRootDir { get; set; } = "C:



        public bool? OptionResult = null;



        public string SelectedDirectoryPath { get; set; } = null;


        public FolderBrowserDialog(MetroWindow baseWindow, int op, string rootDir = "c:
        {
            InitializeComponent();
            Title = "File directory";
            this.DialogSettings.OwnerCanCloseWithDialog = true;
            this.Loaded += FolderBrowserDialog_Loaded;
            this.RootDirs.Add(new DirectoryItem(rootDir, DirectoryItem.Dir, op));
            this.CurrentRootDir = rootDir;
            RootDirs.First().Update();
            Option = op;
        }
        private void ExecuteColorButtonCommand(object parameter)
        {

            StartColorDrawing();
        }
        private void StartColorDrawing()
        {

            if (MainWindow.Instance != null)
            {
                MainWindow.Instance.StartColorDrawing();
            }
        }
    
    public event PropertyChangedEventHandler? PropertyChanged;

        private void FolderBrowserDialog_Loaded(object sender, RoutedEventArgs e)
        {
            this.DataContext = this;
        }

        private void TreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            DirectoryItem item = e.NewValue as DirectoryItem;
            if (item is null) return;
            CurrentSelectedPath = item;
            SelectedDirectoryPath = item.Root;
            item.Update();
        }





        private void TextBox_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if (Directory.Exists(this.CurrentRootDir))
                {
                    this.RootDirs.Clear();
                    this.RootDirs.Add(new DirectoryItem(CurrentRootDir, DirectoryItem.Dir, this.Option));
                    SelectedDirectoryPath = CurrentRootDir;
                    RootDirs.First().Update();
                }
            }
        }





        private async void SureSelected(object sender, RoutedEventArgs e)
        {
            OptionResult = true;
            await this.RequestCloseAsync();
        }





        private async void CancleSelected(object sender, RoutedEventArgs e)
        {
            OptionResult = false;
            await this.RequestCloseAsync();
        }


        public async Task WaitForResult()
        {
            while (OptionResult is null)
            {
                await Task.Delay(500);
            }
        }
    }



    public class DirectoryItem : INotifyPropertyChanged
    {
        public static int Dir = 1;
        public static int File = 2;
        public static int All = Dir | File;



        public string Root { get; set; }



        public int Type { get; set; }



        public string Name { get; set; }



        public int OptionContent { get; set; } = Dir | File;



        public ObservableCollection<DirectoryItem> Children { get; set; } = new ObservableCollection<DirectoryItem>();

        public DirectoryItem(string root, int type, int op)
        {
            this.Root = root;
            this.Type = type;
            this.OptionContent = op;
            string[] sp = null;
            if (root.Contains(System.IO.Path.DirectorySeparatorChar))
            {
                sp = root.Split(System.IO.Path.DirectorySeparatorChar);
            }
            else
            {
                sp = root.Split("
            }
            if (string.IsNullOrEmpty(sp.Last()))
            {
                Name = sp[sp.Length - 2];
            }
            else
            {
                Name = sp.Last();
            }
        }



        public void Update()
        {
            if (this.Children.Count > 0) return;
            if (this.Type == Dir)
            {
                try
                {
                    if ((this.OptionContent & Dir) > 0)
                    {
                        foreach (var item in Directory.GetDirectories(this.Root))
                        {
                            Children.Add(new DirectoryItem(item, Dir, this.OptionContent));
                        }
                    }
                    else if ((this.OptionContent & File) > 0)
                    {
                        foreach (var item in Directory.GetFiles(this.Root))
                        {
                            Children.Add(new DirectoryItem(item, File, this.OptionContent));
                        }
                    }
                }
                catch (Exception ex)
                {

                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
    }
}

