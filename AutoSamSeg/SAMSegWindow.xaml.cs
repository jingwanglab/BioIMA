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
using YourNamespace.Models;
using YourNamespace.ViewModels;
using System.Globalization;
using System.Windows.Data;
using Newtonsoft.Json;

namespace wpf522
{



    public partial class SAMSegWindow : Window
    {

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
        private double _pixelToRealRatio = 1.0; 
        public event Action<double> SaveArea;
        public ObservableCollection<LabelItem> Labels { get; set; } = new ObservableCollection<LabelItem>();
        public LabelItem SelectedLabel { get; set; }

        public ObservableCollection<DataModel> DataCollection { get; set; } = new ObservableCollection<DataModel>();

        private int _currentId = 1; 

        private string _unit = "cm"; 
        private string _currentFileName;

        public LabelViewModel LabelVM { get; set; }

        

        public class DataModel
        {
            public string ID { get; set; }
            public double Length { get; set; }
            public double Area { get; set; }
            public double Pixels { get; set; }
        }

        public enum Mode
        {
            None,
            SettingRuler,
            CreatingHints
        }
        private Mode currentMode = Mode.None;



























































        public SAMSegWindow()
        {
            InitializeComponent();
            this.MouseUp += SAMSegWindow_MouseUp;

            this.mImage.Stretch = Stretch.Uniform;
            this.mMask.Stretch = Stretch.Uniform;

            this.UI = Dispatcher.CurrentDispatcher;
            this.mCurOp = Operation.None;
            this.currentMode = Mode.None;





            LabelVM = new LabelViewModel();
            this.DataContext = LabelVM;
        }

        private Point _lastMousePosition;
        private bool _isDragging = false;
        private bool isLabelingMode = false; 

        private void SAMSegWindow_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (_isDragging)
            {
                _isDragging = false;
                this.mImage.ReleaseMouseCapture(); 
            }
        }








































        void LoadImage(string imgpath)
        {

            if (this.ImgCanvas.ActualWidth == 0 || this.ImgCanvas.ActualHeight == 0)
            {
                this.ImgCanvas.UpdateLayout(); 
            }
            BitmapImage bitmap = new BitmapImage(new Uri(imgpath));
            this.mOrgwid = (int)bitmap.Width;
            this.mOrghei = (int)bitmap.Height;

            ResetTransform(this.mImage);
            ResetTransform(this.mMask);

            double scaleX = this.ImgCanvas.ActualWidth / bitmap.Width;
            double scaleY = this.ImgCanvas.ActualHeight / bitmap.Height;
            double scale = Math.Min(scaleX, scaleY); 

            SetScaleTransform(this.mImage, scale);
            SetScaleTransform(this.mMask, scale);

            this.mImage.Source = bitmap;  


        }

        private void ResetTransform(Image imageControl)
        {
            var transformGroup = new TransformGroup();
            transformGroup.Children.Add(new ScaleTransform()); 
            transformGroup.Children.Add(new TranslateTransform()); 
            imageControl.RenderTransform = transformGroup;
        }

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

        private Point DisplayToImageCoords(Point displayPoint)
        {
            if (this.mImage.RenderTransform is TransformGroup transformGroup &&
                transformGroup.Children[0] is ScaleTransform scaleTransform)
            {
                double scale = scaleTransform.ScaleX; 

                double xInOriginal = displayPoint.X / scale;
                double yInOriginal = displayPoint.Y / scale;

                return new Point(xInOriginal, yInOriginal);
            }

            return displayPoint; 
        }

        private void SetOperationType(SAMSegWindow.SamOpType type)
        {
            SolidColorBrush brush = type == SamOpType.ADD ? Brushes.Red : Brushes.Black;

        }

        public enum SamOpType
        {
            None,   
            ADD,
            REMOVE
        }

        public OpType mOpType { get; set; }

