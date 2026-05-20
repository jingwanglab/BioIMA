using System;
using System.Globalization;
using Avalonia.Controls;

namespace BioIMA.Avalonia.Views;

public partial class SetScaleWindow : Window
{
    public double PixelLength { get; }
    public double KnownLength { get; private set; }
    public string UnitName { get; private set; } = "mm";
    public bool Confirmed { get; private set; }

    public SetScaleWindow(double pixelLength)
    {
        InitializeComponent();

        PixelLength = pixelLength;
        PixelLengthTextBlock.Text = $"{pixelLength:F2} px";
        KnownLengthTextBox.Text = "1.0";

        OkButton.Click += (_, _) =>
        {
            if (!double.TryParse(KnownLengthTextBox.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out var knownLength) ||
                knownLength <= 0)
            {
                HintTextBlock.Text = "Please enter a valid known length (> 0).";
                return;
            }

            KnownLength = knownLength;

            if (UnitComboBox.SelectedItem is ComboBoxItem item && item.Content is not null)
                UnitName = item.Content.ToString() ?? "mm";

            Confirmed = true;
            Close();
        };

        CancelButton.Click += (_, _) =>
        {
            Confirmed = false;
            Close();
        };
    }
}