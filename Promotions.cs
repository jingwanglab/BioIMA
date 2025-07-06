using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace wpf522
{
    public enum PromotionType
    {
        Point,
        Box
    }

    public abstract class Promotion
    {
        public UserControl mAnation;
        public abstract float[] GetInput();
        public abstract float[] GetLable();
        public PromotionType mType;
    }



    public class PointPromotion : Promotion
    {

        public PointPromotion(OpType optype)
        {
            this.mType = PromotionType.Point;
            this.m_Optype = optype;
        }
        public int X { get; set; }
        public int Y { get; set; }
        public override float[] GetInput()
        {
            return new float[2] { X, Y };
        }
        public override float[] GetLable()
        {
            if (this.m_Optype == OpType.ADD)
            {
                return new float[1] { 1 };
            }
            else
            {
                return new float[1] { 0 };
            }
        }

        public OpType m_Optype;
    }
    public enum OpType
    {
        ADD,
        REMOVE
    }



    class BoxPromotion : Promotion
    {
        public BoxPromotion()
        {
            this.mLeftUp = new PointPromotion(OpType.ADD);
            this.mRightBottom = new PointPromotion(OpType.ADD);
            this.mType = PromotionType.Box;
        }
        public override float[] GetInput()
        {
            return new float[4] { this.mLeftUp.X,
                this.mLeftUp.Y,
                this.mRightBottom.X,
                this.mRightBottom.Y };
        }
        public override float[] GetLable()
        {
            return new float[2] { 2, 3 };
        }
        public PointPromotion mLeftUp { get; set; }
        public PointPromotion mRightBottom { get; set; }

    }



    class MaskPromotion
    {
        public MaskPromotion(int wid, int hei)
        {
            this.mWidth = wid;
            this.mHeight = hei;
            this.mMask = new float[this.mWidth, this.mHeight];
        }

        float[,] mMask { get; set; }
        public int mWidth { get; set; }
        public int mHeight { get; set; }
    }



    class TextPromotion
    {
        public string mText { get; set; }
    }
}

