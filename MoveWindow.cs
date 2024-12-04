using wpf522.Expends;
using MahApps.Metro.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows;

namespace wpf522
{
    /// <summary>
    /// 可以移动的窗口
    /// </summary>
    public class MoveWindow : MetroWindow
    {
        #region 移动
        /// <summary>
        /// 用于移动的元素
        /// </summary>
        private UIElement element = null;
        /// <summary>
        /// 用于移动的元素
        /// </summary>
        public UIElement MoveUIElement
        {
            get => element;
            set
            {
                if (value == element) return;
                element = value;
                element.MouseDown += Move_Down;
                element.MouseMove += Move_Move;
                element.MouseUp += Move_Up;
                element.MouseLeave += Mouse_Leave;
            }
        }
        /// <summary>
        /// 是否拖动
        /// </summary>
        private bool isDrag = false;
        /// 之前的点
        /// </summary>
        private Point forntPoint = new Point();
        /// <summary>
        /// 按下
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Move_Down(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                this.Cursor = Cursors.Hand;
                isDrag = true;
                forntPoint = e.GetPosition(this);
            }
        }
        /// <summary>
        /// 移动
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Move_Move(object sender, MouseEventArgs e)
        {
            Point current = e.GetPosition(this);
            if (isDrag == true && e.LeftButton == MouseButtonState.Pressed)
            {
                Point movep = new Point(current.X - forntPoint.X, current.Y - forntPoint.Y);
                this.Left += movep.X;
                this.Top += movep.Y;
                //forntPoint = e.GetPosition(this);
            }
        }
        /// <summary>
        /// 抬起
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Move_Up(object sender, MouseButtonEventArgs e)
        {
            isDrag = false;
            this.Cursor = Cursors.Arrow;
        }
        /// <summary>
        /// 离开
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Mouse_Leave(object sender, MouseEventArgs e)
        {
            isDrag = false;
            this.Cursor = Cursors.Arrow;
        }
        #endregion
    }
}
