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
        /// <summary>
        /// 之前打开的文件夹历史
        /// </summary>
        public string OpenUriHistory { get; set; }
        /// <summary>
        /// 之前的保存文件对应配置的路径
        /// </summary>
        public string SaveFileConfigHistory { get; set; }
        /// <summary>
        /// 当前的绘制状态
        /// </summary>
        public ShapeType CurrentShapType { get; set; }
        /// <summary>
        /// 是否创建掩码图片
        /// </summary>
        public bool IsCreateMaskImage { get; set; }

        #region 快捷键
        /// <summary>
        /// 绘制矩形命令
        /// </summary>
        public InputKey DrawBoxKeyCommand { get; set; } = new InputKey() { Name = "Draw Box", HotKey = new HotKey(Key.Q, ModifierKeys.Control) };

        /// <summary>
        /// 绘制多边形命令
        /// </summary>
        public InputKey DrawPolygonKeyCommand { get; set; } = new InputKey() { Name = "Draw Polygon", HotKey = new HotKey(Key.W, ModifierKeys.Control) };
        /// <summary>
        /// 框选选中命令
        /// </summary>
        public InputKey DrawSelectedKeyCommand { get; set; } = new InputKey() { Name = "Select Box", HotKey = new HotKey(Key.E, ModifierKeys.Control) };
        /// <summary>
        /// 设置没有操作模式
        /// </summary>
        public InputKey DrawNoneKeyCommand { get; set; } = new InputKey() { Name = "No Operation", HotKey = new HotKey(Key.R, ModifierKeys.Control) };
        /// <summary>
        /// 保存配置命令
        /// </summary>
        public InputKey SaveConfigKeyCommand { get; set; } = new InputKey() { Name = "Save Config", HotKey = new HotKey(Key.S, ModifierKeys.Control) };
        /// <summary>
        /// 删除选中的命令
        /// </summary>
        public InputKey RemoveSelectedShapesKeyCommand { get; set; } = new InputKey() { Name = "Delete Selected", HotKey = new HotKey(Key.Delete) };

        /// <summary>
        /// 下一张图片
        /// </summary>
        public InputKey NextImageKeyCommand { get; set; } = new InputKey() { Name = "Next Image", HotKey = new HotKey(Key.D) };

        /// <summary>
        /// 前一张命令
        /// </summary>
        public InputKey PreviousKeyCommand { get; set; } = new InputKey() { Name = "Previous Image", HotKey = new HotKey(Key.A) };
        /// <summary>
        /// 属性变更
        /// </summary>
        public event PropertyChangedEventHandler? PropertyChanged;
        #endregion
        /// <summary>
        /// 类型和颜色的映射
        /// </summary>
        public ObservableCollection<ShapeTypeColorStruct> ShapeTypeColorStructs { get; set; } = new ObservableCollection<ShapeTypeColorStruct>();
        /// <summary>
        /// 当前类型名称
        /// </summary>
        [JsonIgnore]
        public ShapeTypeColorStruct CurrentTypeName { get; set; }
        /// <summary>
        /// 项目名称
        /// </summary>
        public string ProjectName { get; set; } = "";
        /// <summary>
        /// 根据图片模型获取保存的配置文件路径
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
         /// 颜色
        public static bool IsColorPicking { get; set; } = false;

        public string GetImageModelSavePath(ImageContentInfoModel model)
        {
            string targetFile = "";
            if (SaveFileConfigHistory is null) return null;
            if (model.IsRemote)
            {
                targetFile = System.IO.Path.Combine(SaveFileConfigHistory, System.IO.Path.GetFileNameWithoutExtension(model.GetAbsolutePath(this)) + ".json");
                //FilePath = this.ImageUri.AbsoluteUri;
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
                //FilePath = this.ImageUri.LocalPath;
            }
            return targetFile;
        }
        /// <summary>
        /// 获取当前正在操作的类型
        /// </summary>
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
        /// <summary>
        /// 预定义颜色
        /// </summary>
        public static List<Color> StaticColors = new List<Color>();
        public static Random _Random = new Random();
        /// <summary>
        /// 获取随机的颜色
        /// </summary>
        /// <returns></returns>
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

        /// <summary>
        /// 获取一个形状的类型序号
        /// </summary>
        /// <param name="shape"></param>
        /// <returns></returns>
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
