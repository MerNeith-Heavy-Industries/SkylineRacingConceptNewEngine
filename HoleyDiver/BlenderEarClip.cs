using Microsoft.Xna.Framework;

namespace HoleyDiver;

/// <summary>
/// Blender-style two-pass ear clipping polygon triangulation with compact 2D KDTree
/// spatial index. Ported from Blender's BLI_polyfill_2d (GPL-2.0-or-later).
///
/// Features:
/// - O(n log n) complexity via KDTree spatial index on concave vertices
/// - Two-pass ear selection (CONVEX then TANGENTIAL) with desperate-mode fallback
/// - USE_CLIP_EVEN: advance ear each iteration to avoid fan-fill topology
/// - USE_CLIP_SWEEP: sweep back-and-forth to avoid lop-sided fans
/// - USE_CONVEX_SKIP: fast-path for convex polygons (no intersection tests)
/// - USE_KDTREE_INDEX_CACHE: cache last blocking vertex per ear for O(1) re-check
/// - USE_PRECOMPUTED_ISECT: precomputed edge vectors for fast point-in-triangle
/// - Handles key-holes (self-touching polygons at exact coordinates)
/// </summary>
internal static class BlenderEarClip
{
    // ── Compile-time flags (all enabled) ──────────────────────────────
    private const bool UseClipEven = true;
    private const bool UseClipSweep = true;
    private const bool UseConvexSkip = true;
    private const bool UseKdTree = true;
    private const bool UseKdTreeIndexCache = true;
    private const bool UsePrecomputedIsect = true;

    private const uint KdNodeUnset = uint.MaxValue;
    private const uint KdTreeIndexCacheUnset = uint.MaxValue;

    // ── Enums ─────────────────────────────────────────────────────────

    private enum PolySign : sbyte
    {
        Concave = -1,
        Tangential = 0,
        Convex = 1,
    }

    [Flags]
    private enum PolyIndexFlag : byte
    {
        None = 0,
        /// <summary>Triangle changed since caching; cached index needs verification.</summary>
        IndexCacheDirty = 1 << 0,
    }

    private const byte KdNodeFlagRemoved = 1 << 0;

    // ── Data structures ───────────────────────────────────────────────

    /// <summary>Circular doubly-linked list node for polygon vertices during ear-clipping.</summary>
    private sealed class PolyIndex
    {
        public PolyIndex? Next, Prev;
        public uint Index;
        public PolySign Sign;
        public PolyIndexFlag Flag;
        public uint IndexLastHit = KdTreeIndexCacheUnset;
    }

    /// <summary>Compact 2D KD-tree node (≤16 bytes if packed; uint indices instead of pointers).</summary>
    private struct KDTreeNode2D
    {
        public uint Neg, Pos;
        public uint Index;
        public byte Axis;    // 0 or 1
        public byte Flag;
        public uint Parent;
    }

    private struct KDTree2D
    {
        public KDTreeNode2D[] Nodes;
        public Vector2[] Coords;
        public uint Root;
        public uint NodeNum;
        public uint[] NodesMap;   // vertex index → kd-node index
    }

    private struct KDRange2D
    {
        public float Min, Max;
    }

    /// <summary>Precomputed edge vectors and constants for fast point-in-triangle test.</summary>
    private struct TriIsectPrecomputed
    {
        // edge[i] = v[(i+1)%3] - v[i];  c[i] = edge[i].x * v[i].y - v[i].x * edge[i].y
        public float E0X, E0Y, C0;
        public float E1X, E1Y, C1;
        public float E2X, E2Y, C2;
    }

    private struct PolyFill
    {
        public PolyIndex Indices;       // head of circular linked list
        public PolyIndex[] IndicesArray; // full array for KDTree init
        public Vector2[] Coords;
        public uint CoordsNum;
        public uint CoordsNumConcave;
        public uint[][] Tris;
        public uint TrisNum;
        public KDTree2D KdTree;
    }

    // ── Math helpers ──────────────────────────────────────────────────

