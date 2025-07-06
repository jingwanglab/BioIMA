using System;
using System.Collections.Generic;
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
using wpf522.Models;

namespace wpf522.AutoSamSeg
{
    /// <summary>
    /// autoResultWindow.xaml 的交互逻辑
    /// 
    /// </summary>

    public partial class autoResultWindow : Window
    {
        public string PixelAreaTextBlock { get; set; }
        public string RealAreaTextBlock { get; set; }

        public event Action<string, string> SaveArea; // 保存事件

        public autoResultWindow(string pixelAreaTextBlock, string realAreaTextBlock)
        {
            InitializeComponent();
            PixelAreaTextBlock = pixelAreaTextBlock;
            RealAreaTextBlock = realAreaTextBlock;
            DataContext = this;  // 设置数据上下文以绑定窗口中的 TextBlock
        }



        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            SaveArea?.Invoke(PixelAreaTextBlock, RealAreaTextBlock); // 触发保存事件
            this.Close(); // 关闭窗口
        }



        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
