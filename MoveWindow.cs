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



    public class MoveWindow : MetroWindow
    {
        #region ÒÆ¶¯



        private UIElement element = null;



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



        private bool isDrag = false;


        private Point forntPoint = new Point();





        private void Move_Down(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                this.Cursor = Cursors.Hand;
                isDrag = true;
                forntPoint = e.GetPosition(this);
            }
        }





        private void Move_Move(object sender, MouseEventArgs e)
        {
            Point current = e.GetPosition(this);
            if (isDrag == true && e.LeftButton == MouseButtonState.Pressed)
            {
                Point movep = new Point(current.X - forntPoint.X, current.Y - forntPoint.Y);
                this.Left += movep.X;
                this.Top += movep.Y;

            }
        }





        private void Move_Up(object sender, MouseButtonEventArgs e)
        {
            isDrag = false;
            this.Cursor = Cursors.Arrow;
        }





        private void Mouse_Leave(object sender, MouseEventArgs e)
        {
            isDrag = false;
            this.Cursor = Cursors.Arrow;
        }
        #endregion
    }
}