    private static PolySign SignumEnum(float a)
    {
        if (a > 0f) return PolySign.Convex;
        if (a == 0f) return PolySign.Tangential;
        return PolySign.Concave;
    }

    /// <summary>2× signed area of triangle (v1,v2,v3). No /2 needed for sign tests.</summary>
    private static float AreaTriSignedV2Alt2x(Vector2 v1, Vector2 v2, Vector2 v3)
    {
        float d2x = v2.X - v1.X, d2y = v2.Y - v1.Y;
        float d3x = v3.X - v1.X, d3y = v3.Y - v1.Y;
        return d2x * d3y - d3x * d2y;
    }

    private static PolySign SpanTriV2Sign(Vector2 v1, Vector2 v2, Vector2 v3)
    {
        return SignumEnum(AreaTriSignedV2Alt2x(v3, v2, v1));
    }

    /// <summary>Total signed area of a polygon (un-normalized).</summary>
    private static float CrossPolyV2(Vector2[] coords)
    {
        float sum = 0f;
        for (int i = 0; i < coords.Length; i++)
        {
            int j = (i + 1) % coords.Length;
            sum += coords[i].X * coords[j].Y;
            sum -= coords[j].X * coords[i].Y;
        }
        return sum;
    }

    private static float CrossV2(Vector2 a, Vector2 b) => a.X * b.Y - a.Y * b.X;

    // ── Precomputed triangle intersection ─────────────────────────────

    private static void TriIsectPrecomputedInit(ref TriIsectPrecomputed t, Vector2 v0, Vector2 v1, Vector2 v2)
    {
        t.E0X = v1.X - v0.X; t.E0Y = v1.Y - v0.Y;
        t.E1X = v2.X - v1.X; t.E1Y = v2.Y - v1.Y;
        t.E2X = v0.X - v2.X; t.E2Y = v0.Y - v2.Y;
        t.C0 = t.E0X * v0.Y - v0.X * t.E0Y;
        t.C1 = t.E1X * v1.Y - v1.X * t.E1Y;
        t.C2 = t.E2X * v2.Y - v2.X * t.E2Y;
    }

    /// <summary>
    /// Test if point is inside triangle using precomputed edge vectors.
    /// Checks edge 1 first (opposite edge next→prev) as it fails most often.
    /// </summary>
    private static bool TriIsectPrecomputedTest(ref TriIsectPrecomputed t, Vector2 co)
    {
        return (t.E1Y * co.X - t.E1X * co.Y + t.C1 >= 0f) &&
               (t.E2Y * co.X - t.E2X * co.Y + t.C2 >= 0f) &&
               (t.E0Y * co.X - t.E0X * co.Y + t.C0 >= 0f);
    }

    // ── KDTree2D ──────────────────────────────────────────────────────

    private static void KDTree2DNew(ref KDTree2D tree, uint tot, Vector2[] coords)
    {
        tree.Coords = coords;
        tree.Root = KdNodeUnset;
        tree.NodeNum = tot;
    }

    private static void KDTree2DInit(ref KDTree2D tree, uint coordsNum, PolyIndex[] indices)
    {
        uint nodeIdx = 0;
        for (uint i = 0; i < coordsNum; i++)
        {
            if (indices[i].Sign != PolySign.Convex)
            {
                tree.Nodes[nodeIdx].Neg = KdNodeUnset;
                tree.Nodes[nodeIdx].Pos = KdNodeUnset;
                tree.Nodes[nodeIdx].Index = indices[i].Index;
                tree.Nodes[nodeIdx].Axis = 0;
                tree.Nodes[nodeIdx].Flag = 0;
                nodeIdx++;
            }
        }
    }

