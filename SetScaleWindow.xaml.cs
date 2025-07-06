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



    public partial class SetScaleWindow : Window
    {
        private CanvasOption CanvasOption; 

        public SetScaleWindow(CanvasOption CanvasOptionInstance)
        {
            InitializeComponent();
            CanvasOption = CanvasOptionInstance;
        }

        private ObservableCollection<ScaleItem> scaleItems;

        public SetScaleWindow()
        {
            InitializeComponent();

            scaleItems = new ObservableCollection<ScaleItem>();
            ListView1.ItemsSource = scaleItems;
        }

        private void Apply_Click(object sender, RoutedEventArgs e)
        {
            try
            {

                string title = TitleTextBox.Text;
                double distanceInPixels = double.Parse(WidthinpixelTextBox.Text);
                double knownDistance = double.Parse(TextBox4.Text);
                double ratio = double.Parse(RatioTextBox.Text); 
                string unit = ((ComboBoxItem)UnitComboBox.SelectedItem).Content.ToString();
                double size = double.Parse(TextBox2.Text); 

                double actualDistance = knownDistance * ratio;
                double pixelsPerUnit = distanceInPixels / actualDistance;
                double scaleLength = size * pixelsPerUnit;

                CanvasOption.ScaleStartPoint = new Point(10, 10);  
                CanvasOption.ScaleEndPoint = new Point(10 + scaleLength, 10); 
                CanvasOption.ScaleText = $"{size} {unit}"; 
                CanvasOption.ScaleVisibility = Visibility.Visible; 

                scaleItems.Add(new ScaleItem(title, distanceInPixels, knownDistance, unit, size, ratio));

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

        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void ResetInputs()
        {

            TitleTextBox.Text = "";
            WidthinpixelTextBox.Text = "";
            TextBox4.Text = "10"; 
            RatioTextBox.Text = "1"; 
            UnitComboBox.SelectedIndex = 0; 
            TextBox2.Text = "";
        }
    }

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

