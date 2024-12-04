using wpf522.Expends;
using wpf522.Models.DrawShapes;
using wpf522.Models.Enums;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace wpf522.Models
{
    /// <summary>
    /// 一张图片信息
    /// </summary>
    public class ImageContentInfoModel : INotifyPropertyChanged
    {

        private BitmapSource _imageSource;
        public BitmapSource ImageSource
        {
            get { return _imageSource; }
            set
            {
                if (_imageSource != value)
                {
                    _imageSource = value;
                    ChangeProperty(nameof(ImageSource));  // 通知 UI 进行更新
                }
            }
        }

        /// <summary>
        /// 图片地址
        /// </summary>
        [JsonIgnore]
        public Uri? ImageUri { get; set; }
        /// <summary>
        /// 图片宽度
        /// </summary>
        public int ImageWidth { get; set; } = -1;
        /// <summary>
        /// 图片高度
        /// </summary>
        public int ImageHeight { get; set; } = -1;
        /// <summary>
        /// 文件路径
        /// </summary>
        public string FilePath { get; set; }
        /// <summary>
        /// 是否是目录
        /// </summary>
        public bool IsDirectory { get; set; } = false;
        /// <summary>
        /// 掩码文件路径
        /// </summary>
        public string MaskFilePath { get; set; }
        /// <summary>
        /// 配置文件路径
        /// </summary>
        /// 
        public string ConfigFilePath { get; set; }
        /// <summary>
        /// 是否是远程文件
        /// </summary>
        public bool IsRemote => !string.IsNullOrEmpty(ImageUri?.Host);
        /// <summary>
        /// 名称
        /// </summary>
        public String Name => System.IO.Path.GetFileName(FilePath);
        /// <summary>
        /// 所有的绘制的形状
        /// </summary>
        public ObservableCollection<ShapeArea> Shapes { get; set; } = new AsyncObservableCollection<ShapeArea>();
        /// <summary>
        /// 形状数量
        /// </summary>
        [JsonIgnore]
        public int ShapeCount { get; set; } = 0;
        /// <summary>
        /// 子元素数量
        /// </summary>
        [JsonIgnore]
        public int ChildrenCount { get; set; } = 0;
        /// <summary>
        /// 子元素已经完成框选的数量
        /// </summary>
        [JsonIgnore]
        public int ChildrenComplieCount { get; set; } = 0;
        /// <summary>
        /// 子列表
        /// </summary>
        [JsonIgnore]
        public ObservableCollection<ImageContentInfoModel> Children { get; set; } = new ObservableCollection<ImageContentInfoModel>();
        /// <summary>
        /// 父节点
        /// </summary>
        [JsonIgnore]
        public ImageContentInfoModel ParentNode { get; set; }
        /// <summary>
        /// 当前被选中的形状
        /// </summary>
        [JsonIgnore]
        public ShapeArea SelectedShape { get; set; }
        public string ImagePath { get; internal set; }

        public event PropertyChangedEventHandler? PropertyChanged;


        public ImageContentInfoModel()
        {
            Shapes.CollectionChanged += (o, e) =>
            {
                ShapeCount = Shapes.Count;
                if (ParentNode != null)
                {
                    ParentNode.RefreshComplieNodes();
                }
            };

            Children.CollectionChanged += (o, e) => {
                GetChildrenCount();
            };
        }
        /// <summary>
        /// 让当前对象中的某个字段发生变更通知
        /// </summary>
        public void ChangeProperty(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        /// <summary>
        /// 根据类型的名称进行执行属性变更通知
        /// </summary>
        /// <param name="propertyName"></param>
        /// <param name="byTypeName"></param>
        public void ChangeShapeProperty(string propertyName, string byTypeName)
        {
            foreach (var item in Shapes)
            {
                if (item.TypeName.Equals(byTypeName))
                {
                    item.ChangeProperty(propertyName);
                }
            }
        }
        /// <summary>
        /// 带索引格式的图片像素类型
        /// </summary>
        private static PixelFormat[] PixelFormatArray = {
                                            PixelFormat.Format1bppIndexed
                                            ,PixelFormat.Format4bppIndexed
                                            ,PixelFormat.Format8bppIndexed
                                            ,PixelFormat.Undefined
                                            ,PixelFormat.DontCare
                                            ,PixelFormat.Format16bppArgb1555
                                            ,PixelFormat.Format16bppGrayScale
                                        };
        /// <summary>
        /// 判断图片是否索引像素格式,是否是引发异常的像素格式
        /// </summary>
        /// <param name="imagePixelFormat">图片的像素格式</param>
        /// <returns></returns>
        private static bool IsIndexedPixelFormat(System.Drawing.Imaging.PixelFormat imagePixelFormat)
        {

            foreach (PixelFormat pf in PixelFormatArray)
            {
                if (imagePixelFormat == pf)
                {
                    return true;
                }
            }
            return false;
        }
        /// <summary>
        /// 获取原始图像
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        private static Bitmap CreateOrgBitmap(string filePath)
        {
            Bitmap map = new Bitmap(filePath);
            if (IsIndexedPixelFormat(map.PixelFormat) == false)
            {
                return map;
            }
            Bitmap bitmap = new Bitmap(map.Width, map.Height, PixelFormat.Format24bppRgb);
            using (Graphics g = Graphics.FromImage(bitmap))
            {
                g.DrawImage(map, new Point(0, 0));
            }
            map.Dispose();
            return bitmap;
        }
        /// <summary>
        /// 保存当前文件到配置文件
        /// </summary>
        /// <param name="toolConfig"></param>
        /// <param name="exportMask"></param>
        public void SaveConfig(ToolConfig toolConfig, bool exportMask = true)
        {
            string root = toolConfig.OpenUriHistory;
            string targetFile = toolConfig.GetImageModelSavePath(this).CheckPath();
            if (ImageUri is null)
            {
                return;
            }

            // 判断是否创建遮罩
            if (toolConfig.IsCreateMaskImage && exportMask)
            {
                // 创建遮罩的路径
                MaskFilePath = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(targetFile).ToString(),
                    System.IO.Path.GetFileNameWithoutExtension(targetFile) + "_mask" + System.IO.Path.GetExtension(FilePath)).CheckPath();

                using (Bitmap map = CreateOrgBitmap(GetAbsolutePath(toolConfig)))
                {
                    var black = new SolidBrush(Color.Black);
                    var white = new SolidBrush(Color.White);
                    using (Graphics g = Graphics.FromImage(map))
                    {
                        g.FillRectangle(black, new Rectangle(0, 0, map.Width, map.Height));
                        foreach (var item in this.Shapes)
                        {
                            if (item is ShapeBox)
                            {
                                var box = item as ShapeBox;
                                g.FillRectangle(white, new Rectangle((int)box.X, (int)box.Y, (int)box.Width, (int)box.Height));
                            }
                            else if (item is ShapePolygon)
                            {
                                var polygon = item as ShapePolygon;
                                Point[] points = new Point[polygon.Points.Count];
                                for (int i = 0; i < points.Length; i++)
                                {
                                    points[i] = new Point((int)(polygon.Points[i].X + polygon.StartX), (int)(polygon.Points[i].Y + polygon.StartY));
                                }
                                g.FillPolygon(white, points);

                            }
                            else if (item is ShapeLines)
                            {
                                var line = item as ShapeLines;
                                // 计算线条的端点
                                Point start = new Point((int)line.StartX, (int)line.StartY);
                                Point end = new Point((int)line.EndX, (int)line.EndY);
                                // 设置线条的宽度
                                float lineWidth = 2.0f; // 可以调整宽度
                                                        // 创建画笔
                                using (var pen = new Pen(white, lineWidth))
                                {
                                    // 绘制线条
                                    g.DrawLine(pen, start, end);
                                }
                            }

                        }
                    }
                    map.Save(MaskFilePath);
                }
            }
            ConfigFilePath = targetFile;
            using (FileStream stream = File.Open(targetFile, FileMode.Create))
            {
                var settings = new JsonSerializerSettings()
                {
                    Formatting = Formatting.Indented,
                };
                byte[] content = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(this, settings));
                stream.Write(content);
            }
        }
        /// <summary>
        /// 加载信息
        /// </summary>
        /// <param name="toolConfig"></param>
        public void LoadConfig(ToolConfig toolConfig)
        {

        }
        /// <summary>
        /// 读取图片信息
        /// </summary>
        public void LoadImageInfo(ToolConfig toolConfig)
        {
            var path = GetAbsolutePath(toolConfig);
            if (ImageUri == null)
            {
                this.ImageUri = new Uri(path);
            }
            if ((this.ImageWidth == -1 || this.ImageHeight == -1) && File.Exists(path))
            {
                using (Bitmap map = new Bitmap(path))
                {
                    this.ImageWidth = map.Width;
                    this.ImageHeight = map.Height;
                }
            }
        }


        /// <summary>
        /// 添加子元素
        /// </summary>
        /// <param name="model"></param>
        public void AddChild(ImageContentInfoModel model)
        {
            model.ParentNode = this;
            this.Children.Add(model);
        }
        /// <summary>
        /// 刷新已完成的子节点数量
        /// </summary>
        public void RefreshComplieNodes()
        {
            this.ChildrenComplieCount = GetChildrenComplieCount();
            if (ParentNode != null)
            {
                ParentNode.RefreshComplieNodes();
            }
        }

        /// <summary>
        /// 移除子元素
        /// </summary>
        /// <param name="model"></param>
        public void RemoveChild(ImageContentInfoModel model)
        {
            this.Children.Remove(model);
        }
        /// <summary>
        /// 获取文件的绝对路径地址
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        public string GetAbsolutePath(ToolConfig config) {
            return System.IO.Path.Combine(config.OpenUriHistory, FilePath);
        }
        /// <summary>
        /// 拷贝数据
        /// </summary>
        /// <param name="config"></param>
        public void CopyFrom(ImageContentInfoModel config)
        {
            this.Shapes.Clear();
            foreach (var item in config.Shapes)
            {
                this.Shapes.Add(item);
            }
            this.ConfigFilePath = config.ConfigFilePath;
            this.ImageHeight = config.ImageHeight;
            this.ImageWidth = config.ImageWidth;
            this.MaskFilePath = config.MaskFilePath;
        }

        /// <summary>
        /// 获取子节点数量
        /// </summary>
        /// <returns></returns>
        public void GetChildrenCount(ImageContentInfoModel model = null)
        {
            if (model is null)
            {
                model = this;
            }

            var count = 0;

            foreach (var item in model.Children)
            {
                if (item.IsDirectory)
                {
                    count += item.ChildrenCount;
                }
                else
                    count++;
            }

            ChildrenCount = count;
            if (ParentNode != null)
            {
                ParentNode.GetChildrenCount();
            }
        }

        /// <summary>
        /// 获取子节点数量
        /// </summary>
        /// <returns></returns>
        public int GetChildrenComplieCount(ImageContentInfoModel model = null)
        {
            if (model is null)
            {
                model = this;
            }

            var count = 0;

            foreach (var item in model.Children)
            {
                if (item.IsDirectory)
                {
                    count += item.ChildrenComplieCount;
                }
                else if (item.Shapes.Count > 0)
                {
                    count++;
                }
            }
            return count;
        }

        /// <summary>
        /// 转换所有的子形状到矩形框
        /// </summary>
        public void ConvertPolygonToRect()
        {
            var polygons = Shapes.Where(p => p is ShapePolygon).ToList();
            foreach (var item in polygons)
            {
                Shapes.Remove(item);
                Shapes.Add((item as ShapePolygon).ConvertToBox());
            }
        }
    }
}
