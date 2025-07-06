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


        public event PropertyChangedEventHandler? PropertyChanged;
        private static readonly string _V = "BioIMA";



        public string Version { get; set; } = _V;



        public ShapeTypeColorStruct CreateTypeStruct { get; set; }



        public int NameIndex { get; set; }



        public ObservableCollection<ImageContentInfoModel> ImageModels { get; set; } = new ObservableCollection<ImageContentInfoModel>();



        [JsonIgnore]
        public ObservableCollection<ImageContentInfoModel> ImageStructModels { get; set; } = new ObservableCollection<ImageContentInfoModel>();



        public ProjectManager ProjectManager { get; set; } = new ProjectManager();



        private ImageContentInfoModel _imageContentInfoModel;









        public ImageContentInfoModel CurrentImageModel
        {
            get { return _imageContentInfoModel; }
            set
            {
                _imageContentInfoModel = value;
                if (_imageContentInfoModel != null)
                {
                    _imageContentInfoModel.LoadImageInfo(ToolConfig); 
                }
                OnPropertyChanged("CurrentImageModel");
            }
        }




        public int SelectedIndex { get; set; } = 0;




        public int TabViewSelectedIndex { get; set; } = 0;



        public ToolConfig ToolConfig { get; set; } = new ToolConfig();



        public string ConfigSavePath { get; set; }





        
        private async void MainModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName.Equals("TabViewSelectedIndex"))
            {
                if (TabViewSelectedIndex == 1 && ImageStructModels.Count == 0)
                {

                    ConvertToStructList();

                }
            }
        }






        public void ChangeProperty(string propertyName)
        {
            OnPropertyChanged(propertyName);
        }



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
                    LoadImages(ToolConfig.OpenUriHistory); 
                }
                Version = _V + ": " + this.ToolConfig.OpenUriHistory;
            }
        }









        public void LoadImages(string root)
        {
            ImageModels.Clear();
            ImageStructModels.Clear();

            ToolConfig.OpenUriHistory = root;
            foreach (var item in Directory.EnumerateFiles(root, "*.*", SearchOption.AllDirectories)
                        .Where(p => p.EndsWith(".png") || p.EndsWith(".bmp") || p.EndsWith(".jpg") || p.EndsWith(".jpeg")))
            {
                var model = new ImageContentInfoModel()
                {
                    ImageUri = new Uri(item),
                    FilePath = item.Replace(root, "").Substring(1),
                    ImagePath = item 
                };
                var target = ToolConfig.GetImageModelSavePath(model);
                if (File.Exists(target))
                {
                    try
                    {
                        var configModel = JsonConvert.DeserializeObject<ImageContentInfoModel>(File.ReadAllText(target, Encoding.UTF8), new ShapeAreaJsonConverter());
                        if (configModel is null) throw new Exception("¶ÁÈ¡Òì³£...");
                        model = configModel;
                        model.FilePath = item.Replace(root, "").Substring(1);
                        model.ImageUri = new Uri(model.GetAbsolutePath(ToolConfig));
                        model.ImagePath = item; 
                    }
                    catch (Exception) { }
                }
                ImageModels.Add(model);
            }
            UpdateShapeAndColor();
            Version = _V + ", : " + this.ToolConfig.OpenUriHistory;
            OnPropertyChanged("ImageModels");  

        }






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









        private bool InserNode(ImageContentInfoModel node, ImageContentInfoModel directory)
        {
            var root = directory.GetAbsolutePath(ToolConfig);
            var current = node.GetAbsolutePath(ToolConfig);

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

            foreach (var item in shapes)
            {

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

