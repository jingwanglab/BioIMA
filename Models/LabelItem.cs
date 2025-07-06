using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media;

namespace YourNamespace.Models
{
    //public class LabelItem : INotifyPropertyChanged
    //{
    //    private string _name;
    //    private Color _color;
    //    private bool _isChecked;
    //    private float[] _maskData;

    //    public string Name
    //    {
    //        get => _name;
    //        set { _name = value; OnPropertyChanged(); }
    //    }

    //    public Color Color
    //    {
    //        get => _color;
    //        set { _color = value; OnPropertyChanged(); }
    //    }

    //    public bool IsChecked
    //    {
    //        get => _isChecked;
    //        set { _isChecked = value; OnPropertyChanged(); }
    //    }

    //    public float[] MaskData
    //    {
    //        get => _maskData;
    //        set { _maskData = value; OnPropertyChanged(); }
    //    }

    //    public event PropertyChangedEventHandler PropertyChanged;
    //    protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
    //    {
    //        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    //    }
    //}
    public class LabelItem : INotifyPropertyChanged
    {
        private string _name;
        private Color _color;
        private bool _isChecked;

        public string Name
        {
            get => _name;
            set { _name = value; OnPropertyChanged(nameof(Name)); }
        }

        public Color Color
        {
            get => _color;
            set { _color = value; OnPropertyChanged(nameof(Color)); }
        }

        public bool IsChecked
        {
            get => _isChecked;
            set { _isChecked = value; OnPropertyChanged(nameof(IsChecked)); }
        }

        public float[] MaskData { get; set; } // 存储掩膜数据

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

}
