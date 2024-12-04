using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using wpf522.CustomControls;

namespace wpf522
{
    /// <summary>
    /// SetScaleWindow.xaml 的交互逻辑
    /// </summary>
    public partial class SetScaleWindow : Window
    {
        private CanvasOption CanvasOption; 

        public SetScaleWindow(CanvasOption CanvasOptionInstance)
        {
            InitializeComponent();
            CanvasOption = CanvasOptionInstance;
        }

        // ObservableCollection 用于绑定 ListView
        private ObservableCollection<ScaleItem> scaleItems;

        public SetScaleWindow()
        {
            InitializeComponent();

            // 初始化 ObservableCollection
            scaleItems = new ObservableCollection<ScaleItem>();
            ListView1.ItemsSource = scaleItems;
        }

        private void Apply_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 获取用户输入的数据
                string title = TitleTextBox.Text;
                double distanceInPixels = double.Parse(WidthinpixelTextBox.Text);
                double knownDistance = double.Parse(TextBox4.Text);
                double ratio = double.Parse(RatioTextBox.Text); // 此处需要根据实际情况获取
                string unit = ((ComboBoxItem)UnitComboBox.SelectedItem).Content.ToString();
                double size = double.Parse(TextBox2.Text); // 此处需要根据实际情况获取
                // 计算标尺的实际长度
                double actualDistance = knownDistance * ratio;
                double pixelsPerUnit = distanceInPixels / actualDistance;
                double scaleLength = size * pixelsPerUnit;

                // 更新 CanvasOption 中的标尺属性
                CanvasOption.ScaleStartPoint = new Point(10, 10);  // 标尺线的起点
                CanvasOption.ScaleEndPoint = new Point(10 + scaleLength, 10); // 标尺线的终点
                CanvasOption.ScaleText = $"{size} {unit}"; // 标尺文本内容
                CanvasOption.ScaleVisibility = Visibility.Visible; // 显示标尺

                // 添加新的 ScaleItem 到 ObservableCollection
                scaleItems.Add(new ScaleItem(title, distanceInPixels, knownDistance, unit, size, ratio));

                // 清空输入框
                ResetInputs();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message);
            }
        }

        private void Reset_Click(object sender, RoutedEventArgs e)
        {
            ResetInputs();
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            // 可选：保存用户设置到持久存储，例如数据库
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void ResetInputs()
        {
            // 重置输入框和控件状态
            TitleTextBox.Text = "";
            WidthinpixelTextBox.Text = "";
            TextBox4.Text = "10"; // 恢复默认值
            RatioTextBox.Text = "1"; // 恢复默认值
            UnitComboBox.SelectedIndex = 0; // 恢复默认选择
            TextBox2.Text = "";
        }
    }

    // 数据项类，用于绑定 ListView 的每一行
    public class ScaleItem
    {
        public string Title { get; set; }
        public double DistanceInPixels { get; set; }
        public double KnownDistance { get; set; }
        public string Unit { get; set; }
        public double Size { get; set; }
        public double Ratio { get; set; }

        public ScaleItem(string title, double distanceInPixels, double knownDistance, string unit, double size, double ratio)
        {
            Title = title;
            DistanceInPixels = distanceInPixels;
            KnownDistance = knownDistance;
            Unit = unit;
            Size = size;
            Ratio = ratio;
        }
    }
}
