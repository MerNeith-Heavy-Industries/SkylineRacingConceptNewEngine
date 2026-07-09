using System.Runtime.CompilerServices;

namespace NFMWorld;

/// <summary>
/// Render bucket for draw ordering. Opaque draws come before transparent draws.
/// </summary>
public enum RenderBucket : byte
{
    Opaque = 0,
    Transparent = 1
}

/// <summary>
/// Sort key for ordering draws within a <see cref="RenderQueue"/>.
/// Packs a <see cref="RenderBucket"/> (2 bits) and a sort value (30 bits) into a single uint.
/// </summary>
public readonly struct SortKey(uint value) : IComparable<SortKey>
{
    public readonly uint Value = value;

    private const uint BucketShift = 30;
    private const uint SortValueMask = (1u << 30) - 1;

    public RenderBucket Bucket => (RenderBucket)(Value >> (int)BucketShift);
    public uint SortValue => Value & SortValueMask;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static SortKey Create(RenderBucket bucket, uint sortValue)
    {
        return new SortKey(((uint)bucket << (int)BucketShift) | (sortValue & SortValueMask));
    }

    /// <summary>
    /// Sort key for opaque draws. Sorts by material hash (to reduce GPU state changes),
    /// then by render order within the same material.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static SortKey ForOpaque(int materialHash, int renderOrder = 0)
    {
        // materialHash in top bits, renderOrder in low 2 bits
        uint sortValue = ((uint)materialHash << 2) | ((uint)renderOrder & 0x3);
        return Create(RenderBucket.Opaque, sortValue);
    }

    /// <summary>
    /// Sort key for transparent draws. Sorts back-to-front by depth for correct alpha blending.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static SortKey ForTransparent(float depth, int materialHash = 0)
    {
        // Invert depth for back-to-front: near objects get higher sort values.
        // Clamp depth to [0, 1], convert to 16-bit, then invert.
        var clamped = Math.Clamp(depth, 0f, 1f);
        var depthBits = (uint)((1f - clamped) * 0xFFFF);
        var sortValue = (depthBits << 14) | ((uint)materialHash & 0x3FFF);
        return Create(RenderBucket.Transparent, sortValue);
    }

    public int CompareTo(SortKey other) => Value.CompareTo(other.Value);

    public static bool operator <(SortKey a, SortKey b) => a.Value < b.Value;
    public static bool operator >(SortKey a, SortKey b) => a.Value > b.Value;
    public static bool operator <=(SortKey a, SortKey b) => a.Value <= b.Value;
    public static bool operator >=(SortKey a, SortKey b) => a.Value >= b.Value;
}