    private static uint KDTree2DBalanceRecursive(KDTreeNode2D[] nodes, uint nodeNum, byte axis, Vector2[] coords, uint ofs)
    {
        if (nodeNum == 0) return KdNodeUnset;
        if (nodeNum == 1) return ofs;

        // Quicksort-style partitioning around median
        uint neg = 0, pos = nodeNum - 1;
        uint median = nodeNum / 2;

        while (pos > neg)
        {
            float co = axis == 0 ? coords[nodes[pos].Index].X : coords[nodes[pos].Index].Y;
            uint i = neg;  // neg - 1 equivalent
            uint j = pos;

            while (true)
            {
                // Advance i
                do { i++; } while (
                    i < pos && (axis == 0 ? coords[nodes[i].Index].X : coords[nodes[i].Index].Y) < co);

                // Advance j
                do { j--; } while (
                    j > neg && (axis == 0 ? coords[nodes[j].Index].X : coords[nodes[j].Index].Y) > co);

                if (i >= j) break;

                // Swap nodes[i] and nodes[j] (only header fields)
                SwapNodeHead(ref nodes[i], ref nodes[j]);
            }

            // Swap nodes[i] and nodes[pos]
            SwapNodeHead(ref nodes[i], ref nodes[pos]);

            if (i >= median) pos = i - 1;
            if (i <= median) neg = i + 1;
        }

        // Set median node and recurse
        ref KDTreeNode2D node = ref nodes[median + ofs];
        node.Axis = axis;
        byte nextAxis = (byte)(axis ^ 1);
        uint leftCount = median;
        uint rightCount = nodeNum - (median + 1);

        node.Neg = KDTree2DBalanceRecursive(nodes, leftCount, nextAxis, coords, ofs);
        node.Pos = KDTree2DBalanceRecursive(nodes, rightCount, nextAxis, coords, median + 1 + ofs);

        return median + ofs;
    }

    private static void SwapNodeHead(ref KDTreeNode2D a, ref KDTreeNode2D b)
    {
        (a.Neg, b.Neg) = (b.Neg, a.Neg);
        (a.Pos, b.Pos) = (b.Pos, a.Pos);
        (a.Index, b.Index) = (b.Index, a.Index);
    }

    private static void KDTree2DBalance(ref KDTree2D tree)
    {
        tree.Root = KDTree2DBalanceRecursive(tree.Nodes, tree.NodeNum, 0, tree.Coords, 0);
    }

    private static void KDTree2DInitMapping(ref KDTree2D tree)
    {
        for (uint i = 0; i < tree.NodeNum; i++)
        {
            ref KDTreeNode2D node = ref tree.Nodes[i];
            if (node.Neg != KdNodeUnset)
                tree.Nodes[node.Neg].Parent = i;
            if (node.Pos != KdNodeUnset)
                tree.Nodes[node.Pos].Parent = i;

            tree.NodesMap[node.Index] = i;
        }

        tree.Nodes[tree.Root].Parent = KdNodeUnset;
    }

    /// <summary>
    /// Remove a vertex from the KD-tree. Walks up the tree to disconnect
    /// childless nodes and collapse single-child nodes (no full rebalance).
    /// </summary>
    private static void KDTree2DNodeRemove(ref KDTree2D tree, uint index)
    {
        uint nodeIdx = tree.NodesMap[index];
        if (nodeIdx == KdNodeUnset) return;

        tree.NodesMap[index] = KdNodeUnset;

        ref KDTreeNode2D node = ref tree.Nodes[nodeIdx];
        tree.NodeNum -= 1;
        node.Flag |= KdNodeFlagRemoved;

        // Walk up, disconnecting/collapsing
        while (node.Parent != KdNodeUnset)
        {
            uint nodeChild;
            if (node.Neg == KdNodeUnset)
                nodeChild = node.Pos;
            else if (node.Pos == KdNodeUnset)
                nodeChild = node.Neg;
            else
                break; // Both children set, nothing to collapse

            ref KDTreeNode2D parentNode = ref tree.Nodes[node.Parent];
            if (parentNode.Neg == nodeIdx)
                parentNode.Neg = nodeChild;
            else
                parentNode.Pos = nodeChild;

            if (nodeChild != KdNodeUnset)
                tree.Nodes[nodeChild].Parent = node.Parent;

            if ((parentNode.Flag & KdNodeFlagRemoved) == 0)
                break;

            nodeIdx = node.Parent;
            node = ref parentNode;
        }
    }

