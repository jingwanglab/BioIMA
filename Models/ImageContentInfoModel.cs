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
                    ChangeProperty(nameof(ImageSource));  
                }
            }
        }



        [JsonIgnore]
        public Uri? ImageUri { get; set; }



        public int ImageWidth { get; set; } = -1;



        public int ImageHeight { get; set; } = -1;



        public string FilePath { get; set; }



        public bool IsDirectory { get; set; } = false;



        public string MaskFilePath { get; set; }




        public string ConfigFilePath { get; set; }



        public bool IsRemote => !string.IsNullOrEmpty(ImageUri?.Host);



        public String Name => System.IO.Path.GetFileName(FilePath);



        public ObservableCollection<ShapeArea> Shapes { get; set; } = new AsyncObservableCollection<ShapeArea>();



        [JsonIgnore]
        public int ShapeCount { get; set; } = 0;



        [JsonIgnore]
        public int ChildrenCount { get; set; } = 0;



        [JsonIgnore]
        public int ChildrenComplieCount { get; set; } = 0;



        [JsonIgnore]
        public ObservableCollection<ImageContentInfoModel> Children { get; set; } = new ObservableCollection<ImageContentInfoModel>();



        [JsonIgnore]
        public ImageContentInfoModel ParentNode { get; set; }



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



        public void ChangeProperty(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }





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



        private static PixelFormat[] PixelFormatArray = {
                                            PixelFormat.Format1bppIndexed
                                            ,PixelFormat.Format4bppIndexed
                                            ,PixelFormat.Format8bppIndexed
                                            ,PixelFormat.Undefined
                                            ,PixelFormat.DontCare
                                            ,PixelFormat.Format16bppArgb1555
                                            ,PixelFormat.Format16bppGrayScale
                                        };





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





        public void SaveConfig(ToolConfig toolConfig, bool exportMask = true)
        {
            string root = toolConfig.OpenUriHistory;
            string targetFile = toolConfig.GetImageModelSavePath(this).CheckPath();
            if (ImageUri is null)
            {
                return;
            }

            if (toolConfig.IsCreateMaskImage && exportMask)
            {

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

                                Point start = new Point((int)line.StartX, (int)line.StartY);
                                Point end = new Point((int)line.EndX, (int)line.EndY);

                                float lineWidth = 2.0f; 

                                using (var pen = new Pen(white, lineWidth))
                                {

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




        public void LoadConfig(ToolConfig toolConfig)
        {

        }



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




        public void AddChild(ImageContentInfoModel model)
        {
            model.ParentNode = this;
            this.Children.Add(model);
        }



        public void RefreshComplieNodes()
        {
            this.ChildrenComplieCount = GetChildrenComplieCount();
            if (ParentNode != null)
            {
                ParentNode.RefreshComplieNodes();
            }
        }




        public void RemoveChild(ImageContentInfoModel model)
        {
            this.Children.Remove(model);
        }





        public string GetAbsolutePath(ToolConfig config) {
            return System.IO.Path.Combine(config.OpenUriHistory, FilePath);
        }




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

