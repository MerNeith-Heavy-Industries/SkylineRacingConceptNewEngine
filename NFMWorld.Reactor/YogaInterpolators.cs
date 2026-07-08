using System.Runtime.CompilerServices;

namespace NFMWorld.Reactor;

internal static class YogaInterpolators
{
    [ReactorInterpolator]
    public static MeasurementFlexBasis InterpolateMeasurementFlexBasis(MeasurementFlexBasis from, MeasurementFlexBasis to, float alpha)
    {
        if (from.PointValue is { } fromPoint && to.PointValue is { } toPoint)
        {
            return MeasurementFlexBasis.Point(fromPoint + (toPoint - fromPoint) * alpha);
        }

        if (from.PercentValue is { } fromPercent && to.PercentValue is { } toPercent)
        {
            return MeasurementFlexBasis.Percent(fromPercent + (toPercent - fromPercent) * alpha);
        }

        if (alpha < 0.5f) return from;
        return to;
    }

    [ReactorInterpolator]
    public static MeasurementMarginPosition InterpolateMeasurementMarginPosition(MeasurementMarginPosition from, MeasurementMarginPosition to, float alpha)
    {
        if (from.PointValue is { } fromPoint && to.PointValue is { } toPoint)
        {
            return MeasurementMarginPosition.Point(fromPoint + (toPoint - fromPoint) * alpha);
        }

        if (from.PercentValue is { } fromPercent && to.PercentValue is { } toPercent)
        {
            return MeasurementMarginPosition.Percent(fromPercent + (toPercent - fromPercent) * alpha);
        }

        if (alpha < 0.5f) return from;
        return to;
    }

    [ReactorInterpolator]
    public static MeasurementMultiMargin InterpolateMeasurementMultiMargin(MeasurementMultiMargin fromAll, MeasurementMultiMargin toAll, float alpha)
    {
        InlineArray4<MeasurementMarginPosition> sides = new();

        for (var i = 0; i < 4; i++)
        {
            var from = fromAll.Sides[i];
            var to = toAll.Sides[i];
            if (from.PointValue is { } fromPoint && to.PointValue is { } toPoint)
            {
                sides[i] = MeasurementMarginPosition.Point(fromPoint + (toPoint - fromPoint) * alpha);
            }
            else if (from.PercentValue is { } fromPercent && to.PercentValue is { } toPercent)
            {
                sides[i] = MeasurementMarginPosition.Percent(fromPercent + (toPercent - fromPercent) * alpha);
            }
            else
            {
                sides[i] = alpha < 0.5f ? from : to;
            }
        }

        return new MeasurementMultiMargin
        {
            Top = sides[0],
            Bottom = sides[1],
            Left = sides[2],
            Right = sides[3]
        };
    }

    [ReactorInterpolator]
    public static MeasurementPadding InterpolateMeasurementPadding(MeasurementPadding from, MeasurementPadding to, float alpha)
    {
        if (from.PointValue is { } fromPoint && to.PointValue is { } toPoint)
        {
            return MeasurementPadding.Point(fromPoint + (toPoint - fromPoint) * alpha);
        }

        if (from.PercentValue is { } fromPercent && to.PercentValue is { } toPercent)
        {
            return MeasurementPadding.Percent(fromPercent + (toPercent - fromPercent) * alpha);
        }

        if (alpha < 0.5f) return from;
        return to;
    }
    
    [ReactorInterpolator]
    public static MeasurementMultiPadding InterpolateMeasurementMultiPadding(MeasurementMultiPadding fromAll, MeasurementMultiPadding toAll, float alpha)
    {
        InlineArray4<MeasurementPadding> sides = new();

        for (var i = 0; i < 4; i++)
        {
            var from = fromAll.Sides[i];
            var to = toAll.Sides[i];
            if (from.PointValue is { } fromPoint && to.PointValue is { } toPoint)
            {
                sides[i] = MeasurementPadding.Point(fromPoint + (toPoint - fromPoint) * alpha);
            }
            else if (from.PercentValue is { } fromPercent && to.PercentValue is { } toPercent)
            {
                sides[i] = MeasurementPadding.Percent(fromPercent + (toPercent - fromPercent) * alpha);
            }
            else
            {
                sides[i] = alpha < 0.5f ? from : to;
            }
        }

        return new MeasurementMultiPadding
        {
            Top = sides[0],
            Bottom = sides[1],
            Left = sides[2],
            Right = sides[3]
        };
    }

    [ReactorInterpolator]
    public static MeasurementMultiBorder InterpolateMeasurementMultiBorder(MeasurementMultiBorder fromAll, MeasurementMultiBorder toAll, float alpha)
    {
        InlineArray4<float?> sides = new();

        for (var i = 0; i < 4; i++)
        {
            var from = fromAll.Sides[i];
            var to = toAll.Sides[i];
            sides[i] = from + (to - from) * alpha;
        }

        return new MeasurementMultiBorder
        {
            Top = sides[0],
            Bottom = sides[1],
            Left = sides[2],
            Right = sides[3]
        };
    }

    [ReactorInterpolator]
    public static MeasurementGap InterpolateMeasurementGap(MeasurementGap from, MeasurementGap to, float alpha)
    {
        if (from.PointValue is { } fromPoint && to.PointValue is { } toPoint)
        {
            return MeasurementGap.Point(fromPoint + (toPoint - fromPoint) * alpha);
        }

        if (from.PercentValue is { } fromPercent && to.PercentValue is { } toPercent)
        {
            return MeasurementGap.Percent(fromPercent + (toPercent - fromPercent) * alpha);
        }

        if (alpha < 0.5f) return from;
        return to;
    }

    [ReactorInterpolator]
    public static MeasurementWidthHeight InterpolateMeasurementWidthHeight(MeasurementWidthHeight from, MeasurementWidthHeight to, float alpha)
    {
        if (from.PointValue is { } fromPoint && to.PointValue is { } toPoint)
        {
            return MeasurementWidthHeight.Point(fromPoint + (toPoint - fromPoint) * alpha);
        }

        if (from.PercentValue is { } fromPercent && to.PercentValue is { } toPercent)
        {
            return MeasurementWidthHeight.Percent(fromPercent + (toPercent - fromPercent) * alpha);
        }

        if (alpha < 0.5f) return from;
        return to;
    }
}