    /// <summary>Check if a specific cached vertex still blocks this ear.</summary>
    private static bool KDTree2DIsectTriSingle(ref KDTree2D tree, uint triI0, uint triI1, uint triI2, uint testIndex)
    {
        if (tree.NodesMap[testIndex] == KdNodeUnset)
            return false;
        if (testIndex == triI0 || testIndex == triI1 || testIndex == triI2)
            return false;

        Vector2 v0 = tree.Coords[triI0];
        Vector2 v1 = tree.Coords[triI1];
        Vector2 v2 = tree.Coords[triI2];
        Vector2 co = tree.Coords[testIndex];

        TriIsectPrecomputed triIsect = default;
        TriIsectPrecomputedInit(ref triIsect, v0, v1, v2);
        return TriIsectPrecomputedTest(ref triIsect, co);
    }

    /// <summary>Recursive triangle intersection with AABB pruning.</summary>
    private static uint KDTree2DIsectTriRecursive(
        ref KDTree2D tree,
        uint triI0, uint triI1, uint triI2,
        ref TriIsectPrecomputed triIsect,
        float triCenterX, float triCenterY,
        KDRange2D boundsX, KDRange2D boundsY,
        uint nodeIdx)
    {
        ref KDTreeNode2D node = ref tree.Nodes[nodeIdx];
        Vector2 co = tree.Coords[node.Index];

        if ((node.Flag & KdNodeFlagRemoved) == 0)
        {
            if (TriIsectPrecomputedTest(ref triIsect, co))
            {
                uint idx = node.Index;
                if (idx != triI0 && idx != triI1 && idx != triI2)
                    return idx;
            }
        }

        float nodeAxisCoord = node.Axis == 0 ? co.X : co.Y;
        float triCenterAxis = node.Axis == 0 ? triCenterX : triCenterY;
        float boundsMin = node.Axis == 0 ? boundsX.Min : boundsY.Min;
        float boundsMax = node.Axis == 0 ? boundsX.Max : boundsY.Max;

        uint result;

        if (triCenterAxis > nodeAxisCoord)
        {
            // Check positive (right/down) child first
            result = KdNodeUnset;
            if (node.Pos != KdNodeUnset && nodeAxisCoord <= boundsMax)
                result = KDTree2DIsectTriRecursive(ref tree, triI0, triI1, triI2, ref triIsect, triCenterX, triCenterY, boundsX, boundsY, node.Pos);

            if (result == KdNodeUnset && node.Neg != KdNodeUnset && nodeAxisCoord >= boundsMin)
                result = KDTree2DIsectTriRecursive(ref tree, triI0, triI1, triI2, ref triIsect, triCenterX, triCenterY, boundsX, boundsY, node.Neg);
        }
        else
        {
            // Check negative (left/up) child first
            result = KdNodeUnset;
            if (node.Neg != KdNodeUnset && nodeAxisCoord >= boundsMin)
                result = KDTree2DIsectTriRecursive(ref tree, triI0, triI1, triI2, ref triIsect, triCenterX, triCenterY, boundsX, boundsY, node.Neg);

            if (result == KdNodeUnset && node.Pos != KdNodeUnset && nodeAxisCoord <= boundsMax)
                result = KDTree2DIsectTriRecursive(ref tree, triI0, triI1, triI2, ref triIsect, triCenterX, triCenterY, boundsX, boundsY, node.Pos);
        }

        return result;
    }

