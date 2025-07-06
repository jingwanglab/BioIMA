using wpf522.Adorners;
using wpf522.CustomDialogs;
using wpf522.Models;
using wpf522.Models.DrawShapes;
using MahApps.Metro.Controls;
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
using MahApps.Metro.Controls.Dialogs;
using wpf522.Expends;
using static wpf522.CustomControls.CanvasOption;
using System.Collections.ObjectModel;
using Microsoft.ML.OnnxRuntime.Tensors;
using Microsoft.ML.OnnxRuntime;
using System.Drawing;
using wpf522.CustomControls;
using System.IO;
using wpf522.Models.Enums;



namespace wpf522
{



    public partial class MainWindow : MetroWindow
    {




        private CanvasOption canvasOption; 
        public MainModel MainModel { get; set; }



        public static MainWindow Instance { get; set; } = null;

        public ObservableCollection<MeasureData> MeasureDataCollection { get; set; }


        private int currentImageIndex = 0; 
        private List<string> imagePaths; 
        private string selectedModel;
        private string selectedModelPath;

        public MainWindow()
        {
            InitializeComponent();

            MeasureDataCollection = new ObservableCollection<MeasureData>();
            this.DataContext = this;

            this.Closing += MainWindow_Closing;
            ProjectOptionWindow projectOptionWindow = new ProjectOptionWindow();
            projectOptionWindow.ShowDialog();

            ProjectManager.SaveProjectManager();
            Instance = this;
            MainModel = ProjectManager.Instance.MainModel;
            if (MainModel == null) this.Close();
            this.Loaded += MainWindow_Loaded;


        }





        private void MainWindow_Closing(object? sender, CancelEventArgs e)
        {
            if (MainModel != null)
            {
                MainModel.SaveConfig();
            }
            ProjectManager.SaveProjectManager();
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            this.DataContext = MainModel;
        }




        public void ShowWait()
        {
            BkMask.Visibility = Visibility.Collapsed;
        }



        public void CloseWait()
        {
            BkMask.Visibility = Visibility.Collapsed;
        }





        private void TreeImageListSelectChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            var imageInfo = e.NewValue as ImageContentInfoModel;
            if (imageInfo is null) return;

            MainModel.CurrentImageModel = imageInfo;

        }

        private DateTime ForntDate = DateTime.Now;

        private void ClickVisiableEventMouseUp(object sender, MouseButtonEventArgs e)
        {
            DateTime now = DateTime.Now;
            if ((now - ForntDate).TotalMilliseconds < 200)
            {
                (sender as UIElement).Visibility = Visibility.Visible;
            }
            ForntDate = now;
        }
        private void SetScales_Click(object sender, RoutedEventArgs e)
        {
            SetScaleWindow setScaleWindow = new SetScaleWindow();
            setScaleWindow.ShowDialog();
        }

        private void LoseFocusToHiddenEvent(object sender, RoutedEventArgs e)
        {
            var ui = sender as UIElement;
            ui.Visibility = Visibility.Hidden;
        }
        private void SelectModelButton_Click(object sender, RoutedEventArgs e)
        {
            var modelSelectionWindow = new ModelSelectionWindow();
            if (modelSelectionWindow.ShowDialog() == true)
            {
                selectedModel = modelSelectionWindow.SelectedModel;
                selectedModelPath = modelSelectionWindow.SelectedModelPath;
            }
        }

        private void OpenProjectOptionsWindow(object sender, RoutedEventArgs e)
        {

            ProjectOptionWindow optionsWindow = new ProjectOptionWindow();

            this.Hide();

            optionsWindow.Show();

            optionsWindow.Closed += (s, args) => this.Show();
        }


        private void RunModelOnCurrentImage(string modelPath)
        {

            var CanvasOption = FindVisualChild<CanvasOption>(this);

            if (CanvasOption == null || CanvasOption.ImageModel == null || string.IsNullOrEmpty(CanvasOption.ImageModel.ImagePath))
            {
                MessageBox.Show("Please open an image first.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            string imagePath = CanvasOption.ImageModel.ImagePath;

            Task.Run(() =>
            {
                using (var session = new InferenceSession(modelPath))
                {
                    using (var image = new Bitmap(imagePath))
                    {
                        var inputTensor = ImageToTensor(image);

                        var inputs = new List<NamedOnnxValue>
                {
                    NamedOnnxValue.CreateFromTensor("input", inputTensor)
                };

                        using (var results = session.Run(inputs))
                        {
                            var output = results.First().AsTensor<float>();

                            this.Dispatcher.Invoke(() => DisplaySegmentationResult(output, image.Width, image.Height));
                        }
                    }
                }
            });
        }

        private T FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child != null && child is T)
                    return (T)child;

                var childOfChild = FindVisualChild<T>(child);
                if (childOfChild != null)
                    return childOfChild;
            }
            return null;
        }

        private Tensor<float> ImageToTensor(Bitmap image)
        {
            var width = image.Width;
            var height = image.Height;
            var tensor = new DenseTensor<float>(new[] { 1, 3, height, width });

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    var pixel = image.GetPixel(x, y);
                    tensor[0, 0, y, x] = pixel.R / 255.0f;
                    tensor[0, 1, y, x] = pixel.G / 255.0f;
                    tensor[0, 2, y, x] = pixel.B / 255.0f;
                }
            }

            return tensor;
        }

        private void DisplaySegmentationResult(Tensor<float> output, int width, int height)
        {
            var bitmap = new WriteableBitmap(width, height, 96, 96, System.Windows.Media.PixelFormats.Bgr32, null);
            byte[] pixels = new byte[width * height * 4];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int index = (y * width + x) * 4;
                    float value = output[0, 0, y, x];

                    byte intensity = (byte)(value > 0.5f ? 255 : 0);
                    pixels[index] = intensity; 
                    pixels[index + 1] = intensity; 
                    pixels[index + 2] = intensity; 
                    pixels[index + 3] = 255; 
                }
            }

            bitmap.WritePixels(new Int32Rect(0, 0, width, height), pixels, width * 4, 0);

        }

        
        private BitmapSource ConvertTo8BitGrayscale(BitmapSource original)
        {

            FormatConvertedBitmap grayBitmap = new FormatConvertedBitmap();
            grayBitmap.BeginInit();
            grayBitmap.Source = original;
            grayBitmap.DestinationFormat = PixelFormats.Gray8;
            grayBitmap.EndInit();
            return grayBitmap;
        }
    

    private string GetPredefinedModelPath(string modelName)
        {

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

        internal void StartColorDrawing()
        {
            throw new NotImplementedException();
        }





    }
}