        private void image_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {

            if (this.currentMode == Mode.None) return;

            if (!this.ImgCanvas.IsMouseOver)
            {

                return;
            }

            if (this.currentMode == Mode.SettingRuler)
            {

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

        private void image_MouseMove(object sender, MouseEventArgs e)
        {

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



        private async void SelectFileButton_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog openFileDialog = new Microsoft.Win32.OpenFileDialog();
            openFileDialog.DefaultExt = ".png";
            openFileDialog.Filter = "Image files (*.jpg, *.jpeg, *.png)|*.jpg;*.jpeg;*.png|All files (*.*)|*.*";

            bool? result = openFileDialog.ShowDialog();
            if (result == true)
            {
                this.ImgPathTxt.Text = openFileDialog.FileName;
                this.mImagePath = this.ImgPathTxt.Text;

                if (!File.Exists(this.mImagePath))
                    return;

                _currentFileName = System.IO.Path.GetFileName(this.mImagePath);
                this.LoadImgGrid.Visibility = Visibility.Collapsed;
                this.ImgCanvas.Visibility = Visibility.Visible;
                this.LoadImage(this.mImagePath);

                this.StatusTxt.Visibility = Visibility.Visible;
                this.ProgressBarStatus.Visibility = Visibility.Visible;

                ShowStatus("Image Loaded", 10); 

                ClearDrawnElements(this.ImgCanvas);

                await Task.Run(() =>
                {

                    Dispatcher.BeginInvoke(new Action(() => ShowStatus("Loading ONNX Model...", 30)));
                    this.mSam.LoadONNXModel();

                    Dispatcher.BeginInvoke(new Action(() => ShowStatus("ONNX Model Loaded ?", 50))); 

                    Dispatcher.BeginInvoke(new Action(() => ShowStatus("Processing Image...", 70))); 
                    OpenCvSharp.Mat image = OpenCvSharp.Cv2.ImRead(this.mImagePath, OpenCvSharp.ImreadModes.Color);

                    Dispatcher.BeginInvoke(new Action(() => ShowStatus("Encoding Image...", 80))); 
                    this.mImgEmbedding = this.mSam.Encode(image, this.mOrgwid, this.mOrghei);

                    this.mAutoMask = new SAMAutoMask();
                    this.mAutoMask.mImgEmbedding = this.mImgEmbedding;
                    this.mAutoMask.mSAM = this.mSam;
                    image.Dispose();

                    Dispatcher.BeginInvoke(new Action(() => ShowStatus("Image Embedding ?", 100))); 
                });

                await Task.Delay(100);
                this.ProgressBarStatus.Visibility = Visibility.Collapsed;
                this.StatusTxt.Visibility = Visibility.Collapsed; 
            
        }
        }



        private void BReLoad_Click(object sender, RoutedEventArgs e)
        {
            _isDragging = false;
            this.mImage.ReleaseMouseCapture(); 
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



        private void BReset_Click(object sender, RoutedEventArgs e)
        {
            this.Reset();

            this.currentMode = Mode.CreatingHints;
            this.mCurOp = Operation.Point; 
            this.mOpType = OpType.ADD; 
        }



        private float[] mMaskData; 

        void ShowMask(float[] mask, Color color)
        {
            this.mMaskData = mask; 
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

                            pixelData[4 * ind] = color.B;
                            pixelData[4 * ind + 1] = color.G;
                            pixelData[4 * ind + 2] = color.R;
                            pixelData[4 * ind + 3] = 100; 

                            _maskPixelCount++; 
                        }
                    }
                }

                bp.WritePixels(new Int32Rect(0, 0, this.mOrgwid, this.mOrghei), pixelData, this.mOrgwid * 4, 0);
                this.mMask.Source = bp;
            }));
        }



        void ShowMask(MaskData mask)
        {
            UI.Invoke(new Action(delegate
            {
                this.ShowStatus("Finish");
                this.ClearAnation();

                WriteableBitmap bp = new WriteableBitmap(this.mOrgwid, this.mOrghei, 96, 96, PixelFormats.Pbgra32, null);
                byte[] pixelData = new byte[this.mOrgwid * this.mOrghei * 4];
                Array.Clear(pixelData, 0, pixelData.Length);

                _maskPixelCount = 0; 

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

                                pixelData[4 * indpixel] = randomColor.B;
                                pixelData[4 * indpixel + 1] = randomColor.G;
                                pixelData[4 * indpixel + 2] = randomColor.R;
                                pixelData[4 * indpixel + 3] = 100; 

                                _maskPixelCount++; 
                            }
                        }
                    }

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



        void RemoveAnation(Promotion pt)
        {
            if (this.ImgCanvas.Children.Contains(pt.mAnation))
                this.ImgCanvas.Children.Remove(pt.mAnation);
        }



        void AddAnation(Promotion pt)
        {
            if (!this.ImgCanvas.Children.Contains(pt.mAnation))
                this.ImgCanvas.Children.Add(pt.mAnation);

        }



        void ShowStatus(string message, int progress = -1)
        {
            Dispatcher.Invoke(() =>
            {
                this.StatusTxt.Text = message; 
                if (progress >= 0) 
                {
                    this.ProgressBarStatus.Visibility = Visibility.Visible;
                    this.ProgressBarStatus.Value = progress;
                }
            });
        }


        void Reset()
        {
            this.ClearAnation();
            this.mPromotionList.Clear();
            this.mMask.Source = null;
        }






        private void Startseg_Click(object sender, RoutedEventArgs e)
        {






            this.currentMode = Mode.CreatingHints;
            this.mCurOp = Operation.Point;
            this.mOpType = OpType.ADD;

            isLabelingMode = false;
            StartsegButton.IsEnabled = true;
            MessageBox.Show("Try clicking the image to perform segmentation!");
        }

        private void AddPoint_Click(object sender, RoutedEventArgs e)
        {
            this.mCurOp = Operation.Point;
            this.mOpType = OpType.ADD;
        }

        private void RemovePoint_Click(object sender, RoutedEventArgs e)
        {
            this.mCurOp = Operation.Point;
            this.mOpType = OpType.REMOVE;
        }

        private void DrawBox_Click(object sender, RoutedEventArgs e)
        {
            this.mCurOp = Operation.Box;
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

                int x = this.mAutoMaskData.mBox[4 * i];  
                int y = this.mAutoMaskData.mBox[4 * i + 1];
                int width = this.mAutoMaskData.mBox[4 * i + 2] - this.mAutoMaskData.mBox[4 * i];  
                int height = this.mAutoMaskData.mBox[4 * i + 3] - this.mAutoMaskData.mBox[4 * i + 1];  

                OpenCvSharp.Rect roiRect = new OpenCvSharp.Rect(x, y, width, height);

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

                OpenCvSharp.Mat largeMat = new OpenCvSharp.Mat(new OpenCvSharp.Size(224, 224), OpenCvSharp.MatType.CV_8UC3, OpenCvSharp.Scalar.Black);

                int xoffset = (largeMat.Width - resizedImage.Width) / 2;
                int yoffset = (largeMat.Height - resizedImage.Height) / 2;

                resizedImage.CopyTo(largeMat[new OpenCvSharp.Rect(xoffset, yoffset, resizedImage.Width, resizedImage.Height)]);

                OpenCvSharp.Mat floatImage = new OpenCvSharp.Mat();
                largeMat.ConvertTo(floatImage, OpenCvSharp.MatType.CV_32FC3);

                OpenCvSharp.Scalar mean = new OpenCvSharp.Scalar(0.48145466, 0.4578275, 0.40821073);
                OpenCvSharp.Scalar std = new OpenCvSharp.Scalar(0.26862954, 0.26130258, 0.27577711);

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

            final.mfinalMask.Add(this.mAutoMaskData.mfinalMask[maxindex]);



            image.Dispose();


            return final;
        }


        private void SetRuler_Click(object sender, RoutedEventArgs e)
        {

            ResetRuler(); 

            MessageBox.Show("Please select the start and end points of the ruler on the image.");

            _rulerStartPoint = null;

            this.currentMode = Mode.SettingRuler;

            ImgCanvas.MouseLeftButtonDown += SetRulerPoints;
        }

        private void SetRulerPoints(object sender, MouseButtonEventArgs e)
        {
            if (this.currentMode != Mode.SettingRuler)
            {
                return;
            }

            Point clickedPointInDisplay = e.GetPosition(this.ImgCanvas);

            Point clickedPointInOriginal = DisplayToImageCoords(clickedPointInDisplay);

            if (_rulerStartPoint == null)
            {
                _rulerStartPoint = clickedPointInOriginal; 

                _startPointEllipse = new Ellipse
                {
                    Width = 5,
                    Height = 5,
                    Fill = Brushes.Blue
                };
                Canvas.SetLeft(_startPointEllipse, clickedPointInDisplay.X - 2.5);
                Canvas.SetTop(_startPointEllipse, clickedPointInDisplay.Y - 2.5);
                ImgCanvas.Children.Add(_startPointEllipse);

                MessageBox.Show("Start point set. Please select the end point of the ruler.");
            }
            else
            {

                _endPointEllipse = new Ellipse
                {
                    Width = 5,
                    Height = 5,
                    Fill = Brushes.Red
                };
                Canvas.SetLeft(_endPointEllipse, clickedPointInDisplay.X - 2.5);
                Canvas.SetTop(_endPointEllipse, clickedPointInDisplay.Y - 2.5);
                ImgCanvas.Children.Add(_endPointEllipse);

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

                double pixelDistance = Math.Sqrt(
                    Math.Pow(clickedPointInOriginal.X - _rulerStartPoint.Value.X, 2) +
                    Math.Pow(clickedPointInOriginal.Y - _rulerStartPoint.Value.Y, 2)
                );

                string input = Microsoft.VisualBasic.Interaction.InputBox(
                      "Please enter the actual length of the ruler (e.g., 10.0):", "Set Ruler Length", "1.0");

                if (double.TryParse(input, out double actualLength))
                {
                    _pixelToRealRatio = actualLength / pixelDistance;

                    string unit = Microsoft.VisualBasic.Interaction.InputBox(
                       "Please enter the unit (e.g., cm, mm, m):", "Set Unit", "cm");

                    _unit = unit;
                    MessageBox.Show($"Ruler set successfully: 1 pixel = {_pixelToRealRatio} {unit}");

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

                    ImgCanvas.MouseLeftButtonDown -= SetRulerPoints;
                }
                else
                {
                    MessageBox.Show("Invalid input, please try again.");
                    ResetRuler();
                }
            }
        }

        private void ResetRuler()
        {
            _rulerStartPoint = null;

            if (_rulerLine != null) ImgCanvas.Children.Remove(_rulerLine);
            if (_startPointEllipse != null) ImgCanvas.Children.Remove(_startPointEllipse);
            if (_endPointEllipse != null) ImgCanvas.Children.Remove(_endPointEllipse);
        }
       

        private void BCarea_Click(object sender, RoutedEventArgs e)
        {
            if (_pixelToRealRatio <= 0)
            {
                MessageBox.Show("Please set the scale first!");
                return;
            }

            double maskAreaInRealUnits = _maskPixelCount * Math.Pow(_pixelToRealRatio, 2);

            string pixelAreaTextBlock = $"Mask Area: {_maskPixelCount} pixels";
            string realAreaTextBlock = $"Real Area: {maskAreaInRealUnits:F2} {_unit}2";

            autoResultWindow resultWindow = new autoResultWindow(pixelAreaTextBlock, realAreaTextBlock);

            resultWindow.SaveArea += (pixelArea, realArea) =>
            {

                if (double.TryParse(pixelArea, out double pixelAreaValue) && double.TryParse(realArea, out double realAreaValue))
                {
                    DataCollection.Add(new DataModel
                    {
                        ID = _currentId.ToString(),
                        Area = realAreaValue,  
                        Pixels = pixelAreaValue,  
                        Length = 0,  
                    });

                    _currentId++; 
                }

            };

            resultWindow.Show();


            Point mousePosition = Mouse.GetPosition(this);

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

        private bool IsEdgePixel(float[] mask, int x, int y)
        {

            int width = this.mOrgwid;
            int height = this.mOrghei;

            bool isEdge =
                (x > 0 && mask[y * width + (x - 1)] <= this.mSam.mask_threshold) ||  
                (x < width - 1 && mask[y * width + (x + 1)] <= this.mSam.mask_threshold) ||  
                (y > 0 && mask[(y - 1) * width + x] <= this.mSam.mask_threshold) ||  
                (y < height - 1 && mask[(y + 1) * width + x] <= this.mSam.mask_threshold);  

            return isEdge;
        }

        private void Bperimeter_Click(object sender, RoutedEventArgs e)
        {

            this.currentMode = Mode.None;

            if (_pixelToRealRatio <= 0)
            {
                MessageBox.Show("Please set the scale first!");
                return;
            }

            if (this.mMask == null)
            {
                MessageBox.Show("Mask data not found!");
                return;
            }

            double maskPerimeterInPixels = CalculateMaskPerimeter(this.mMaskData);

            double perimeterInRealUnits = maskPerimeterInPixels * _pixelToRealRatio;

            string pixelPerimeterTextBlock = $"Perimeter: {maskPerimeterInPixels} pixels";
            string realPerimeterTextBlock = $"Real Perimeter: {perimeterInRealUnits:F2} {_unit}";

            autoResultWindow resultWindow = new autoResultWindow(pixelPerimeterTextBlock, realPerimeterTextBlock);

            resultWindow.SaveArea += (pixelPerimeter, realPerimeter) =>
            {
                if (double.TryParse(pixelPerimeter, out double pixelPerimeterValue) &&
                    double.TryParse(realPerimeter, out double realPerimeterValue))
                {
                    DataCollection.Add(new DataModel
                    {
                        ID = _currentId.ToString(),
                        Area = 0,  
                        Pixels = 0,  
                        Length = realPerimeterValue,  
                    });

                    _currentId++; 
                }
            };

            Point mousePosition = Mouse.GetPosition(this);
            resultWindow.Left = this.Left + mousePosition.X;
            resultWindow.Top = this.Top + mousePosition.Y;

            resultWindow.Show();
        }



        private (Point center, double radius) CalculateEnclosingCircle(float[] mask)
        {
            List<Point> edgePoints = new List<Point>();

            for (int y = 0; y < mOrghei; y++)
            {
                for (int x = 0; x < mOrgwid; x++)
                {
                    int index = y * mOrgwid + x;
                    if (mask[index] > 0.5 && IsEdgePixel(mask, x, y)) 
                    {
                        edgePoints.Add(new Point(x, y));
                    }
                }
            }

            var center = new Point(mOrgwid / 2, mOrghei / 2);
            double radius = 0;

            if (edgePoints.Count > 0)
            {

                center = new Point(edgePoints.Average(p => p.X), edgePoints.Average(p => p.Y));

                radius = edgePoints.Max(p => Distance(center, p));
            }

            return (center, radius);
        }

        private (Point point1, Point point2) CalculateMaxInternalDistancePoints(float[] mask)
        {
            List<Point> edgePoints = new List<Point>();

            for (int y = 0; y < mOrghei; y++)
            {
                for (int x = 0; x < mOrgwid; x++)
                {
                    int index = y * mOrgwid + x;
                    if (mask[index] > 0.5 && IsEdgePixel(mask, x, y)) 
                    {
                        edgePoints.Add(new Point(x, y));
                    }
                }
            }

            double maxDistance = 0;
            Point maxPoint1 = new Point();
            Point maxPoint2 = new Point();

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







        private List<UIElement> _drawnElements = new List<UIElement>();

        private void ClearDrawnElements(Canvas canvas)
        {
            foreach (var element in _drawnElements)
            {
                canvas.Children.Remove(element);
            }
            _drawnElements.Clear(); 
        }

        private void DrawLinesOnCanvas(Canvas canvas, float[] mask)
        {

            double scaleX = this.ImgCanvas.ActualWidth / this.mOrgwid;
            double scaleY = this.ImgCanvas.ActualHeight / this.mOrghei;
            double scale = Math.Min(scaleX, scaleY);





            var (center, radius) = CalculateEnclosingCircle(mask);
            Point point1Circle = new Point((center.X - radius) * scale, center.Y * scale); 
            Point point2Circle = new Point((center.X + radius) * scale, center.Y * scale); 

            var (maxPoint1, maxPoint2) = CalculateMaxInternalDistancePoints(mask);
            Point scaledMaxPoint1 = new Point(maxPoint1.X * scale, maxPoint1.Y * scale);
            Point scaledMaxPoint2 = new Point(maxPoint2.X * scale, maxPoint2.Y * scale);

            double diameterInRealUnits = 2 * radius * _pixelToRealRatio; 
            double maxInternalDistanceInRealUnits = Distance(maxPoint1, maxPoint2) * _pixelToRealRatio; 

            DrawLineOnCanvas(canvas, point1Circle, point2Circle, Brushes.Red, diameterInRealUnits);

            DrawLineOnCanvas(canvas, scaledMaxPoint1, scaledMaxPoint2, Brushes.Blue, maxInternalDistanceInRealUnits);
        }

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
            _drawnElements.Add(line); 

            TextBox lengthText = new TextBox
            {
                Text = $"{lengthInRealUnits:F2} {_unit}",
                Foreground = brush,
                FontSize = 12,
                Background = Brushes.Transparent, 
                BorderThickness = new Thickness(0), 
                IsReadOnly = true, 
                AcceptsReturn = false, 
                Padding = new Thickness(2, 0, 2, 0) 
            };

            Canvas.SetLeft(lengthText, point1.X);
            Canvas.SetTop(lengthText, point1.Y);

            ContextMenu contextMenu = new ContextMenu();
            MenuItem copyMenuItem = new MenuItem { Header = "Copy" };
            copyMenuItem.Click += (s, e) => Clipboard.SetText(lengthText.Text);
            contextMenu.Items.Add(copyMenuItem);
            lengthText.ContextMenu = contextMenu;

            lengthText.PreviewMouseRightButtonDown += (s, e) => e.Handled = true;

            canvas.Children.Add(lengthText);
            _drawnElements.Add(lengthText);
        }

        private double Distance(Point p1, Point p2)
        {
            return Math.Sqrt(Math.Pow(p1.X - p2.X, 2) + Math.Pow(p1.Y - p2.Y, 2));
        }

        private bool IsEdgePixel2(float[] mask, int x, int y)
        {
            int width = this.mOrgwid;
            int height = this.mOrghei;

            bool isEdge =
                (x > 0 && mask[y * width + (x - 1)] <= 0.5) ||  
                (x < width - 1 && mask[y * width + (x + 1)] <= 0.5) ||  
                (y > 0 && mask[(y - 1) * width + x] <= 0.5) ||  
                (y < height - 1 && mask[(y + 1) * width + x] <= 0.5);  

            return isEdge;
        }
        public class LabelItem
        {
            public string Name { get; set; }
            public Color Color { get; set; }
            public bool IsChecked { get; set; } = true;
            public float[] MaskData { get; set; } 
            public string JsonPath { get; set; } 
    }


        public class LabelViewModel
        {
            public ObservableCollection<LabelItem> Labels { get; set; } = new ObservableCollection<LabelItem>();

            public LabelViewModel()
            {

                Labels.Add(new LabelItem { Name = "Label 1", Color = ((SolidColorBrush)Brushes.Red).Color });
                Labels.Add(new LabelItem { Name = "Label 2", Color = ((SolidColorBrush)Brushes.Blue).Color });

            }
        }




        private void BLabel_Click(object sender, RoutedEventArgs e)
        {
            if (mMaskData == null)
            {
                MessageBox.Show("Please generate a mask first!");
                return;
            }

            var selectedLabel = SelectedLabel ?? Labels.FirstOrDefault();
            if (selectedLabel == null)
            {
                MessageBox.Show("Please add labels first!");
                return;
            }

            Microsoft.Win32.SaveFileDialog saveFileDialog = new Microsoft.Win32.SaveFileDialog
            {
                Title = "Save Label File",
                Filter = "JSON 文件 (*.json)|*.json",
                FileName = $"{selectedLabel.Name}.json"
            };

            if (saveFileDialog.ShowDialog() == true)
            {

                string jsonPath = saveFileDialog.FileName;
                var jsonData = new
                {
                    Label = selectedLabel.Name,
                    Color = selectedLabel.Color.ToString(),
                    Mask = selectedLabel.MaskData
                };

                try
                {

                    File.WriteAllText(jsonPath, JsonConvert.SerializeObject(jsonData, Formatting.Indented));
                    selectedLabel.JsonPath = jsonPath;

                    DisplayContour(selectedLabel.MaskData, selectedLabel.Color);

                    isLabelingMode = true;
                    StartsegButton.IsEnabled = false;
                    MessageBox.Show($"标签 '{selectedLabel.Name}' 已生成并保存至：\n{jsonPath}\n请进行标记，然后点击“启动分割”继续。");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"保存文件失败：{ex.Message}");
                }
            }
            else
            {
                MessageBox.Show("保存操作已取消。");
            }
        }



        private void UpdateMaskDisplay()
        {
            WriteableBitmap bp = new WriteableBitmap(mOrgwid, mOrghei, 96, 96, PixelFormats.Pbgra32, null);
            byte[] pixelData = new byte[mOrgwid * mOrghei * 4];
            Array.Clear(pixelData, 0, pixelData.Length);

            foreach (var label in Labels.Where(l => l.IsChecked))
            {
                for (int y = 0; y < mOrghei; y++)
                {
                    for (int x = 0; x < mOrgwid; x++)
                    {
                        int ind = y * mOrgwid + x;
                        if (label.MaskData[ind] > mSam.mask_threshold)
                        {
                            pixelData[4 * ind] = label.Color.B;
                            pixelData[4 * ind + 1] = label.Color.G;
                            pixelData[4 * ind + 2] = label.Color.R;
                            pixelData[4 * ind + 3] = 150; 
                        }
                    }
                }
            }

            bp.WritePixels(new Int32Rect(0, 0, mOrgwid, mOrghei), pixelData, mOrgwid * 4, 0);
            mMask.Source = bp;
        }

        public class ColorToBrushConverter : IValueConverter
        {
            public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            {
                if (value is Color color)
                {
                    return new SolidColorBrush(color);
                }
                return Brushes.Transparent;
            }

            public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            {
                if (value is SolidColorBrush brush)
                {
                    return brush.Color;
                }
                return Colors.Transparent;
            }
        }

        private void ChangeColor_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedLabel != null)
            {
                var currentColor = SelectedLabel.Color; 
                var colorDialog = new System.Windows.Forms.ColorDialog
                {
                    Color = System.Drawing.Color.FromArgb(currentColor.A, currentColor.R, currentColor.G, currentColor.B)
                };

                if (colorDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    SelectedLabel.Color = Color.FromRgb(colorDialog.Color.R, colorDialog.Color.G, colorDialog.Color.B);
                    UpdateMaskDisplay();
                }
            }
        }
        private void SaveImageWithLabels_Click(object sender, RoutedEventArgs e)
        {
            if (Labels.Count == 0)
            {
                MessageBox.Show("没有标签可保存！");
                return;
            }

            var saveDialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "PNG 图像 (*.png)|*.png|JPEG 图像 (*.jpg)|*.jpg"
            };

            if (saveDialog.ShowDialog() == true)
            {
                var bitmap = new RenderTargetBitmap((int)mImage.ActualWidth, (int)mImage.ActualHeight, 96, 96, PixelFormats.Pbgra32);
                bitmap.Render(mImage);

                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(bitmap));

                using (var fs = new FileStream(saveDialog.FileName, FileMode.Create))
                {
                    encoder.Save(fs);
                }

                MessageBox.Show($"图像已保存至: {saveDialog.FileName}");
            }
        }
        private void AddLabel_Click(object sender, RoutedEventArgs e)
        {
            Random rand = new Random();
            Color randomColor = Color.FromRgb((byte)rand.Next(256), (byte)rand.Next(256), (byte)rand.Next(256));

            var newLabel = new LabelItem
            {
                Name = $"Label {Labels.Count + 1}",
                Color = randomColor,
                IsChecked = true,
                MaskData = new float[mOrgwid * mOrghei] 
            };

            Labels.Add(newLabel);
            MessageBox.Show("新标签已添加！");
        }
        private void RenameLabel_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedLabel == null)
            {
                MessageBox.Show("请选择要重命名的标签！");
                return;
            }

            string newName = Microsoft.VisualBasic.Interaction.InputBox("输入新的标签名称：", "重命名标签", SelectedLabel.Name);
            if (!string.IsNullOrWhiteSpace(newName))
            {
                SelectedLabel.Name = newName;
                MessageBox.Show("标签已重命名！");
            }
        }
        private void DisplayContour(float[] maskData, Color color)
        {
            WriteableBitmap contourBitmap = new WriteableBitmap(mOrgwid, mOrghei, 96, 96, PixelFormats.Pbgra32, null);
            byte[] pixels = new byte[mOrgwid * mOrghei * 4];

            for (int y = 1; y < mOrghei - 1; y++)
            {
                for (int x = 1; x < mOrgwid - 1; x++)
                {
                    int index = y * mOrgwid + x;
                    if (maskData[index] > mSam.mask_threshold)
                    {

                        if (maskData[index - 1] <= mSam.mask_threshold || maskData[index + 1] <= mSam.mask_threshold ||
                            maskData[index - mOrgwid] <= mSam.mask_threshold || maskData[index + mOrgwid] <= mSam.mask_threshold)
                        {
                            pixels[4 * index] = color.B;
                            pixels[4 * index + 1] = color.G;
                            pixels[4 * index + 2] = color.R;
                            pixels[4 * index + 3] = 255;
                        }
                    }
                }
            }

            contourBitmap.WritePixels(new Int32Rect(0, 0, mOrgwid, mOrghei), pixels, mOrgwid * 4, 0);
            mMask.Source = contourBitmap;
        }

        private void Bdiameter_Click(object sender, RoutedEventArgs e)
        {

            this.currentMode = Mode.None;
            if (_pixelToRealRatio <= 0)
            {
                MessageBox.Show("Please set the scale first!");
                return;
            }

            if (this.mMaskData == null)
            {
                MessageBox.Show("Mask data not found!");
                return;
            }

            var canvas = this.ImgCanvas;

            ClearDrawnElements(canvas);

            DrawLinesOnCanvas(canvas, this.mMaskData);

            var (center, radius) = CalculateEnclosingCircle(this.mMaskData);
            double maskDiameterInPixels = radius * 2;
            double diameterInRealUnits = maskDiameterInPixels * _pixelToRealRatio;

            var (maxPoint1, maxPoint2) = CalculateMaxInternalDistancePoints(this.mMaskData);
            double maxInternalDistanceInPixels = Distance(maxPoint1, maxPoint2);
            double maxInternalDistanceInRealUnits = maxInternalDistanceInPixels * _pixelToRealRatio;

            

        }

    }
}






































        enum Operation
        {
            None,   
            Point,
            Box,
            Everything,
            Text
        }
    
    