    /// <summary>Find any vertex inside the triangle (triI0, triI1, triI2). Returns KdNodeUnset if none.</summary>
    private static uint KDTree2DIsectTri(ref KDTree2D tree, uint triI0, uint triI1, uint triI2)
    {
        Vector2 v0 = tree.Coords[triI0];
        Vector2 v1 = tree.Coords[triI1];
        Vector2 v2 = tree.Coords[triI2];

        KDRange2D boundsX = new()
        {
            Min = Math.Min(v0.X, Math.Min(v1.X, v2.X)),
            Max = Math.Max(v0.X, Math.Max(v1.X, v2.X)),
        };
        KDRange2D boundsY = new()
        {
            Min = Math.Min(v0.Y, Math.Min(v1.Y, v2.Y)),
            Max = Math.Max(v0.Y, Math.Max(v1.Y, v2.Y)),
        };

        float triCenterX = (v0.X + v1.X + v2.X) * (1f / 3f);
        float triCenterY = (v0.Y + v1.Y + v2.Y) * (1f / 3f);

        TriIsectPrecomputed triIsect = default;
        TriIsectPrecomputedInit(ref triIsect, v0, v1, v2);

        return KDTree2DIsectTriRecursive(ref tree, triI0, triI1, triI2, ref triIsect, triCenterX, triCenterY, boundsX, boundsY, tree.Root);
    }

    // ── PolyFill (ear-clip engine) ────────────────────────────────────

    private static uint[] PolyFillTriAdd(ref PolyFill pf)
    {
        return pf.Tris[pf.TrisNum++];
    }

    private static void PolyFillCoordRemove(ref PolyFill pf, PolyIndex pi)
    {
        if (UseKdTree && pf.KdTree.NodeNum > 0)
        {
            KDTree2DNodeRemove(ref pf.KdTree, pi.Index);
        }

        pi.Next!.Prev = pi.Prev;
        pi.Prev!.Next = pi.Next;

        if (pf.Indices == pi)
            pf.Indices = pi.Next!;

        pi.Next = pi.Prev = null;
        pf.CoordsNum -= 1;
    }

    private static void PolyFillCoordSignCalc(PolyFill pf, PolyIndex pi)
    {
        pi.Sign = SpanTriV2Sign(pf.Coords[pi.Prev!.Index], pf.Coords[pi.Index], pf.Coords[pi.Next!.Index]);
    }

    private static bool PolyFillEarTipCheck(PolyFill pf, PolyIndex piEarTip, PolySign signAccept)
    {
        // Fast-path for circles / all-convex
        if (pf.CoordsNumConcave == 0)
            return true;

        if (piEarTip.Sign != signAccept)
            return false;

        uint triI0 = piEarTip.Index;
        uint triI1 = piEarTip.Next!.Index;
        uint triI2 = piEarTip.Prev!.Index;

        if (UseKdTreeIndexCache)
        {
            uint cachedHit = piEarTip.IndexLastHit;
            if (cachedHit != KdTreeIndexCacheUnset)
            {
                if ((piEarTip.Flag & PolyIndexFlag.IndexCacheDirty) == 0)
                {
                    // Triangle unchanged — cached hit is guaranteed to still block
                    if (pf.KdTree.NodesMap[cachedHit] != KdNodeUnset)
                        return false;
                }
                else
                {
                    // Triangle changed — verify cached vertex still blocks
                    if (KDTree2DIsectTriSingle(ref pf.KdTree, triI0, triI1, triI2, cachedHit))
                    {
                        piEarTip.Flag &= ~PolyIndexFlag.IndexCacheDirty;
                        return false;
                    }
                    // Cache miss — fall through to full search
                }
            }
        }

        uint hit = KDTree2DIsectTri(ref pf.KdTree, triI0, triI1, triI2);
        if (hit != KdNodeUnset)
        {
            if (UseKdTreeIndexCache)
            {
                piEarTip.IndexLastHit = hit;
                piEarTip.Flag &= ~PolyIndexFlag.IndexCacheDirty;
            }
            return false;
        }

        return true;
    }

