using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace wpf522.Models
{
    /// <summary>
    /// 颜色和形状的类型对应
    /// </summary>
    public class ShapeTypeColorStruct : INotifyPropertyChanged
    {
        /// <summary>
        /// 颜色
        /// </summary>
        private string color;

        private string typeName;

        private bool isChecked;
        /// <summary>
        /// 类型名称
        /// </summary>
        public string TypeName
        {
            get { return typeName; }
            set
            {
                typeName = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("TypeName"));
            }
        }
        /// <summary>
        /// 颜色
        /// </summary>
        public string Color
        {
            get { return color; }
            set
            {
                color = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Color"));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("TypeName"));
                if (MainWindow.Instance is not null && MainWindow.Instance.MainModel is not null &&
                    MainWindow.Instance.MainModel.CurrentImageModel is not null)
                { 
                    //通知颜色选中变更 导致刷新 刷新颜色
                    MainWindow.Instance.MainModel.CurrentImageModel.ChangeShapeProperty("TypeName", TypeName);
                }
            }
        }
        /// <summary>
        /// 是否被选中
        /// </summary>
        [JsonIgnore]
        public bool IsChecked

        {
            get { return isChecked; }
            set
            {
                isChecked = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("IsChecked"));
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
    }
}
