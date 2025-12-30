using FixedMathSharp;

namespace SoftFloat;

public static partial class FixedTrigLUT
{
    public static fix64 SinDeg(fix64 value)
    {
        // wrap to 0-360 degrees
        var degrees = value % 360;
        if (degrees < fix64.Zero)
            degrees += 360;
        // scale to 0-36000, interpolate with fractional part
        var scaled = degrees * 100;
        var index = (int)(scaled.Raw >> fix64.FRACTION_BITS); // integer part
        var frac = scaled - fix64.FromRaw((long)index << fix64.FRACTION_BITS); // fractional part
        var nextIndex = index + 1;
        if (nextIndex >= 36000)
            nextIndex = 0;
        var sinA = fix64.FromRaw(SinTable[index]);
        var sinB = fix64.FromRaw(SinTable[nextIndex]);
        var delta = sinB - sinA;
        return sinA + (delta * frac);
    }
    
    public static fix64 CosDeg(fix64 value)
    {
        return SinDeg(value + 90);
    }
}