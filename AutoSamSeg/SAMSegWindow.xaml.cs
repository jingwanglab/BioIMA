using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.Linq;
using System.Text;
using wpf522.AutoSamSeg;
using System.Collections.ObjectModel;
using System.Windows.Shapes;
using System.Threading.Tasks;

namespace wpf522
{
    /// <summary>
    /// SAMSegWindow.xaml 的交互逻辑
    /// </summary>
    public partial class SAMSegWindow : Window
    {

        // 图像文件路径
        private string mImagePath = string.Empty;
        private int _maskPixelCount;

        SAM mSam = SAM.Instance();
        CLIP mCLIP = CLIP.Instance();
        List<Promotion> mPromotionList = new List<Promotion>();
        float[] mImgEmbedding;
        private RectAnnotation mCurRectAnno;
        private Point _startPoint;
        int mOrgwid;
        int mOrghei;
        //undo and redo
        private Stack<Promotion> mUndoStack = new Stack<Promotion>();
        private Stack<Promotion> mRedoStack = new Stack<Promotion>();
        Dispatcher UI;
        SAMAutoMask mAutoMask;
        MaskData mAutoMaskData;
        Operation mCurOp;



        private Point? _rulerStartPoint = null;
        private Line _rulerLine;
        private TextBlock _rulerText;
        private Ellipse _startPointEllipse, _endPointEllipse;
        private double _pixelToRealRatio = 1.0; // 像素与实际单位的比例
        public event Action<double> SaveArea;

        public ObservableCollection<DataModel> DataCollection { get; set; } = new ObservableCollection<DataModel>();

        private int _currentId = 1; // 用于生成唯一的ID

        private string _unit = "cm"; // 用于保存用户设定的单位这里假设默认单位是 "cm"，实际情况会在用户输入后更新
        private string _currentFileName;

        public class DataModel
        {
            public string ID { get; set; }
            public double Length { get; set; }
            public double Area { get; set; }
            public double Pixels { get; set; }
        }

        //MaskData maskData = new MaskData(); // 在方法内部初始化
        public enum Mode
        {
            None,
            SettingRuler,
            CreatingHints
        }
        private Mode currentMode = Mode.None;
        // 构造函数
        //public SAMSegWindow()
        //{
        //    InitializeComponent();

        //    this.mImage.Width = this.Width;
        //    this.mImage.Height = this.Height;

        //    this.mMask.Width = this.Width;
        //    this.mMask.Height = this.Height;

        //    this.UI = Dispatcher.CurrentDispatcher;
        //    this.mCurOp = Operation.None;
        //    // 设置初始操作模式为 None
        //    this.currentMode = Mode.None;
        //    //// 初始化操作类型为无效状态
        //    //this.mCurOp = Operation.None;
        //    //this.mOpType = (OpType)SamOpType.None;
        //}
        //public SAMSegWindow()
        //{
        //    InitializeComponent();

        //    // 将 Image 和 Mask 的 Stretch 设置为 Uniform 确保等比例缩放
        //    this.mImage.Stretch = Stretch.Uniform;
        //    this.mMask.Stretch = Stretch.Uniform;

        //    this.UI = Dispatcher.CurrentDispatcher;
        //    this.mCurOp = Operation.None;
        //    this.currentMode = Mode.None;
        //}


        ///// <summary>
        /////// 加载图像
        /////// </summary>
        //void LoadImage(string imgpath)
        //{
        //    BitmapImage bitmap = new BitmapImage(new Uri(imgpath));
        //    this.mOrgwid = (int)bitmap.Width;
        //    this.mOrghei = (int)bitmap.Height;
        //    this.mImage.Source = bitmap;//显示图像


        //}这里开始是原始方法看这里aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa
        //public SAMSegWindow()
        //{
        //    InitializeComponent();

        //    this.mImage.Width = 1f * this.Width;
        //    this.mImage.Height = this.Height;

        //    this.mMask.Width = 1f * this.Width;
        //    this.mMask.Height = this.Height;

        //    this.UI = Dispatcher.CurrentDispatcher;
        //    this.mCurOp = Operation.None;
        //    // 设置初始操作模式为 None
        //    this.currentMode = Mode.None;
        //    //// 初始化操作类型为无效状态
        //    //this.mCurOp = Operation.None;
        //    //this.mOpType = (OpType)SamOpType.None;
        //}
        //void LoadImage(string imgpath)
        //{
        //    // 加载原始图像以获取其宽度和高度
        //    BitmapImage bitmap = new BitmapImage(new Uri(imgpath));
        //    this.mOrgwid = (int)bitmap.Width;
        //    this.mOrghei = (int)bitmap.Height;

        //    this.mImage.Source = bitmap;
        //}
        public SAMSegWindow()
        {
            InitializeComponent();
            this.MouseUp += SAMSegWindow_MouseUp;
            // 将 Image 和 Mask 的 Stretch 设置为 Uniform 确保等比例缩放
            this.mImage.Stretch = Stretch.Uniform;
            this.mMask.Stretch = Stretch.Uniform;

            this.UI = Dispatcher.CurrentDispatcher;
            this.mCurOp = Operation.None;
            this.currentMode = Mode.None;

            //// 添加鼠标事件处理
            //this.mImage.MouseRightButtonDown += Image_MouseRightButtonDown;
            //this.mImage.MouseMove += Image_MouseMove;
            //this.mImage.MouseRightButtonUp += Image_MouseRightButtonUp;
        }

        private Point _lastMousePosition;
        private bool _isDragging = false;
        private void SAMSegWindow_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (_isDragging)
            {
                _isDragging = false;
                this.mImage.ReleaseMouseCapture(); // 防止意外未释放
            }
        }
        //// 鼠标右键按下事件处理
        //private void Image_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        //{
        //    _lastMousePosition = e.GetPosition(this.ImgCanvas);
        //    _isDragging = true;
        //    this.mImage.CaptureMouse(); // 捕获鼠标事件
        //}

        //// 鼠标移动事件处理
        //private void Image_MouseMove(object sender, MouseEventArgs e)
        //{
        //    if (_isDragging)
        //    {
        //        Point currentMousePosition = e.GetPosition(this.ImgCanvas);
        //        Vector offset = currentMousePosition - _lastMousePosition;

        //        // 更新图像的平移变换
        //        UpdateTranslateTransform(this.mImage, offset);
        //        UpdateTranslateTransform(this.mMask, offset);

        //        _lastMousePosition = currentMousePosition; // 更新上次鼠标位置
        //    }
        //}

        //// 鼠标右键抬起事件处理
        //private void Image_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        //{
        //    _isDragging = false;
        //    this.mImage.ReleaseMouseCapture();
        //    e.Handled = true; // 阻止事件继续传播
        //}
        //// 更新平移变换
        //private void UpdateTranslateTransform(Image imageControl, Vector offset)
        //{
        //    if (imageControl.RenderTransform is TransformGroup transformGroup)
        //    {
        //        if (transformGroup.Children.Count > 1 && transformGroup.Children[1] is TranslateTransform translateTransform)
        //        {
        //            translateTransform.X += offset.X;
        //            translateTransform.Y += offset.Y;
        //        }
        //    }
        //}