    /// <summary>Two-pass ear search: CONVEX then TANGENTIAL, with desperate-mode fallback.</summary>
    private static PolyIndex PolyFillEarTipFind(PolyFill pf, PolyIndex piEarInit, bool reverse)
    {
        uint coordsNum = pf.CoordsNum;

        // Two passes: first CONVEX (good ears), then TANGENTIAL (degenerate)
        for (PolySign signAccept = PolySign.Convex; signAccept >= PolySign.Tangential; signAccept--)
        {
            PolyIndex? piEar = piEarInit;

            for (uint i = 0; i < coordsNum; i++)
            {
                if (PolyFillEarTipCheck(pf, piEar!, signAccept))
                    return piEar!;

                piEar = reverse ? piEar!.Prev : piEar!.Next;
            }
        }

        // Desperate mode: return any convex or tangential vertex
        PolyIndex? pi = piEarInit;

        for (uint i = 0; i < coordsNum; i++)
        {
            if (pi!.Sign != PolySign.Concave)
                return pi;
            pi = pi.Next;
        }

        // All vertices concave — return the first one
        return pi!;
    }

    private static void PolyFillEarTipCut(ref PolyFill pf, PolyIndex piEarTip)
    {
        uint[] tri = PolyFillTriAdd(ref pf);
        tri[0] = piEarTip.Prev!.Index;
        tri[1] = piEarTip.Index;
        tri[2] = piEarTip.Next!.Index;

        PolyFillCoordRemove(ref pf, piEarTip);
    }

    private static void PolyFillTriangulate(ref PolyFill pf)
    {
        PolyIndex piEarInit = pf.Indices;
        bool reverse = false;

        while (pf.CoordsNum > 3)
        {
            PolyIndex piEar = PolyFillEarTipFind(pf, piEarInit, reverse);

            if (UseConvexSkip && piEar.Sign != PolySign.Convex)
                pf.CoordsNumConcave -= 1;

            PolyIndex piPrev = piEar.Prev!;
            PolyIndex piNext = piEar.Next!;

            PolyFillEarTipCut(ref pf, piEar);

            if (UseKdTreeIndexCache)
            {
                piPrev.Flag |= PolyIndexFlag.IndexCacheDirty;
                piNext.Flag |= PolyIndexFlag.IndexCacheDirty;
            }

            // Recompute signs of neighbors (removing the ear may have changed curvature)
            PolySign signOrigPrev = piPrev.Sign;
            PolySign signOrigNext = piNext.Sign;

            if (signOrigPrev != PolySign.Convex)
            {
                PolyFillCoordSignCalc(pf, piPrev);
                if (UseConvexSkip && piPrev.Sign == PolySign.Convex)
                {
                    pf.CoordsNumConcave -= 1;
                    if (UseKdTree)
                        KDTree2DNodeRemove(ref pf.KdTree, piPrev.Index);
                }
            }
            if (signOrigNext != PolySign.Convex)
            {
                PolyFillCoordSignCalc(pf, piNext);
                if (UseConvexSkip && piNext.Sign == PolySign.Convex)
                {
                    pf.CoordsNumConcave -= 1;
                    if (UseKdTree)
                        KDTree2DNodeRemove(ref pf.KdTree, piNext.Index);
                }
            }

            // Advance ear for next iteration
            piEarInit = reverse ? piPrev.Prev! : piNext.Next!;

            if (piEarInit.Sign != PolySign.Convex)
            {
                piEarInit = reverse ? piEarInit.Prev! : piEarInit.Next!;
                reverse = !reverse;
            }
        }

        // Final 3-vertex triangle
        if (pf.CoordsNum == 3)
        {
            uint[] tri = PolyFillTriAdd(ref pf);
            PolyIndex pi = pf.Indices;
            tri[0] = pi.Index;
            pi = pi.Next!;
            tri[1] = pi.Index;
            pi = pi.Next!;
            tri[2] = pi.Index;
        }
    }

