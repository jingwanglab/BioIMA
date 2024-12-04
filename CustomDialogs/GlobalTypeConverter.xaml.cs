using wpf522.Expends;
using wpf522.Models;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.VisualBasic.FileIO;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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

namespace wpf522.CustomDialogs
{
    /// <summary>
    /// GlobalTypeConverter.xaml 的交互逻辑
    /// </summary>
    public partial class GlobalTypeConverter : CustomDialog, INotifyPropertyChanged
    {
        /// <summary>
        /// 转换的数据源
        /// </summary>
        public List<ShapeArea> TypeFroms { get; set; } = new List<ShapeArea>();
        /// <summary>
        /// 目标类型
        /// </summary>
        public List<ShapeTypeColorStruct> TypeTargets { get; set; } = new List<ShapeTypeColorStruct>();
        /// <summary>
        /// 是否确认转换
        /// </summary>
        public bool? IsSure { get; set; } = null;


        public ShapeArea SelectedFrom { get; set; }
        public ShapeTypeColorStruct SelectedTarget { get; set; }


        public GlobalTypeConverter(MetroWindow baseWindow, MainModel model) : base(baseWindow)
        {
            InitializeComponent();
            this.DialogSettings.OwnerCanCloseWithDialog = true;
            this.Loaded += GlobalTypeConverter_Loaded;

            
            model.ImageModels.Foreach(p => TypeFroms.AddRange(p.Shapes));
            TypeTargets.AddRange(model.ToolConfig.ShapeTypeColorStructs);

            TypeFroms = TypeFroms.Distinct<ShapeArea, string>(p => p.TypeName).ToList();

            SelectedFrom = TypeFroms.FirstOrDefault();
            SelectedTarget = TypeTargets.FirstOrDefault();
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// 加载事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void GlobalTypeConverter_Loaded(object sender, RoutedEventArgs e)
        {

            this.DataContext = this;
        }

        private async void CancaleClick(object sender, RoutedEventArgs e)
        {
            IsSure = false;
            await this.RequestCloseAsync();
        }

        private async void SureClick(object sender, RoutedEventArgs e)
        {
            IsSure = true;
            await this.RequestCloseAsync();
        }

        public async Task WaitForResult()
        {
            while (IsSure is null)
            {
                await Task.Delay(500);
            }
        }
    }
}
