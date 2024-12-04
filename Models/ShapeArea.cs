using wpf522.Models.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace wpf522.Models
{
    /// <summary>
    /// 一个形状
    /// </summary>
    public abstract class ShapeArea : INotifyPropertyChanged
    {
        /// <summary>
        /// 类型
        /// </summary>
        public ShapeType ShapeType { get; set; }
        /// <summary>
        /// 类型名称
        /// </summary>
        public string TypeName { get; set; } = null;
        /// <summary>
        /// 是否被选中
        /// </summary>
        [JsonIgnore]
        public bool IsSelected { get; set; }

        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// 让当前对象中的某个字段发生变更通知
        /// </summary>
        public void ChangeProperty(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }


  
    }
}
