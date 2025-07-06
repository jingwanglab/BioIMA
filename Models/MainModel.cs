using wpf522.CustomCommand;
using wpf522.CustomDialogs;
using wpf522.Expends;
using wpf522.Models.DrawShapes;
using wpf522.Models.Enums;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace wpf522.Models
{

    public partial class MainModel : INotifyPropertyChanged
    {


        public event PropertyChangedEventHandler? PropertyChanged;///PropertyChanged事件用于通知属性更改
        private static readonly string _V = "BioIMA";
        /// <summary>
        /// 版本号
        /// </summary>
        public string Version { get; set; } = _V;///Version自动属性，初始值为 _V，版本号。
        
        /// <summary>
        /// 创建的类型
        /// </summary>
        public ShapeTypeColorStruct CreateTypeStruct { get; set; }
      
        /// <summary>
        /// NameIndex 是一个整数属性，用于存储名称序号。
        /// </summary>
        public int NameIndex { get; set; }
        /// <summary>
        /// 所有的图片模型
        /// </summary>
        public ObservableCollection<ImageContentInfoModel> ImageModels { get; set; } = new ObservableCollection<ImageContentInfoModel>();
        /// <summary>
        /// 所有的图片模型
        /// </summary>
        [JsonIgnore]
        public ObservableCollection<ImageContentInfoModel> ImageStructModels { get; set; } = new ObservableCollection<ImageContentInfoModel>();
        /// <summary>
        /// 项目管理
        /// </summary>
        public ProjectManager ProjectManager { get; set; } = new ProjectManager();
        /// <summary>
        /// _imageContentInfoModel是一个私有字段，用于存储当前选中的图片模型
        /// </summary>
        private ImageContentInfoModel _imageContentInfoModel;
        /// <summary>
        /// 当前图片模型数据，CurrentImageModel用于获取和设置当前图片模型，并在设置时触发 PropertyChanged 事件。
        /// </summary>
        //public ImageContentInfoModel CurrentImageModel { get { return _imageContentInfoModel; }
        //    set {
        //        _imageContentInfoModel = value;
        //        _imageContentInfoModel.LoadImageInfo(ToolConfig);
        //        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("CurrentImageModel"));
        //    } }
        public ImageContentInfoModel CurrentImageModel
        {
            get { return _imageContentInfoModel; }
            set
            {
                _imageContentInfoModel = value;
                if (_imageContentInfoModel != null)
                {
                    _imageContentInfoModel.LoadImageInfo(ToolConfig); // 加载图像信息
                }
                OnPropertyChanged("CurrentImageModel");
            }
        }

        /// <summary>
        /// 选中的序号
        /// </summary>
        /// 
        public int SelectedIndex { get; set; } = 0;
        /// <summary>
        /// 页面显示切换视图序号
        /// </summary>
        /// 
        public int TabViewSelectedIndex { get; set; } = 0;
        /// <summary>
        /// 工具配置文件
        /// </summary>
        public ToolConfig ToolConfig { get; set; } = new ToolConfig();
        /// <summary>
        /// 配置文件
        /// </summary>
        public string ConfigSavePath { get; set; }

        /// <summary>
        /// 属性值变更事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        
        private async void MainModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName.Equals("TabViewSelectedIndex"))
            {
                if (TabViewSelectedIndex == 1 && ImageStructModels.Count == 0)
                {
                    //MainWindow.Instance.ShowWait();
                    ConvertToStructList();
                    //MainWindow.Instance.CloseWait();
                }
            }
        }

        /// <summary>
        /// 让当前对象中的某个字段发生变更通知
        /// </summary>
        //public void ChangeProperty(string propertyName) {
        //    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        //}
        public void ChangeProperty(string propertyName)
        {
            OnPropertyChanged(propertyName);
        }
        /// <summary>
        /// 保存配置文件
        /// </summary>
        public void SaveConfig()
        {
            using (FileStream stream = File.Open(ConfigSavePath, FileMode.Create))
            {
                var settings = new JsonSerializerSettings()
                {
                    Formatting = Formatting.Indented,
                };
                byte[] datas = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(ToolConfig, settings));
                stream.Write(datas);
            }
        }
        /// <summary>
        /// 检查是否存在配置文件，如果存在则读取文件内容。
        /// 使用 JsonConvert.DeserializeObject() 方法将文件内容反序列化为 ToolConfig 对象如果成功反序列化
        /// 将新的配置信息赋给 ToolConfig 属性。
        /// 检查 ToolConfig.OpenUriHistory 是否为空，如果不为空则调用 LoadImages() 方法加载图片。
        /// 最后更新版本信息 Version，包括当前版本和打开的路径信息。
        /// </summary>
        public void LoadConfig()
        {
            if (File.Exists(ConfigSavePath))
            {
                string content = File.ReadAllText(ConfigSavePath, encoding: Encoding.UTF8);
                ToolConfig config = JsonConvert.DeserializeObject<ToolConfig>(content);
                if (config is not null)
                {
                    this.ToolConfig = config;
                }
                if (string.IsNullOrEmpty(this.ToolConfig.OpenUriHistory) == false)
                {
                    LoadImages(ToolConfig.OpenUriHistory); // 加载图像
                }
                Version = _V + ": " + this.ToolConfig.OpenUriHistory;
            }
        }
        /// <summary>
        /// 加载图片列表
        /// 清空 ImageModels 和 ImageStructModels 集合，设置 ToolConfig.OpenUriHistory 为传入的根目录
        /// 遍历根目录下的所有文件，筛选出图片文件，并为每个文件创建一个 ImageContentInfoModel 对象
        /// 如果存在针对该图片的配置文件，则尝试读取配置信息并应用到 ImageContentInfoModel 对象中。
        /// 将创建好的 ImageContentInfoModel 添加到 ImageModels 集合中。
        /// 最后调用 UpdateShapeAndColor() 方法更新形状和颜色，并更新版本信息 Version。
        /// </summary>
        /// <param name="root"></param>
        public void LoadImages(string root)
        {
            ImageModels.Clear();
            ImageStructModels.Clear();
            // 设置打开的根目录
            ToolConfig.OpenUriHistory = root;
            foreach (var item in Directory.EnumerateFiles(root, "*.*", SearchOption.AllDirectories)
                        .Where(p => p.EndsWith(".png") || p.EndsWith(".bmp") || p.EndsWith(".jpg") || p.EndsWith(".jpeg")))
            {
                var model = new ImageContentInfoModel()
                {
                    ImageUri = new Uri(item),
                    FilePath = item.Replace(root, "").Substring(1),
                    ImagePath = item // 设置 ImagePath 属性
                };
                var target = ToolConfig.GetImageModelSavePath(model);
                if (File.Exists(target))
                {
                    try
                    {
                        var configModel = JsonConvert.DeserializeObject<ImageContentInfoModel>(File.ReadAllText(target, Encoding.UTF8), new ShapeAreaJsonConverter());
                        if (configModel is null) throw new Exception("读取异常...");
                        model = configModel;
                        model.FilePath = item.Replace(root, "").Substring(1);
                        model.ImageUri = new Uri(model.GetAbsolutePath(ToolConfig));
                        model.ImagePath = item; // 设置 ImagePath 属性
                    }
                    catch (Exception) { }
                }
                ImageModels.Add(model);
            }
            UpdateShapeAndColor();
            Version = _V + ", : " + this.ToolConfig.OpenUriHistory;
            OnPropertyChanged("ImageModels");  // 触发 UI 更新
            // 引发 ImagesLoaded 事件
        }
        

        /// <summary>
        /// 清空 ImageStructModels 集合。
        /// 创建一个名为 rootNode 的根节点 ImageContentInfoModel 对象。
        /// 遍历 ImageModels 集合中的每个 ImageContentInfoModel 对象，调用 InsertNode() 方法将其插入到树状结构中
        /// 将根节点 rootNode 的子节点添加到 ImageStructModels 集合中，形成结构化的列表
        /// </summary>
        private void ConvertToStructList()
        {
            ImageStructModels.Clear();
            ImageContentInfoModel rootNode = new ImageContentInfoModel()
            {
                IsDirectory = true,
                FilePath = ""
            };
            foreach (var item in ImageModels)
            {
                InserNode(item, rootNode);
            }
            foreach (var item in rootNode.Children)
            {
                ImageStructModels.Add(item);
            }
        }
        /// <summary>
        /// 向目录结构中插入节点，以构建一个逻辑上的目录结构。
        /// 首先获取 directory 目录的绝对路径和 node 节点的绝对路径
        /// 检查 current 节点的路径是否包含 root 目录的路径。
        /// 如果路径包含，遍历 directory 目录下的所有子节点，如果是目录则递归调用 InsertNode() 方法。
        /// 分割 root 和 current 的路径，然后循环构建目录结构，创建目录节点并添加到相应位置。
        /// 最后将 node 节点添加到目录结构中并返回 true。
        /// </summary>
        /// <param name="node"></param>
        private bool InserNode(ImageContentInfoModel node, ImageContentInfoModel directory)
        {
            var root = directory.GetAbsolutePath(ToolConfig);
            var current = node.GetAbsolutePath(ToolConfig);

            // 路径是否包含
            if (current.Contains(root))
            {
                foreach (var item in directory.Children.Where(p => p.IsDirectory))
                {
                    var res = InserNode(node, item);
                    if (res) return true;
                }
                var rootPathNodes = root.Split('\\');
                var currentPathNodes = current.Split('\\');
                var dir = directory;
                var startRootPath = root;
                // 循环构建目录结构
                for (int i = rootPathNodes.Length; i < currentPathNodes.Length - 1; i++)
                {
                    var d = new ImageContentInfoModel()
                    {
                        IsDirectory = true,
                        FilePath = Path.Combine(startRootPath, currentPathNodes[i])
                    };
                    startRootPath = d.FilePath;
                    dir.AddChild(d);
                    dir.RefreshComplieNodes();
                    dir = d;
                }
                dir.AddChild(node);
                dir.RefreshComplieNodes();
                return true;
            }
            else
            {
                return false;
            }

        }
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        /// <summary>
        /// 更新本地颜色和形状(当加载的数据中默认没有对应的需要的形状和颜色)
        /// 首先创建一个 shapes 列表用于存储所有形状信息
        /// 遍历每个 ImageModels 中的 ImageContentInfoModel 对象，并将其中的形状信息添加到 shapes 列表中。
        /// 对 shapes 列表进行去重操作。
        /// 遍历 shapes 列表中的每个形状，检查是否存在与该形状对应的颜色信息，
        /// 如果不存在则创建一个新的颜色信息添加到 ToolConfig.ShapeTypeColorStructs 中。
        /// </summary>
        private void UpdateShapeAndColor()
        {
            var shapes = new List<ShapeArea>();
            foreach (var item in ImageModels)
            {
                foreach (var shape in item.Shapes)
                {
                    shapes.Add(shape);
                }
            }
            shapes.Distinct(new ShapeAreaIEqualityComparer());
            Random r = new Random();
            // 遍历
            foreach (var item in shapes)
            {
                // 如果不存在则创建
                if(ToolConfig.ShapeTypeColorStructs.Where(p => p.TypeName.Equals(item.TypeName)).Count() == 0)
                {
                    ToolConfig.ShapeTypeColorStructs.Add(new ShapeTypeColorStruct()
                    {
                        Color = Color.FromRgb((byte)r.Next(0, 255), (byte)r.Next(0, 255), (byte)r.Next(0, 255)).ColorToHexARGB(),
                        TypeName = item.TypeName,
                    });
                }
            }
        }
    }
}