        // 加载图像
        void LoadImage(string imgpath)
        {

            if (this.ImgCanvas.ActualWidth == 0 || this.ImgCanvas.ActualHeight == 0)
            {
                this.ImgCanvas.UpdateLayout(); // 强制更新布局
            }
            BitmapImage bitmap = new BitmapImage(new Uri(imgpath));
            this.mOrgwid = (int)bitmap.Width;
            this.mOrghei = (int)bitmap.Height;

            // 重置图像的变换
            ResetTransform(this.mImage);
            ResetTransform(this.mMask);

            // 计算适应屏幕的比例
            double scaleX = this.ImgCanvas.ActualWidth / bitmap.Width;
            double scaleY = this.ImgCanvas.ActualHeight / bitmap.Height;
            double scale = Math.Min(scaleX, scaleY); // 等比例缩放

            // 应用缩放
            SetScaleTransform(this.mImage, scale);
            SetScaleTransform(this.mMask, scale);

            // 更新图像源
            this.mImage.Source = bitmap;  // 显示图像

            // 重新设置掩膜的源
            // this.mMask.Source = ...; // 根据需要加载掩膜图像
        }

        // 重置变换
        private void ResetTransform(Image imageControl)
        {
            var transformGroup = new TransformGroup();
            transformGroup.Children.Add(new ScaleTransform()); // 用于缩放
            transformGroup.Children.Add(new TranslateTransform()); // 用于拖曳
            imageControl.RenderTransform = transformGroup;
        }

        // 设置缩放变换
        private void SetScaleTransform(Image imageControl, double scale)
        {
            if (imageControl.RenderTransform is TransformGroup transformGroup && transformGroup.Children.Count > 0)
            {
                if (transformGroup.Children[0] is ScaleTransform scaleTransform)
                {
                    scaleTransform.ScaleX = scale;
                    scaleTransform.ScaleY = scale;
                }
            }
        }

        // 将显示坐标映射到原始图像的坐标
        private Point DisplayToImageCoords(Point displayPoint)
        {
            if (this.mImage.RenderTransform is TransformGroup transformGroup &&
                transformGroup.Children[0] is ScaleTransform scaleTransform)
            {
                double scale = scaleTransform.ScaleX; // X和Y使用同样的比例缩放

                // 计算在原始图像中的对应坐标
                double xInOriginal = displayPoint.X / scale;
                double yInOriginal = displayPoint.Y / scale;

                return new Point(xInOriginal, yInOriginal);
            }

            return displayPoint; // 如果没有缩放，直接返回原始点
        }



        // 设置当前操作类型的方法
        private void SetOperationType(SAMSegWindow.SamOpType type)
        {
            SolidColorBrush brush = type == SamOpType.ADD ? Brushes.Red : Brushes.Black;

        }
        // 定义 OpType 枚举
        public enum SamOpType
        {
            None,   // 无效状态
            ADD,
            REMOVE
        }

