//using Emgu.CV.Stitching;
//using GalaSoft.MvvmLight;
//using GalaSoft.MvvmLight.Command;
//using System;
//using System.Collections.Generic;
//using System.Collections.ObjectModel;
//using System.ComponentModel;
//using System.Reflection;
//using System.Windows;
//using System.Windows.Input;
//using wpf522;

//namespace wpf522.ViewModel
//{
//        public class ViewModelMain : ViewModelBase
//        {
//            private string bindingText;
//            public string BindingText
//            {
//                get => bindingText;
//                set => Set(ref bindingText, value);
//            }

//            private double bindingNumber;
//            public double BindingNumber
//            {
//                get => bindingNumber;
//                set => Set(ref bindingNumber, value);
//            }

//            private bool _BoolIsChecked = true;
//            public bool BoolIsChecked
//            {
//                get => _BoolIsChecked;
//                set => Set(ref _BoolIsChecked, value);
//            }

           

//            private string showingText;
//            public string ShowingText
//            {
//                get => showingText;
//                set => Set(ref showingText, value);
//            }

//            public RelayCommand<MainWindow> CmdLoaded => new Lazy<RelayCommand<MainWindow>>(() => new RelayCommand<MainWindow>(Loaded)).Value;
//            private void Loaded(MainWindow window)
//            {
//                MessageBox.Show("MainWindow Loaded: " + window.ActualWidth + " * " + window.ActualHeight);
//            }

//            public RelayCommand<MouseEventArgs> CmdMouseMove => new RelayCommand<MouseEventArgs>(MouseMove);
//            private void MouseMove(MouseEventArgs e)
//            {
//                // 显示鼠标所在位置
//                System.Windows.Point point = e.GetPosition(e.Device.Target);
//                ShowingText = point.X + ", " + point.Y;
//            }

//            public RelayCommand CmdWithoutParameter => new Lazy<RelayCommand>(() => new RelayCommand(WithoutParameter)).Value;
//            private void WithoutParameter()
//            {
//                MessageBox.Show("Command Binding without parameter");
//            }

//            public RelayCommand<string> CmdWithParameter => new Lazy<RelayCommand<string>>(() => new RelayCommand<string>(WithParameter)).Value;
//            private void WithParameter(string info)
//            {
//                MessageBox.Show("Command Binding without parameter: " + info);
//            }

//            /// <summary>
//            /// 鼠标点击事件
//            /// </summary>
//            public RelayCommand<MouseButtonEventArgs> CmdMouseDown => new Lazy<RelayCommand<MouseButtonEventArgs>>(() => new RelayCommand<MouseButtonEventArgs>(MouseDown)).Value;
//            private void MouseDown(MouseButtonEventArgs e)
//            {
//                // 判断按下的鼠标按键
//                if (e.LeftButton == MouseButtonState.Pressed)
//                {
//                    MessageBox.Show("Left mouse button down.");
//                }
//                else if (e.RightButton == MouseButtonState.Pressed)
//                {
//                    MessageBox.Show("Right mouse button down.");
//                }
//                else if (e.MiddleButton == MouseButtonState.Pressed)
//                {
//                    MessageBox.Show("Middle mouse button down.");
//                }
//            }

//        }
//    }