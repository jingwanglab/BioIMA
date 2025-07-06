using wpf522.Expends;
using wpf522.Models.Enums;
using MahApps.Metro.Controls;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media;

namespace wpf522.Models
{
    public class ToolConfig : INotifyPropertyChanged
    {



        public string OpenUriHistory { get; set; }



        public string SaveFileConfigHistory { get; set; }



        public ShapeType CurrentShapType { get; set; }



        public bool IsCreateMaskImage { get; set; }

        #region ¿ì½Ý¼ü



        public InputKey DrawBoxKeyCommand { get; set; } = new InputKey() { Name = "Draw Box", HotKey = new HotKey(Key.Q, ModifierKeys.Control) };



        public InputKey DrawPolygonKeyCommand { get; set; } = new InputKey() { Name = "Draw Polygon", HotKey = new HotKey(Key.W, ModifierKeys.Control) };



        public InputKey DrawSelectedKeyCommand { get; set; } = new InputKey() { Name = "Select Box", HotKey = new HotKey(Key.E, ModifierKeys.Control) };



        public InputKey DrawNoneKeyCommand { get; set; } = new InputKey() { Name = "No Operation", HotKey = new HotKey(Key.R, ModifierKeys.Control) };



        public InputKey SaveConfigKeyCommand { get; set; } = new InputKey() { Name = "Save Config", HotKey = new HotKey(Key.S, ModifierKeys.Control) };



        public InputKey RemoveSelectedShapesKeyCommand { get; set; } = new InputKey() { Name = "Delete Selected", HotKey = new HotKey(Key.Delete) };



        public InputKey NextImageKeyCommand { get; set; } = new InputKey() { Name = "Next Image", HotKey = new HotKey(Key.D) };



        public InputKey PreviousKeyCommand { get; set; } = new InputKey() { Name = "Previous Image", HotKey = new HotKey(Key.A) };



        public event PropertyChangedEventHandler? PropertyChanged;
        #endregion



        public ObservableCollection<ShapeTypeColorStruct> ShapeTypeColorStructs { get; set; } = new ObservableCollection<ShapeTypeColorStruct>();



        [JsonIgnore]
        public ShapeTypeColorStruct CurrentTypeName { get; set; }



        public string ProjectName { get; set; } = "";






        public static bool IsColorPicking { get; set; } = false;

        public string GetImageModelSavePath(ImageContentInfoModel model)
        {
            string targetFile = "";
            if (SaveFileConfigHistory is null) return null;
            if (model.IsRemote)
            {
                targetFile = System.IO.Path.Combine(SaveFileConfigHistory, System.IO.Path.GetFileNameWithoutExtension(model.GetAbsolutePath(this)) + ".json");

            }
            else
            {
                string[] fileEndPath = null;
                if (model.FilePath.Contains(":"))
                {
                    fileEndPath = model.FilePath.Replace(this.OpenUriHistory, "").Substring(1).Split("\\");
                }
                else
                {
                    fileEndPath = model.FilePath.Split('\\');
                }

                var basePath = SaveFileConfigHistory;
                for (int i = 0; i < fileEndPath.Length - 1; i++)
                {
                    var path = System.IO.Path.Combine(basePath, fileEndPath[i]);
                    if (Directory.Exists(path) == false)
                    {
                        Directory.CreateDirectory(path);
                    }
                    basePath = path;
                }
                targetFile = System.IO.Path.Combine(basePath,
                    System.IO.Path.GetFileNameWithoutExtension(model.GetAbsolutePath(this)) + ".json");

            }
            return targetFile;
        }



        public ShapeTypeColorStruct GetCurrentTypeName()
        {
            if(CurrentTypeName is null)
            {
                if (ShapeTypeColorStructs.Count > 0)
                {
                    CurrentTypeName = ShapeTypeColorStructs.First();
                }
                else
                {
                    ShapeTypeColorStructs.Add(new ShapeTypeColorStruct() { TypeName = "label1", Color = GetRandomColor() });
                    CurrentTypeName = ShapeTypeColorStructs.First();
                }
            }
            return CurrentTypeName;
        }



        public static List<Color> StaticColors = new List<Color>();
        public static Random _Random = new Random();




        public static string GetRandomColor()
        {
            if (StaticColors.Count == 0)
            {
                var type = typeof(Colors);
                var colors = from p in type.GetProperties(BindingFlags.Static | BindingFlags.Public | BindingFlags.GetProperty)
                             select (Color)p.GetValue(null);
                StaticColors.AppendRange(colors);
            }
            return StaticColors[_Random.Next(0, StaticColors.Count)].ColorToHexARGB();
        }





        public int GetTypeIndex(ShapeArea shape)
        {
            for (int i = 0; i < ShapeTypeColorStructs.Count; i++)
            {
                if (ShapeTypeColorStructs[i].TypeName.Equals(shape.TypeName))
                {
                    return i;
                }
            }

            return -1;
        }
    }
}

