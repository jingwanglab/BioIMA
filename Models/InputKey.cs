using MahApps.Metro.Controls;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace wpf522.Models
{
    /// <summary>
    /// 输入快捷键
    /// </summary>
    public class InputKey : INotifyPropertyChanged
    {
        private HotKey hotKey = null;
        private Key key = Key.Q;
        private ModifierKeys modifierKeys = ModifierKeys.None;
        /// <summary>
        /// 快捷键名称
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// 按键
        /// </summary>
        public Key Key
        {
            get { return key; }
            set
            {
                key = value;
                hotKey = new HotKey(Key, ModifierKeys);
                ChangeProperty("Key");
                ChangeProperty("ModifierKeys");
                ChangeProperty("HotKey");
            }
        }
        /// <summary>
        /// 多按键
        /// </summary>
        public ModifierKeys ModifierKeys
        {
            get { return modifierKeys; }
            set
            {
                modifierKeys = value;
                hotKey = new HotKey(Key, ModifierKeys);
                ChangeProperty("ModifierKeys");
                ChangeProperty("Key");
                ChangeProperty("HotKey");
            }
        }
        /// <summary>
        /// 快捷键按键
        /// </summary>
        [JsonIgnore]
        public HotKey HotKey
        {
            get { return hotKey; }
            set
            {
                hotKey = value;
                key = HotKey.Key;
                modifierKeys = HotKey.ModifierKeys;
                ChangeProperty("HotKey");
                ChangeProperty("Key");
                ChangeProperty("ModifierKeys");
            }
        }
        /// <summary>
        /// 构造函数
        /// </summary>
        public InputKey()
        {
            //HotKey = new HotKey(Key.Q, ModifierKeys.None);
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// 变更属性
        /// </summary>
        /// <param name="property"></param>
        public void ChangeProperty(string property)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
        }
    }
}
