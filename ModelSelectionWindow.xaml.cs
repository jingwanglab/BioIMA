using Microsoft.Win32;
using System.Windows;
using System.Windows.Controls;

namespace wpf522
{
    public partial class ModelSelectionWindow : Window
    {
        public string SelectedModelPath { get; private set; }
        public string SelectedModel { get; private set; }

        public ModelSelectionWindow()
        {
            InitializeComponent();
        }

        private void SelectButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedItem = ModelListBox.SelectedItem as ListBoxItem;
            if (selectedItem != null)
            {
                SelectedModel = selectedItem.Content.ToString();
                if (SelectedModel == "Load Custom Model......") // 和ListBoxItem的内容保持一致
                {
                    var dialog = new OpenFileDialog();
                    dialog.Filter = "ONNX Files (*.onnx)|*.onnx|YAML Files (*.yaml)|*.yaml|All Files (*.*)|*.*";
                    if (dialog.ShowDialog() == true)
                    {
                        SelectedModelPath = dialog.FileName;
                    }
                    else
                    {
                        MessageBox.Show("Please select a model file.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                }
                else
                {
                    SelectedModelPath = GetPredefinedModelPath(SelectedModel);
                }

                DialogResult = true;
                Close();
            }
            else
            {
                MessageBox.Show("Please select a model.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string GetPredefinedModelPath(string modelName)
        {
            // 返回预定义模型路径
            switch (modelName)
            {
                case "MobileSAM":
                    return "./Models/segmentmodels/mobilesam/mobile_sam.encoder.onnx";
                case "Segmentanything (ViT-B)":
                    return "path/to/Segmentanything/ViT-B/model";
                case "Segmentanything (ViT-L)":
                    return "path/to/Segmentanything/ViT-L/model";
                case "Segmentanything (ViT-H)":
                    return "path/to/Segmentanything/ViT-H/model";
                case "Yolov5s":
                    return "path/to/Yolov5s/model";
                default:
                    return null;
            }
        }

        private void ModelListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ModelListBox.SelectedItem is ListBoxItem selectedItem)
            {
                switch (selectedItem.Content.ToString())
                {
                    case "SAM":
                        // 打开SAMSegWindow窗口
                        SAMSegWindow samWindow = new SAMSegWindow();
                        samWindow.Show();
                        this.Close(); // 可选，关闭当前窗口
                        break;
                    case "Segmentanything (ViT-B)":
                        // 在这里添加操作，例如打开其他窗口或执行任务
                        MessageBox.Show("Selected: Segmentanything (ViT-B)");
                        break;
                    case "Segmentanything (ViT-L)":
                        // 在这里添加操作
                        MessageBox.Show("Selected: Segmentanything (ViT-L)");
                        break;
                    case "Segmentanything (ViT-H)":
                        // 在这里添加操作
                        MessageBox.Show("Selected: Segmentanything (ViT-H)");
                        break;
                    case "Yolov5s":
                        // 在这里添加操作
                        MessageBox.Show("Selected: Yolov5s");
                        break;
                    case "Load Custom Model......":
                        // 在这里添加操作
                        MessageBox.Show("Selected: Load Custom Model");
                        break;
                    default:
                        break;
                }
            }
        }
    }
}