    private static void PolyFillPrepare(
        ref PolyFill pf, Vector2[] coords, uint coordsNum, int coordsSign, uint[][] rTris, PolyIndex[] rIndices)
    {
        PolyIndex[] indices = rIndices;
        pf.Indices = rIndices[0];  // head of linked list
        pf.IndicesArray = rIndices; // full array for KDTree init
        pf.Coords = coords;
        pf.CoordsNum = coordsNum;
        pf.CoordsNumConcave = 0;
        pf.Tris = rTris;
        pf.TrisNum = 0;

        // Auto-detect winding if not specified
        if (coordsSign == 0)
            coordsSign = CrossPolyV2(coords) <= 0f ? 1 : -1;

        if (coordsSign == 1)
        {
            // Clockwise — forward linking
            for (uint i = 0; i < coordsNum; i++)
            {
                indices[i].Next = (i + 1 < coordsNum) ? indices[i + 1] : indices[0];
                indices[i].Prev = (i > 0) ? indices[i - 1] : indices[coordsNum - 1];
                indices[i].Index = i;
            }
        }
        else
        {
            // Counter-clockwise — reversed indexing
            uint n = coordsNum - 1;
            for (uint i = 0; i < coordsNum; i++)
            {
                indices[i].Next = (i + 1 < coordsNum) ? indices[i + 1] : indices[0];
                indices[i].Prev = (i > 0) ? indices[i - 1] : indices[coordsNum - 1];
                indices[i].Index = n - i;
            }
        }

        // Close the circular list
        indices[0].Prev = indices[coordsNum - 1];
        indices[coordsNum - 1].Next = indices[0];

        // Compute initial signs
        for (uint i = 0; i < coordsNum; i++)
        {
            PolyIndex pi = indices[i];
            PolyFillCoordSignCalc(pf, pi);
            if (UseConvexSkip && pi.Sign != PolySign.Convex)
                pf.CoordsNumConcave += 1;

            if (UseKdTreeIndexCache)
            {
                pi.Flag = PolyIndexFlag.None;
                pi.IndexLastHit = KdTreeIndexCacheUnset;
            }
        }
    }

    private static void PolyFillCalc(ref PolyFill pf)
    {
        if (UseKdTree && UseConvexSkip && pf.CoordsNumConcave > 0)
        {
            KDTree2DNew(ref pf.KdTree, pf.CoordsNumConcave, pf.Coords);
            KDTree2DInit(ref pf.KdTree, pf.CoordsNum, pf.IndicesArray);
            KDTree2DBalance(ref pf.KdTree);
            KDTree2DInitMapping(ref pf.KdTree);
        }

        PolyFillTriangulate(ref pf);
    }

    // ── Public API ────────────────────────────────────────────────────

    /// <summary>
    /// Triangulate a simple 2D polygon using Blender's two-pass ear-clipping algorithm.
    /// </summary>
    /// <param name="coords">2D polygon vertices (simple boundary, no holes required).</param>
    /// <param name="coordsSign">Winding: 1 = clockwise, -1 = counter-clockwise, 0 = auto-detect.</param>
    /// <returns>Array of triangles, each triangle is uint[3] with vertex indices into <paramref name="coords"/>.</returns>
    public static uint[][] Triangulate(Vector2[] coords, int coordsSign = 0)
    {
        uint coordsNum = (uint)coords.Length;
        if (coordsNum < 3)
            return Array.Empty<uint[]>();

        uint trisMax = coordsNum - 2;
        uint[][] tris = new uint[trisMax][];

        PolyIndex[] indices = new PolyIndex[coordsNum];
        for (uint i = 0; i < coordsNum; i++)
            indices[i] = new PolyIndex();

        PolyFill pf = default;
        PolyFillPrepare(ref pf, coords, coordsNum, coordsSign, tris, indices);

        if (UseKdTree && pf.CoordsNumConcave > 0)
        {
            pf.KdTree.Nodes = new KDTreeNode2D[pf.CoordsNumConcave];
            pf.KdTree.NodesMap = new uint[coordsNum];
            Array.Fill(pf.KdTree.NodesMap, KdNodeUnset);
        }
        else
        {
            pf.KdTree.NodeNum = 0;
        }

        PolyFillCalc(ref pf);

        // Trim to actual triangle count
        if (pf.TrisNum < trisMax)
        {
            uint[][] result = new uint[pf.TrisNum][];
            Array.Copy(tris, result, pf.TrisNum);
            return result;
        }

        return tris;
    }
}
