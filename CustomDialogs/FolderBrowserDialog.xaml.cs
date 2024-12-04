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
    /// <summary>
    /// FolderBrowserDialog.xaml 的交互逻辑
    /// </summary>
    public partial class FolderBrowserDialog : CustomDialog, INotifyPropertyChanged
    {
        /// <summary>
        /// 设置根目录
        /// </summary>
        public ObservableCollection<DirectoryItem> RootDirs { get; set; } = new ObservableCollection<DirectoryItem>();
        /// <summary>
        /// 当前选中的路径
        /// </summary>
        public DirectoryItem CurrentSelectedPath { get; set; } = null;
        /// <summary>
        /// 当前显示方式
        /// </summary>
        private int Option { get; set; } = DirectoryItem.All;
        /// <summary>
        /// 当前路径
        /// </summary>
        public string CurrentRootDir { get; set; } = "C://";
        /// <summary>
        /// 操作结果
        /// </summary>
        public bool? OptionResult = null;
        /// <summary>
        /// 选中的目录
        /// </summary>
        public string SelectedDirectoryPath { get; set; } = null;


        public FolderBrowserDialog(MetroWindow baseWindow, int op, string rootDir = "c://") : base(baseWindow)
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
            // 触发绘制颜色框的逻辑
            StartColorDrawing();
        }
        private void StartColorDrawing()
        {
            // 通知 CanvasOption 开始绘制颜色框
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
        /// <summary>
        /// 按键按下
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
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
        /// <summary>
        /// 确认选择
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void SureSelected(object sender, RoutedEventArgs e)
        {
            OptionResult = true;
            await this.RequestCloseAsync();
        }
        /// <summary>
        /// 取消选择
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
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
    /// <summary>
    /// 目录项
    /// </summary>
    public class DirectoryItem : INotifyPropertyChanged
    {
        public static int Dir = 1;
        public static int File = 2;
        public static int All = Dir | File;
        /// <summary>
        /// 路径
        /// </summary>
        public string Root { get; set; }
        /// <summary>
        /// 类型
        /// </summary>
        public int Type { get; set; }
        /// <summary>
        /// 名称
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// 操作的内容
        /// </summary>
        public int OptionContent { get; set; } = Dir | File;
        /// <summary>
        /// 子项集合
        /// </summary>
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
                sp = root.Split("//");
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
        /// <summary>
        /// 刷新内容
        /// </summary>
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