        // 定义 mOpType 属性
        public OpType mOpType { get; set; }
        // 鼠标左键按下事件处理提示点程序
        private void image_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // 检查点击是否发生在 ImgCanvas 区域内
            if (!this.ImgCanvas.IsMouseOver)
            {
                // 如果点击不在 ImgCanvas 区域，直接返回，不执行任何操作
                return;
            }
            // 根据 currentMode 判断执行什么操作
            if (this.currentMode == Mode.SettingRuler)
            {
                // 在标尺模式下，执行标尺设置的逻辑
                SetRulerPoints(sender, e);
            }
            else if (this.currentMode == Mode.CreatingHints && this.mCurOp != Operation.None)
            {
                this.mImage.CaptureMouse();

                if (this.mCurOp == Operation.Point)
                {
                    SolidColorBrush brush = this.mOpType == OpType.ADD ? Brushes.Red : Brushes.Black;
                    PointAnnotation annotation = new PointAnnotation(brush);
                    Point canvasP = e.GetPosition(this.ImgCanvas);
                    annotation.Position = canvasP;
                    this.ImgCanvas.Children.Add(annotation);

                    Promotion promt = new PointPromotion(this.mOpType);
                    Point clickPoint = e.GetPosition(this.mImage);
                    Point orgImgPoint = this.Window2Image(clickPoint);
                    (promt as PointPromotion).X = (int)orgImgPoint.X;
                    (promt as PointPromotion).Y = (int)orgImgPoint.Y;

                    Transforms ts = new Transforms(1024);
                    PointPromotion ptn = ts.ApplyCoords((promt as PointPromotion), this.mOrgwid, this.mOrghei);
                    ptn.mAnation = annotation;
                    this.mUndoStack.Push(ptn);
                    this.mPromotionList.Add(ptn);

                    Thread thread = new Thread(() =>
                    {
                        MaskData md = this.mSam.Decode(this.mPromotionList, this.mImgEmbedding, this.mOrgwid, this.mOrghei);
                        this.ShowMask(md.mMask.ToArray(), Color.FromArgb((byte)100, (byte)255, (byte)100, (byte)0));
                    });
                    thread.Start();
                }
                else if (this.mCurOp == Operation.Box)
                {
                    _startPoint = e.GetPosition(this.ImgCanvas);
                    this.mCurRectAnno = new RectAnnotation
                    {
                        Width = 0,
                        Height = 0,
                        StartPosition = _startPoint
                    };
                    this.Reset();
                    this.ImgCanvas.Children.Add(this.mCurRectAnno);

                    Point clickPoint = e.GetPosition(this.mImage);
                    Point orgImgPoint = this.Window2Image(clickPoint);
                    this.mCurRectAnno.LeftUP = orgImgPoint;
                }
            }
        }

        // 鼠标移动事件处理程序
        private void image_MouseMove(object sender, MouseEventArgs e)
        {
            // 如果当前有选中的标注，处理拖动和调整大小操作
            if (e.LeftButton == MouseButtonState.Pressed && this.mCurRectAnno != null)
            {
                var currentPoint = e.GetPosition(this.ImgCanvas);
                var width = Math.Abs(currentPoint.X - _startPoint.X);
                var height = Math.Abs(currentPoint.Y - _startPoint.Y);
                this.mCurRectAnno.Width = width;
                this.mCurRectAnno.Height = height;
                Canvas.SetLeft(this.mCurRectAnno, Math.Min(_startPoint.X, currentPoint.X));
                Canvas.SetTop(this.mCurRectAnno, Math.Min(_startPoint.Y, currentPoint.Y));
            }
        }
        private void image_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            this.mImage.ReleaseMouseCapture();
            if (this.mCurRectAnno == null)
                return;

            Point clickPoint = e.GetPosition(this.mImage);
            Point orgImgPoint = this.Window2Image(clickPoint);
            this.mCurRectAnno.RightBottom = orgImgPoint;

            BoxPromotion promt = new BoxPromotion();
            (promt as BoxPromotion).mLeftUp.X = (int)this.mCurRectAnno.LeftUP.X;
            (promt as BoxPromotion).mLeftUp.Y = (int)this.mCurRectAnno.LeftUP.Y;

            (promt as BoxPromotion).mRightBottom.X = (int)this.mCurRectAnno.RightBottom.X;
            (promt as BoxPromotion).mRightBottom.Y = (int)this.mCurRectAnno.RightBottom.Y;

            Transforms ts = new Transforms(1024);
            var pb = ts.ApplyBox(promt, this.mOrgwid, this.mOrghei);
            pb.mAnation = this.mCurRectAnno;
            this.mUndoStack.Push(pb);
            this.mPromotionList.Add(pb);
            Thread thread = new Thread(() =>
            {
                MaskData md = this.mSam.Decode(this.mPromotionList, this.mImgEmbedding, this.mOrgwid, this.mOrghei);
                this.ShowMask(md.mMask.ToArray(), Color.FromArgb((byte)100, (byte)155, (byte)100, (byte)0));
            });
            thread.Start();
            this.mCurRectAnno = null;
        }

        /// <summary>
        /// 图像路径选择
        /// </summary>
        private void SelectFileButton_Click(object sender, RoutedEventArgs e)
        {
            // Create OpenFileDialog
            Microsoft.Win32.OpenFileDialog openFileDialog = new Microsoft.Win32.OpenFileDialog();

            // Set filter for file extension and default file extension
            openFileDialog.DefaultExt = ".png";
            openFileDialog.Filter = "Image files (*.jpg, *.jpeg, *.png)|*.jpg;*.jpeg;*.png|All files (*.*)|*.*";

            // Display OpenFileDialog by calling ShowDialog method
            Nullable<bool> result = openFileDialog.ShowDialog();

            // Get the selected file name and display in a TextBox
            if (result == true)
            {
                this.ImgPathTxt.Text = openFileDialog.FileName;
                this.mImagePath = this.ImgPathTxt.Text;

                if (!File.Exists(this.mImagePath))
                    return;


                // Set _currentFileName to the name of the selected file
                _currentFileName = System.IO.Path.GetFileName(this.mImagePath);


                this.LoadImgGrid.Visibility = Visibility.Collapsed;
                this.ImgCanvas.Visibility = Visibility.Visible;
                this.LoadImage(this.mImagePath);
                this.ShowStatus("Image Loaded");

                // 清除之前绘制的元素
                ClearDrawnElements(this.ImgCanvas);

                this.LoadImage(this.mImagePath);
                this.ShowStatus("Image Loaded");
                Thread thread = new Thread(() =>
                {
                    this.mSam.LoadONNXModel();//加载Segment Anything模型

                    UI.Invoke(new Action(delegate
                    {
                        this.ShowStatus("ONNX Model Loaded ✔");
                    }));
                    // 读取图像
                    OpenCvSharp.Mat image = OpenCvSharp.Cv2.ImRead(this.mImagePath, OpenCvSharp.ImreadModes.Color);
                    this.mImgEmbedding = this.mSam.Encode(image, this.mOrgwid, this.mOrghei);//Image Embedding

                    this.mAutoMask = new SAMAutoMask();
                    this.mAutoMask.mImgEmbedding = this.mImgEmbedding;
                    this.mAutoMask.mSAM = this.mSam;
                    image.Dispose();
                    UI.Invoke(new Action(delegate
                    {
                        this.ShowStatus("Image Embedding ✔");
                    }));
                });
                thread.Start();

            }
        }

        private void BReLoad_Click(object sender, RoutedEventArgs e)
        {
            _isDragging = false;
            this.mImage.ReleaseMouseCapture(); // 确保状态重置
            this.Reset();
            this.LoadImgGrid.Visibility = Visibility.Visible;
            this.ImgCanvas.Visibility = Visibility.Hidden;
        }


        double CalculateCosineSimilarity(List<float> vector1, List<float> vector2)
        {
            double dotProduct = DotProduct(vector1, vector2);
            double magnitude1 = Magnitude(vector1);
            double magnitude2 = Magnitude(vector2);

            if (magnitude1 == 0 || magnitude2 == 0)
                return 0;

            return dotProduct / (magnitude1 * magnitude2);
        }

        double DotProduct(List<float> vector1, List<float> vector2)
        {
            return vector1.Zip(vector2, (a, b) => a * b).Sum();
        }

        static double Magnitude(List<float> vector)
        {
            return Math.Sqrt(vector.Select(x => x * x).Sum());
        }
        /// <summary>
        /// 撤销
        /// </summary>
        private void BUndo_Click(object sender, RoutedEventArgs e)
        {
            if (this.mUndoStack.Count > 0)
            {
                Promotion p = this.mUndoStack.Pop();
                this.mRedoStack.Push(p);
                this.RemoveAnation(p);
                this.mPromotionList.Clear();
                this.mPromotionList.AddRange(this.mUndoStack.ToArray());

                Thread thread = new Thread(() =>
                {
                    MaskData md = this.mSam.Decode(this.mPromotionList, this.mImgEmbedding, this.mOrgwid, this.mOrghei);
                    this.ShowMask(md.mMask.ToArray(), Color.FromArgb((byte)100, (byte)100, (byte)0, (byte)0));
                });
                thread.Start();
            }
            else
            {
                MessageBox.Show("No Undo Promot");
            }
        }
        /// <summary>
        /// 重做
        /// </summary>
        private void BRedo_Click(object sender, RoutedEventArgs e)
        {
            if (this.mRedoStack.Count > 0)
            {
                Promotion pt = this.mRedoStack.Pop();
                this.mUndoStack.Push(pt);
                this.AddAnation(pt);
                this.mPromotionList.Clear();
                this.mPromotionList.AddRange(this.mUndoStack.ToArray());
                Thread thread = new Thread(() =>
                {
                    MaskData md = this.mSam.Decode(this.mPromotionList, this.mImgEmbedding, this.mOrgwid, this.mOrghei);
                    this.ShowMask(md.mMask.ToArray(), Color.FromArgb((byte)100, (byte)200, (byte)0, (byte)0));
                });
                thread.Start();
            }
            else
            {
                MessageBox.Show("No Redo Promot");
            }
        }
        /// <summary>
        /// 复位
        /// </summary>
        private void BReset_Click(object sender, RoutedEventArgs e)
        {
            this.Reset();
        }
        /// <summary>
        /// 显示分割结果
        /// </summary>
        private float[] mMaskData; // 新增用于存储掩膜数据

        void ShowMask(float[] mask, Color color)
        {
            this.mMaskData = mask; // 存储掩膜数据以便后续使用
            UI.Invoke(new Action(delegate
            {
                WriteableBitmap bp = new WriteableBitmap(this.mOrgwid, this.mOrghei, 96, 96, PixelFormats.Pbgra32, null);
                byte[] pixelData = new byte[this.mOrgwid * this.mOrghei * 4];
                Array.Clear(pixelData, 0, pixelData.Length);

                _maskPixelCount = 0;

                for (int y = 0; y < this.mOrghei; y++)
                {
                    for (int x = 0; x < this.mOrgwid; x++)
                    {
                        int ind = y * this.mOrgwid + x;
                        if (mask[ind] > this.mSam.mask_threshold)
                        {
                            // 计算掩膜覆盖区域下的原图部分的像素数量
                            pixelData[4 * ind] = color.B;
                            pixelData[4 * ind + 1] = color.G;
                            pixelData[4 * ind + 2] = color.R;
                            pixelData[4 * ind + 3] = 100; // Alpha 通道值

                            _maskPixelCount++; // 每次掩膜覆盖时增加像素计数
                        }
                    }
                }

                bp.WritePixels(new Int32Rect(0, 0, this.mOrgwid, this.mOrghei), pixelData, this.mOrgwid * 4, 0);
                this.mMask.Source = bp;
            }));
        }


        /// <summary>
        /// 显示分割结果
        /// </summary>
        void ShowMask(MaskData mask)
        {
            UI.Invoke(new Action(delegate
            {
                this.ShowStatus("Finish");
                this.ClearAnation();

                WriteableBitmap bp = new WriteableBitmap(this.mOrgwid, this.mOrghei, 96, 96, PixelFormats.Pbgra32, null);
                byte[] pixelData = new byte[this.mOrgwid * this.mOrghei * 4];
                Array.Clear(pixelData, 0, pixelData.Length);

                _maskPixelCount = 0; // 初始化掩膜像素计数

                for (int i = 0; i < mask.mShape[1]; i++)
                {
                    Random random = new Random();
                    Color randomColor = Color.FromArgb((byte)100, (byte)random.Next(256), (byte)random.Next(256), (byte)random.Next(256));
                    for (int y = 0; y < this.mOrghei; y++)
                    {
                        for (int x = 0; x < this.mOrgwid; x++)
                        {
                            int indpixel = y * this.mOrgwid + x;
                            if (mask.mfinalMask[i][indpixel] > this.mSam.mask_threshold)
                            {
                                // 计算掩膜覆盖区域下的原图部分的像素数量
                                pixelData[4 * indpixel] = randomColor.B;
                                pixelData[4 * indpixel + 1] = randomColor.G;
                                pixelData[4 * indpixel + 2] = randomColor.R;
                                pixelData[4 * indpixel + 3] = 100; // Alpha 通道值

                                _maskPixelCount++; // 每次掩膜覆盖时增加像素计数
                            }
                        }
                    }

                    // 绘制掩膜框
                    Point leftup = this.Image2Window(new Point(mask.mBox[4 * i], mask.mBox[4 * i + 1]));
                    Point rightdown = this.Image2Window(new Point(mask.mBox[4 * i + 2], mask.mBox[4 * i + 3]));
                    RectAnnotation box = new RectAnnotation();
                    this.ImgCanvas.Children.Add(box);
                    box.Width = rightdown.X - leftup.X;
                    box.Height = rightdown.Y - leftup.Y;
                    Canvas.SetLeft(box, leftup.X);
                    Canvas.SetTop(box, leftup.Y);
                }

                bp.WritePixels(new Int32Rect(0, 0, this.mOrgwid, this.mOrghei), pixelData, this.mOrgwid * 4, 0);
                this.mMask.Source = bp;
            }));
        }


        /// <summary>
        /// 窗口坐标转图像坐标
        /// </summary>
        Point Window2Image(Point clickPoint)
        {
            double imageWidth = this.mImage.ActualWidth;
            double imageHeight = this.mImage.ActualHeight;
            double scaleX = imageWidth / this.mOrgwid;
            double scaleY = imageHeight / this.mOrghei;
            double offsetX = (imageWidth - scaleX * this.mOrgwid) / 2;
            double offsetY = (imageHeight - scaleY * this.mOrghei) / 2;
            double imageX = (clickPoint.X - offsetX) / scaleX;
            double imageY = (clickPoint.Y - offsetY) / scaleY;
            Point p = new Point();
            p.X = (int)imageX;
            p.Y = (int)imageY;

            return p;
        }
        Point Image2Window(Point image)
        {
            double imageWidth = this.mImage.ActualWidth;
            double imageHeight = this.mImage.ActualHeight;
            double scaleX = imageWidth / this.mOrgwid;
            double scaleY = imageHeight / this.mOrghei;
            double offsetX = (imageWidth - scaleX * this.mOrgwid) / 2;
            double offsetY = (imageHeight - scaleY * this.mOrghei) / 2;

            double windowsX = image.X * scaleX + offsetX;
            double windowsY = image.Y * scaleY + offsetX;

            Point p = new Point();
            p.X = (int)windowsX;
            p.Y = (int)windowsY;

            return p;
        }
        /// <summary>
        /// 清空
        /// </summary>
        void ClearAnation()
        {
            List<UserControl> todel = new List<UserControl>();
            foreach (var v in this.ImgCanvas.Children)
            {
                if (v is PointAnnotation || v is RectAnnotation)
                    todel.Add(v as UserControl);
            }

            todel.ForEach(e => { this.ImgCanvas.Children.Remove(e); });
        }
        /// <summary>
        /// 删除
        /// </summary>
        void RemoveAnation(Promotion pt)
        {
            if (this.ImgCanvas.Children.Contains(pt.mAnation))
                this.ImgCanvas.Children.Remove(pt.mAnation);
        }
        /// <summary>
        /// 添加
        /// </summary>
        void AddAnation(Promotion pt)
        {
            if (!this.ImgCanvas.Children.Contains(pt.mAnation))
                this.ImgCanvas.Children.Add(pt.mAnation);

        }
        /// <summary>
        /// 显示状态信息
        /// </summary>
        void ShowStatus(string message)
        {
            this.StatusTxt.Text = message;
        }
        void Reset()
        {
            this.ClearAnation();
            this.mPromotionList.Clear();
            this.mMask.Source = null;
        }
        private void Startseg_Click(object sender, RoutedEventArgs e)
        {
            this.currentMode = Mode.CreatingHints; // 切换到提示点模式
            this.mCurOp = Operation.Point;  // 设置为点操作
            this.mOpType = OpType.ADD;      // 设置为增加点
        }
        // 加点操作
        private void AddPoint_Click(object sender, RoutedEventArgs e)
        {
            this.mCurOp = Operation.Point;
            this.mOpType = OpType.ADD;
        }

        // 减点操作
        private void RemovePoint_Click(object sender, RoutedEventArgs e)
        {
            this.mCurOp = Operation.Point;
            this.mOpType = OpType.REMOVE;
        }

        // 画框操作
        private void DrawBox_Click(object sender, RoutedEventArgs e)
        {
            this.mCurOp = Operation.Box;
        }


        private void mAutoSeg_Click(object sender, RoutedEventArgs e)
        {
            this.mAutoMask.points_per_side = int.Parse(this.mPoints_per_side.Text);
            this.mAutoMask.pred_iou_thresh = float.Parse(this.mPred_iou_thresh.Text);
            this.mAutoMask.stability_score_thresh = float.Parse(this.mStability_score_thresh.Text);
            this.ShowStatus("Auto Segment......");
            Thread thread = new Thread(() =>
            {
                this.mCurOp = Operation.Everything;
                this.mAutoMaskData = this.mAutoMask.Generate(this.mImagePath);
                this.ShowMask(this.mAutoMaskData);
            });
            thread.Start();
        }
        MaskData MatchTextAndImage(string txt)
        {
            var txtEmbedding = this.mCLIP.TxtEncoder(txt);
            OpenCvSharp.Mat image = new OpenCvSharp.Mat(this.mImagePath, OpenCvSharp.ImreadModes.Color);
            int maxindex = 0;
            double max = 0.0;
            MaskData final = new MaskData();
            for (int i = 0; i < this.mAutoMaskData.mShape[1]; i++)
            {
                // Define the coordinates of the ROI
                int x = this.mAutoMaskData.mBox[4 * i];  // Top-left x coordinate
                int y = this.mAutoMaskData.mBox[4 * i + 1];// Top-left y coordinate
                int width = this.mAutoMaskData.mBox[4 * i + 2] - this.mAutoMaskData.mBox[4 * i];  // Width of the ROI
                int height = this.mAutoMaskData.mBox[4 * i + 3] - this.mAutoMaskData.mBox[4 * i + 1];  // Height of the ROI

                // Create a Rect object for the ROI
                OpenCvSharp.Rect roiRect = new OpenCvSharp.Rect(x, y, width, height);
                // Extract the ROI from the image
                OpenCvSharp.Mat roi = new OpenCvSharp.Mat(image, roiRect);
                int neww = 0;
                int newh = 0;
                float scale = 224 * 1.0f / Math.Max(image.Rows, image.Cols);
                float newht = image.Rows * scale;
                float newwt = image.Cols * scale;

                neww = (int)(newwt + 0.5);
                newh = (int)(newht + 0.5);

                OpenCvSharp.Mat resizedImage = new OpenCvSharp.Mat();
                OpenCvSharp.Cv2.Resize(roi, resizedImage, new OpenCvSharp.Size(neww, newh));
                // 创建大的Mat
                OpenCvSharp.Mat largeMat = new OpenCvSharp.Mat(new OpenCvSharp.Size(224, 224), OpenCvSharp.MatType.CV_8UC3, OpenCvSharp.Scalar.Black);

                // 计算小的Mat放置的位置
                int xoffset = (largeMat.Width - resizedImage.Width) / 2;
                int yoffset = (largeMat.Height - resizedImage.Height) / 2;

                // 将小的Mat放置到大的Mat的中心位置
                resizedImage.CopyTo(largeMat[new OpenCvSharp.Rect(xoffset, yoffset, resizedImage.Width, resizedImage.Height)]);

                //将图像转换为浮点型
                OpenCvSharp.Mat floatImage = new OpenCvSharp.Mat();
                largeMat.ConvertTo(floatImage, OpenCvSharp.MatType.CV_32FC3);
                // 计算均值和标准差
                OpenCvSharp.Scalar mean = new OpenCvSharp.Scalar(0.48145466, 0.4578275, 0.40821073);
                OpenCvSharp.Scalar std = new OpenCvSharp.Scalar(0.26862954, 0.26130258, 0.27577711);
                // 归一化
                OpenCvSharp.Cv2.Normalize(floatImage, floatImage, 0, 255, OpenCvSharp.NormTypes.MinMax);
                OpenCvSharp.Cv2.Subtract(floatImage, mean, floatImage);
                OpenCvSharp.Cv2.Divide(floatImage, std, floatImage);

                float[] transformedImg = new float[3 * 224 * 224];
                for (int ii = 0; ii < 224; ii++)
                {
                    for (int j = 0; j < 224; j++)
                    {
                        int index = j * 224 + ii;
                        transformedImg[index] = floatImage.At<OpenCvSharp.Vec3f>(j, ii)[0];
                        transformedImg[224 * 224 + index] = floatImage.At<OpenCvSharp.Vec3f>(j, ii)[1];
                        transformedImg[2 * 224 * 224 + index] = floatImage.At<OpenCvSharp.Vec3f>(j, ii)[2];
                    }
                }

                var imgEmbedding = this.mCLIP.ImgEncoder(transformedImg);
                double maxs = CalculateCosineSimilarity(txtEmbedding.ToList(), imgEmbedding.ToList());
                if (maxs > max)
                {
                    maxindex = i;
                    max = maxs;
                }

                roi.Dispose();
                resizedImage.Dispose();
                largeMat.Dispose();
                floatImage.Dispose();
            }

            this.mAutoMaskData.mShape.CopyTo(final.mShape, 0);
            final.mShape[1] = 1;
            final.mBox.AddRange(this.mAutoMaskData.mBox.GetRange(maxindex * 4, 4));
            final.mIoU.AddRange(this.mAutoMaskData.mIoU.GetRange(maxindex, 1));
            final.mStalibility.AddRange(this.mAutoMaskData.mStalibility.GetRange(maxindex, 1));
            //.GetRange(maxindex * final.mShape[2] * final.mShape[3], final.mShape[2] * final.mShape[3])
            final.mfinalMask.Add(this.mAutoMaskData.mfinalMask[maxindex]);



            image.Dispose();


            return final;
        }
        // 点击设定标尺的按钮事件

        // 设置标尺两端点
        private void SetRuler_Click(object sender, RoutedEventArgs e)
        {

            ResetRuler(); // 先重置标尺

            MessageBox.Show("请在图像上选择标尺的起点和终点。");

            // 重置标尺起点
            _rulerStartPoint = null;

            // 设置当前模式为 SettingRuler
            this.currentMode = Mode.SettingRuler;

            // 注册鼠标左键点击事件，选择起点和终点
            ImgCanvas.MouseLeftButtonDown += SetRulerPoints;
        }

        // 设置标尺两端点
        private void SetRulerPoints(object sender, MouseButtonEventArgs e)
        {
            if (this.currentMode != Mode.SettingRuler)
            {
                return;
            }

            // 获取显示坐标上的点击位置
            Point clickedPointInDisplay = e.GetPosition(this.ImgCanvas);

            // 将显示坐标转换为原始图像的坐标（用于计算）
            Point clickedPointInOriginal = DisplayToImageCoords(clickedPointInDisplay);

            // 如果没有起点，则设置起点并绘制
            if (_rulerStartPoint == null)
            {
                _rulerStartPoint = clickedPointInOriginal; // 存储原始坐标

                // 在Canvas上绘制起点（使用显示坐标）
                _startPointEllipse = new Ellipse
                {
                    Width = 5,
                    Height = 5,
                    Fill = Brushes.Blue
                };
                Canvas.SetLeft(_startPointEllipse, clickedPointInDisplay.X - 2.5);
                Canvas.SetTop(_startPointEllipse, clickedPointInDisplay.Y - 2.5);
                ImgCanvas.Children.Add(_startPointEllipse);

                MessageBox.Show("起点已设定，请选择标尺的终点。");
            }
            else
            {
                // 设置终点并绘制线段
                _endPointEllipse = new Ellipse
                {
                    Width = 5,
                    Height = 5,
                    Fill = Brushes.Red
                };
                Canvas.SetLeft(_endPointEllipse, clickedPointInDisplay.X - 2.5);
                Canvas.SetTop(_endPointEllipse, clickedPointInDisplay.Y - 2.5);
                ImgCanvas.Children.Add(_endPointEllipse);

                // 在Canvas上绘制线段（使用显示坐标）
                _rulerLine = new Line
                {
                    Stroke = Brushes.Green,
                    StrokeThickness = 2,
                    X1 = Canvas.GetLeft(_startPointEllipse) + 2.5,
                    Y1 = Canvas.GetTop(_startPointEllipse) + 2.5,
                    X2 = clickedPointInDisplay.X,
                    Y2 = clickedPointInDisplay.Y
                };
                ImgCanvas.Children.Add(_rulerLine);

                // 计算原始图像中的距离
                double pixelDistance = Math.Sqrt(
                    Math.Pow(clickedPointInOriginal.X - _rulerStartPoint.Value.X, 2) +
                    Math.Pow(clickedPointInOriginal.Y - _rulerStartPoint.Value.Y, 2)
                );

                // 提示用户输入实际长度和单位
                string input = Microsoft.VisualBasic.Interaction.InputBox(
                    "请输入标尺的实际长度（例如10.0）：", "设定标尺长度", "1.0");

                if (double.TryParse(input, out double actualLength))
                {
                    _pixelToRealRatio = actualLength / pixelDistance;

                    string unit = Microsoft.VisualBasic.Interaction.InputBox(
                        "请输入单位（例如cm、mm、m等）：", "设定单位", "cm");

                    _unit = unit;
                    MessageBox.Show($"标尺设置成功：1 像素 = {_pixelToRealRatio} {unit}");

                    // 在中点显示标尺的长度
                    TextBlock rulerText = new TextBlock
                    {
                        Text = $"{actualLength} {unit}",
                        Foreground = Brushes.Black,
                        Background = Brushes.White,
                        FontWeight = FontWeights.Bold
                    };

                    Canvas.SetLeft(rulerText, (_rulerLine.X1 + _rulerLine.X2) / 2);
                    Canvas.SetTop(rulerText, (_rulerLine.Y1 + _rulerLine.Y2) / 2);
                    ImgCanvas.Children.Add(rulerText);

                    // 完成后解绑鼠标事件
                    ImgCanvas.MouseLeftButtonDown -= SetRulerPoints;
                }
                else
                {
                    MessageBox.Show("无效输入，请重试。");
                    ResetRuler();
                }
            }
        }



        // 重置标尺设置
        private void ResetRuler()
        {
            _rulerStartPoint = null;

            // 移除线和端点
            if (_rulerLine != null) ImgCanvas.Children.Remove(_rulerLine);
            if (_startPointEllipse != null) ImgCanvas.Children.Remove(_startPointEllipse);
            if (_endPointEllipse != null) ImgCanvas.Children.Remove(_endPointEllipse);
        }
        //private void BCarea_Click(object sender, RoutedEventArgs e)
        //{

        //    // 更新 TextBlock 来显示掩膜的面积
        //    string areaText = $"Mask Area: {_maskPixelCount} pixels"; // 定义 areaText 变量
        // // 创建并显示结果窗口

        //    autoResultWindow resultWindow = new autoResultWindow(areaText);
        //    // 获取鼠标位置
        //    Point mousePosition = Mouse.GetPosition(this);
        //    // 设置新窗口的位置
        //    resultWindow.Left = this.Left + mousePosition.X;
        //    resultWindow.Top = this.Top + mousePosition.Y;
        //    resultWindow.ShowDialog();
        //}

        private void BCarea_Click(object sender, RoutedEventArgs e)
        {
            if (_pixelToRealRatio <= 0)
            {
                MessageBox.Show("请先设定标尺！");
                return;
            }

            // 计算掩码下原图区域的实际面积
            double maskAreaInRealUnits = _maskPixelCount * Math.Pow(_pixelToRealRatio, 2);

            // 更新 TextBlock 来显示掩码的实际面积
            string pixelAreaTextBlock = $"Mask Area: {_maskPixelCount} pixels";
            string realAreaTextBlock = $"Real Area: {maskAreaInRealUnits:F2} {_unit}²";

            // 创建并显示结果窗口
            autoResultWindow resultWindow = new autoResultWindow(pixelAreaTextBlock, realAreaTextBlock);

            // 订阅保存事件
            resultWindow.SaveArea += (pixelArea, realArea) =>
            {
                // 解析传递的像素面积和实际面积为 double
                if (double.TryParse(pixelArea, out double pixelAreaValue) && double.TryParse(realArea, out double realAreaValue))
                {
                    DataCollection.Add(new DataModel
                    {
                        ID = _currentId.ToString(),
                        Area = realAreaValue,  // 将实际面积存储为 double
                        Pixels = pixelAreaValue,  // 将像素面积存储为 double
                        Length = 0,  // 长度默认为 0
                    });

                    _currentId++; // 增加 ID
                }

            };

            // 显示窗口
            resultWindow.Show();

            // 处理删除事件（此处不作处理）

            // 获取鼠标位置
            Point mousePosition = Mouse.GetPosition(this);

            // 设置新窗口的位置
            resultWindow.Left = this.Left + mousePosition.X;
            resultWindow.Top = this.Top + mousePosition.Y;

            resultWindow.Show();
        }
        private int CalculateMaskPerimeter(float[] mask)
        {
            int perimeter = 0;

            for (int y = 0; y < this.mOrghei; y++)
            {
                for (int x = 0; x < this.mOrgwid; x++)
                {
                    int index = y * this.mOrgwid + x;
                    if (mask[index] > this.mSam.mask_threshold)
                    {
                        if (IsEdgePixel(mask, x, y))
                        {
                            perimeter++;
                        }
                    }
                }
            }

            return perimeter;
        }

        // 检查像素是否位于边缘的辅助方法
        private bool IsEdgePixel(float[] mask, int x, int y)
        {
            // 检查相邻像素来检测边缘（处理边界情况）
            int width = this.mOrgwid;
            int height = this.mOrghei;

            bool isEdge =
                (x > 0 && mask[y * width + (x - 1)] <= this.mSam.mask_threshold) ||  // Left
                (x < width - 1 && mask[y * width + (x + 1)] <= this.mSam.mask_threshold) ||  // Right
                (y > 0 && mask[(y - 1) * width + x] <= this.mSam.mask_threshold) ||  // Top
                (y < height - 1 && mask[(y + 1) * width + x] <= this.mSam.mask_threshold);  // Bottom

            return isEdge;
        }

        private void Bperimeter_Click(object sender, RoutedEventArgs e)
        {
            if (_pixelToRealRatio <= 0)
            {
                MessageBox.Show("请先设定标尺！");
                return;
            }

            // 确保 mask 数据存在
            if (this.mMask == null)
            {
                MessageBox.Show("未找到掩膜数据！");
                return;
            }

            // 计算掩膜下区域的像素周长
            double maskPerimeterInPixels = CalculateMaskPerimeter(this.mMaskData);

            // 根据比例转换为实际单位
            double perimeterInRealUnits = maskPerimeterInPixels * _pixelToRealRatio;

            // 更新 TextBlock 来显示周长信息
            string pixelPerimeterTextBlock = $"Perimeter: {maskPerimeterInPixels} pixels";
            string realPerimeterTextBlock = $"Real Perimeter: {perimeterInRealUnits:F2} {_unit}";

            // 创建并显示结果窗口
            autoResultWindow resultWindow = new autoResultWindow(pixelPerimeterTextBlock, realPerimeterTextBlock);

            // 订阅保存事件
            resultWindow.SaveArea += (pixelPerimeter, realPerimeter) =>
            {
                if (double.TryParse(pixelPerimeter, out double pixelPerimeterValue) &&
                    double.TryParse(realPerimeter, out double realPerimeterValue))
                {
                    DataCollection.Add(new DataModel
                    {
                        ID = _currentId.ToString(),
                        Area = 0,  // 面积默认为 0
                        Pixels = 0,  // 像素面积默认为 0
                        Length = realPerimeterValue,  // 存储实际周长
                    });

                    _currentId++; // 增加 ID
                }
            };

            // 获取鼠标位置并设置弹出窗口的位置
            Point mousePosition = Mouse.GetPosition(this);
            resultWindow.Left = this.Left + mousePosition.X;
            resultWindow.Top = this.Top + mousePosition.Y;

            // 显示窗口
            resultWindow.Show();
        }

        // 计算最小外接圆的中心和半径



        private (Point center, double radius) CalculateEnclosingCircle(float[] mask)
        {
            List<Point> edgePoints = new List<Point>();

            // 获取所有边缘点
            for (int y = 0; y < mOrghei; y++)
            {
                for (int x = 0; x < mOrgwid; x++)
                {
                    int index = y * mOrgwid + x;
                    if (mask[index] > 0.5 && IsEdgePixel(mask, x, y)) // 使用阈值检查
                    {
                        edgePoints.Add(new Point(x, y));
                    }
                }
            }

            // 计算最小外接圆
            var center = new Point(mOrgwid / 2, mOrghei / 2);
            double radius = 0;

            if (edgePoints.Count > 0)
            {
                // 简单计算中心，假设为边缘点的平均值（近似）
                center = new Point(edgePoints.Average(p => p.X), edgePoints.Average(p => p.Y));

                // 计算最远的边缘点距离，作为半径
                radius = edgePoints.Max(p => Distance(center, p));
            }

            return (center, radius);
        }

        // 计算最大内部距离的两个端点
        private (Point point1, Point point2) CalculateMaxInternalDistancePoints(float[] mask)
        {
            List<Point> edgePoints = new List<Point>();

            // 获取所有边缘点
            for (int y = 0; y < mOrghei; y++)
            {
                for (int x = 0; x < mOrgwid; x++)
                {
                    int index = y * mOrgwid + x;
                    if (mask[index] > 0.5 && IsEdgePixel(mask, x, y)) // 使用阈值检查
                    {
                        edgePoints.Add(new Point(x, y));
                    }
                }
            }

            double maxDistance = 0;
            Point maxPoint1 = new Point();
            Point maxPoint2 = new Point();

            // 计算边缘点之间的最大距离
            for (int i = 0; i < edgePoints.Count; i++)
            {
                for (int j = i + 1; j < edgePoints.Count; j++)
                {
                    double distance = Distance(edgePoints[i], edgePoints[j]);
                    if (distance > maxDistance)
                    {
                        maxDistance = distance;
                        maxPoint1 = edgePoints[i];
                        maxPoint2 = edgePoints[j];
                    }
                }
            }

            return (maxPoint1, maxPoint2);
        }
        /// <summary>
        /// 计算直径咯------------------------------------------------
        /// </summary>
        /// <param name="canvas"></param>
        /// <param name="mask"></param>
        /// 
        // 用于存储绘制的线条和文本
        private List<UIElement> _drawnElements = new List<UIElement>();

        // 清除之前绘制的元素
        private void ClearDrawnElements(Canvas canvas)
        {
            foreach (var element in _drawnElements)
            {
                canvas.Children.Remove(element);
            }
            _drawnElements.Clear(); // 清空列表
        }

        // 绘制线段到 Canvas
        private void DrawLinesOnCanvas(Canvas canvas, float[] mask)
        {
            // 获取当前图像的缩放比例
            double scaleX = this.ImgCanvas.ActualWidth / this.mOrgwid;
            double scaleY = this.ImgCanvas.ActualHeight / this.mOrghei;
            double scale = Math.Min(scaleX, scaleY);

            //// 计算最小外接圆
            //var (center, radius) = CalculateEnclosingCircle(mask);
            //Point point1Circle = new Point((center.X - radius) * scale, center.Y * scale); // 左边的直径端点
            //Point point2Circle = new Point((center.X + radius) * scale, center.Y * scale); // 右边的直径端点

            // 计算最小外接圆
            var (center, radius) = CalculateEnclosingCircle(mask);
            Point point1Circle = new Point((center.X - radius) * scale, center.Y * scale); // 左边的直径端点
            Point point2Circle = new Point((center.X + radius) * scale, center.Y * scale); // 右边的直径端点
            // 计算最大内部距离的两个端点
            var (maxPoint1, maxPoint2) = CalculateMaxInternalDistancePoints(mask);
            Point scaledMaxPoint1 = new Point(maxPoint1.X * scale, maxPoint1.Y * scale);
            Point scaledMaxPoint2 = new Point(maxPoint2.X * scale, maxPoint2.Y * scale);

            // 计算真实长度
            double diameterInRealUnits = 2 * radius * _pixelToRealRatio; // 最小外接圆直径的真实长度
            double maxInternalDistanceInRealUnits = Distance(maxPoint1, maxPoint2) * _pixelToRealRatio; // 最大内部距离的真实长度

            // 在 Canvas 上绘制最小外接圆直径 (红色)
            DrawLineOnCanvas(canvas, point1Circle, point2Circle, Brushes.Red, diameterInRealUnits);

            // 在 Canvas 上绘制最大内部距离 (蓝色)
            DrawLineOnCanvas(canvas, scaledMaxPoint1, scaledMaxPoint2, Brushes.Blue, maxInternalDistanceInRealUnits);
        }
        // 在 Canvas 上绘制一条线段
        private void DrawLineOnCanvas(Canvas canvas, Point point1, Point point2, Brush brush, double lengthInRealUnits)
        {
            Line line = new Line
            {
                X1 = point1.X,
                Y1 = point1.Y,
                X2 = point2.X,
                Y2 = point2.Y,
                Stroke = brush,
                StrokeThickness = 2,
                SnapsToDevicePixels = true
            };
            canvas.Children.Add(line);
            _drawnElements.Add(line); // 将线条添加到列表中

            // 在第一个端点位置创建显示长度的 TextBlock
            TextBlock lengthText1 = new TextBlock
            {
                Text = $"{lengthInRealUnits:F2} {_unit}", // 仅显示真实长度
                Foreground = brush,
                FontSize = 12,
                Background = new SolidColorBrush(Color.FromArgb(60, 240, 248, 255)) // 透明度的 AliceBlue
            };

            // 在第一个端点位置创建显示长度的 TextBlock
            TextBlock lengthText = new TextBlock
            {
                Text = $"{lengthInRealUnits:F2} {_unit}", // 仅显示真实长度
                Foreground = brush,
                FontSize = 12,
                Background = new SolidColorBrush(Color.FromArgb(60, 240, 248, 255)) //
            };

            // 设置 TextBlock 的位置（在第一个端点）
            Canvas.SetLeft(lengthText, point1.X);
            Canvas.SetTop(lengthText, point1.Y);


            // 添加 TextBlock 到 Canvas
            canvas.Children.Add(lengthText);
            _drawnElements.Add(lengthText); // 将 TextBlock 添加到列表
        }

        // 计算两个点之间的距离
        private double Distance(Point p1, Point p2)
        {
            return Math.Sqrt(Math.Pow(p1.X - p2.X, 2) + Math.Pow(p1.Y - p2.Y, 2));
        }

        // 检查像素是否位于边缘
        private bool IsEdgePixel2(float[] mask, int x, int y)
        {
            int width = this.mOrgwid;
            int height = this.mOrghei;

            bool isEdge =
                (x > 0 && mask[y * width + (x - 1)] <= 0.5) ||  // 左
                (x < width - 1 && mask[y * width + (x + 1)] <= 0.5) ||  // 右
                (y > 0 && mask[(y - 1) * width + x] <= 0.5) ||  // 上
                (y < height - 1 && mask[(y + 1) * width + x] <= 0.5);  // 下

            return isEdge;
        }

        // 按钮点击事件，显示直径和最大内部距离
        private void Bdiameter_Click(object sender, RoutedEventArgs e)
        {
            if (_pixelToRealRatio <= 0)
            {
                MessageBox.Show("请先设定标尺！");
                return;
            }

            // 确保掩膜数据存在
            if (this.mMaskData == null)
            {
                MessageBox.Show("未找到掩膜数据！");
                return;
            }
          
            // 获取 Canvas 控件
            var canvas = this.ImgCanvas;
            // 清除之前的绘制元素
            ClearDrawnElements(canvas);
            // 在 Canvas 上绘制最小外接圆和最大内部距离线段
            DrawLinesOnCanvas(canvas, this.mMaskData);

            // 计算直径和最大内部距离
            var (center, radius) = CalculateEnclosingCircle(this.mMaskData);
            double maskDiameterInPixels = radius * 2;
            double diameterInRealUnits = maskDiameterInPixels * _pixelToRealRatio;

            var (maxPoint1, maxPoint2) = CalculateMaxInternalDistancePoints(this.mMaskData);
            double maxInternalDistanceInPixels = Distance(maxPoint1, maxPoint2);
            double maxInternalDistanceInRealUnits = maxInternalDistanceInPixels * _pixelToRealRatio;
          
            // 显示结果窗口
            /*
            string pixelDiameterTextBlock = $"Enclosing Circle Diameter: {maskDiameterInPixels:F2} pixels";
            string realDiameterTextBlock = $"Real Enclosing Circle Diameter: {diameterInRealUnits:F2} {_unit}";

            string pixelMaxDistanceTextBlock = $"Max Internal Distance: {maxInternalDistanceInPixels:F2} pixels";
            string realMaxDistanceTextBlock = $"Real Max Internal Distance: {maxInternalDistanceInRealUnits:F2} {_unit}";

            // 创建窗口
            autoResultWindow resultWindow = new autoResultWindow(
                pixelDiameterTextBlock + "\n" + realDiameterTextBlock,
                pixelMaxDistanceTextBlock + "\n" + realMaxDistanceTextBlock
            );
            resultWindow.Show();

            */

        }

    }
}

        //private void mText_Click(object sender, RoutedEventArgs e)
        //{
        //    this.mCurOp = Operation.Text;
        //    this.ShowStatus("Image And Text Matching......");
        //    string txt = this.mTextinput.Text;
        //    Thread thread = new Thread(() =>
        //    {
        //        MaskData matches = this.MatchTextAndImage(txt);
        //        this.ShowMask(matches);
        //    });
        //    thread.Start();
        //}

        //private void Expanded(object sender, RoutedEventArgs e)
        //{
        //    if (this.mPointexp == null || this.mBoxexp == null || this.mEverythingExp == null || this.mTextExp == null)
        //        return;

        //    Expander exp = sender as Expander;
        //    if (exp.IsExpanded == true)
        //    {
        //        this.mPointexp.IsExpanded = this.mPointexp == exp;
        //        this.mBoxexp.IsExpanded = this.mBoxexp == exp;
        //        this.mEverythingExp.IsExpanded = this.mEverythingExp == exp;
        //        this.mTextExp.IsExpanded = this.mTextExp == exp;
        //    }

        //    //}
        //}
        //private void Expanded(object sender, RoutedEventArgs e)
        //{
        //    if ( this.mEverythingExp == null || this.mTextExp == null)
        //        return;

        //    Expander exp = sender as Expander;
        //    if (exp.IsExpanded == true)
        //    {

        //        this.mEverythingExp.IsExpanded = this.mEverythingExp == exp;
        //        this.mTextExp.IsExpanded = this.mTextExp == exp;
        //    }


        //}

        enum Operation
        {
            None,   // 无效状态
            Point,
            Box,
            Everything,
            Text
        }
    
    