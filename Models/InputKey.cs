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



    public class InputKey : INotifyPropertyChanged
    {
        private HotKey hotKey = null;
        private Key key = Key.Q;
        private ModifierKeys modifierKeys = ModifierKeys.None;



        public string Name { get; set; }



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



        public InputKey()
        {

        }

        public event PropertyChangedEventHandler? PropertyChanged;




        public void ChangeProperty(string property)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
        }
    }
}

