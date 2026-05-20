using Avalonia.Controls;
using Avalonia.Input;
using BioIMA.Core.Models;

namespace BioIMA.Avalonia.Views;

public partial class EditLabelWindow : Window
{
    public string EditedName { get; private set; } = string.Empty;
    public string EditedStrokeColor { get; private set; } = "#00BCD4";
    public string EditedFillColor { get; private set; } = "#5500BCD4";
    public bool ShouldDelete { get; private set; } = false;

    public EditLabelWindow(AnnotationLabel label)
    {
        InitializeComponent();

        NameTextBox.Text = label.Name;
        EditedStrokeColor = label.StrokeColor;
        EditedFillColor = label.FillColor;

        BindColor(ColorCyan,   "#00BCD4", "#5500BCD4");
        BindColor(ColorRed,    "#F44336", "#55F44336");
        BindColor(ColorGreen,  "#4CAF50", "#554CAF50");
        BindColor(ColorBlue,   "#2196F3", "#552196F3");
        BindColor(ColorOrange, "#FF9800", "#55FF9800");
        BindColor(ColorPurple, "#9C27B0", "#559C27B0");
        BindColor(ColorPink,   "#E91E63", "#55E91E63");
        BindColor(ColorYellow, "#FFEB3B", "#55FFEB3B");
        BindColor(ColorLime,   "#CDDC39", "#55CDDC39");
        BindColor(ColorTeal,   "#009688", "#55009688");
        BindColor(ColorBrown,  "#795548", "#55795548");
        BindColor(ColorWhite,  "#FFFFFF", "#55FFFFFF");

        DeleteButton.Click += (_, _) =>
        {
            ShouldDelete = true;
            Close(true);
        };

        OkButton.Click += (_, _) =>
        {
            EditedName = NameTextBox.Text ?? label.Name;
            Close(true);
        };

        CancelButton.Click += (_, _) =>
        {
            Close(false);
        };
    }

    private void BindColor(Control control, string stroke, string fill)
    {
        control.PointerPressed += (_, _) =>
        {
            EditedStrokeColor = stroke;
            EditedFillColor = fill;
        };
    }
}