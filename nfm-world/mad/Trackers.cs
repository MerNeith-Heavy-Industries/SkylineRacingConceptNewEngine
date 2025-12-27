using System.Runtime.InteropServices;
using Microsoft.Xna.Framework.Graphics;
using NFMWorld.Util;
using SoftFloat;
using Stride.Core.Mathematics;

namespace NFMWorld.Mad;

internal class Trackers
{
    // This will be replaced with something better after Phy's physics rewrite.
    public readonly struct TempTracker(int index, int x, int z, int radx, int radz) : IQuadObject
    {
        public readonly int Index = index;
        private readonly f64Bounds _bounds = new(
            x - radx,
            z - radz,
            radx * 2,
            radz * 2
        );
        
        public f64Bounds GetBounds()
        {
            return _bounds;
        }
    }
    
    internal static QuadTree<TempTracker> TrackersQuadTree = new(0,0,0,0);

    internal static void LoadTrackers(IReadOnlyList<GameObject> elements, int sx, int ncx, int sz, int ncz)
    {
        TrackersQuadTree = new QuadTree<TempTracker>(sx, sz, ncx, ncz);
        
        TrackersQuadTree.TrimExcess();
        
        // maxine: remove trackers.sect which was assigned here. it's not used anymore.
    }
    
    private static List<TempTracker> _tempTrackers = new();
    public static ReadOnlySpan<TempTracker> RetrievePoint(fix64 x, fix64 z)
    {
        _tempTrackers.Clear();
        TrackersQuadTree.RetrievePoint(_tempTrackers, x, z);
        return CollectionsMarshal.AsSpan(_tempTrackers);
    }
}