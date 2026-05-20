using System;

namespace BioIMA.Avalonia.Segmentation;

public enum SamPromotionType
{
    Point,
    Box
}

public enum SamOpType
{
    Add,
    Remove
}

public abstract class SamPromotion
{
    public SamPromotionType Type { get; protected set; }

    protected SamPromotion(SamPromotionType type)
    {
        Type = type;
    }

    public abstract float[] GetInput();
    public abstract float[] GetLabel();
}

public sealed class PointPromotion : SamPromotion
{
    public int X { get; set; }
    public int Y { get; set; }
    public SamOpType OpType { get; }

    public PointPromotion(SamOpType opType) : base(SamPromotionType.Point)
    {
        OpType = opType;
    }

    public override float[] GetInput()
    {
        return new float[] { X, Y };
    }

    public override float[] GetLabel()
    {
        return new float[]
        {
            OpType == SamOpType.Add ? 1f : 0f
        };
    }
}

public sealed class BoxPromotion : SamPromotion
{
    public PointPromotion LeftUp { get; set; }
    public PointPromotion RightBottom { get; set; }

    public BoxPromotion() : base(SamPromotionType.Box)
    {
        LeftUp = new PointPromotion(SamOpType.Add);
        RightBottom = new PointPromotion(SamOpType.Add);
    }

    public override float[] GetInput()
    {
        return new float[]
        {
            LeftUp.X, LeftUp.Y,
            RightBottom.X, RightBottom.Y
        };
    }

    public override float[] GetLabel()
    {
        // SAM box prompt 的两个 corner label
        return new float[] { 2f, 3f };
    }
}