using System.Collections;
using System.Runtime.InteropServices;
using Microsoft.Xna.Framework.Graphics;

namespace NFMWorld;

/// <summary>
/// Unified render queue that collects both instanced draws (batched by render element)
/// and immediate draws (deferred, sorted by <see cref="SortKey"/>). Replaces the
/// <c>RenderDataCache</c> inner class of <c>Scene</c>.
/// </summary>
public class RenderQueue(GraphicsDevice graphicsDevice) : IDisposable
{
    // ── Instanced queue (migrated from RenderDataCache) ──

    private sealed class CachedInstancedGroup(List<InstanceData> instances)
    {
        public readonly List<InstanceData> Instances = instances;
        public readonly List<InstanceData> OldInstances = [];
        public DynamicVertexBuffer? VertexBuffer;
        public int HashCode;
    }

    /// <summary>Outer key = RenderOrder, inner key = IInstancedRenderElement.</summary>
    private readonly SortedDictionary<int, Dictionary<IInstancedRenderElement, CachedInstancedGroup>> _instancedCache = new();

    private readonly List<IInstancedRenderElement> _elementsToPrune = [];

    // ── Immediate queue ──

    private readonly List<(SortKey Key, IImmediateRenderElement Element)> _immediateDraws = [];

    // ── Public API ──

    /// <summary>
    /// Reset per-frame state. Prunes instanced entries not seen for two consecutive frames.
    /// Must be called at the start of each render pass.
    /// </summary>
    public void Clear()
    {
        foreach (var (_, innerCache) in _instancedCache)
        {
            _elementsToPrune.Clear();
            foreach (var (element, group) in innerCache)
            {
                if (group.Instances.Count == 0)
                {
                    _elementsToPrune.Add(element);
                }
                else
                {
                    CollectionsMarshal.SetCount(group.Instances, 0);
                }
            }

            foreach (var element in _elementsToPrune)
            {
                if (innerCache.TryGetValue(element, out var group))
                {
                    group.VertexBuffer?.Dispose();
                    innerCache.Remove(element);
                }
            }
        }

        _immediateDraws.Clear();
    }

    /// <summary>
    /// Queue an instanced draw. Draws with the same <paramref name="element"/> and
    /// <paramref name="renderOrder"/> are batched into a single GPU instanced draw call.
    /// </summary>
    public void AddInstanced(IInstancedRenderElement element, in InstanceData data, int renderOrder = 0)
    {
        if (!_instancedCache.TryGetValue(renderOrder, out var innerCache))
        {
            _instancedCache[renderOrder] = innerCache =
                new Dictionary<IInstancedRenderElement, CachedInstancedGroup>();
        }

        ref var entry = ref CollectionsMarshal.GetValueRefOrAddDefault(innerCache, element, out var exists);
        if (!exists)
        {
            entry = new CachedInstancedGroup([data]);
        }
        else
        {
            entry!.Instances.Add(data);
        }
    }

    /// <summary>
    /// Queue an immediate (non-instanced) draw. Deferred draws are sorted by
    /// <paramref name="sortKey"/> before execution in <see cref="Flush"/>.
    /// </summary>
    public void AddImmediate(SortKey sortKey, IImmediateRenderElement element)
    {
        _immediateDraws.Add((sortKey, element));
    }

    /// <summary>
    /// Execute all queued draws in the correct order:
    /// 1. Opaque immediate draws (environment — behind geometry)
    /// 2. Instanced batches (sorted by render order)
    /// 3. PostOpaque immediate draws (depth-read-only — on top of stage, below cars)
    /// 4. Transparent immediate draws (effects — in front of everything)
    /// </summary>
    public void Flush(Camera camera, Lighting? lighting)
    {
        // Sort immediate draws by SortKey (opaque < postOpaque < transparent, then by material/depth)
        if (_immediateDraws.Count > 0)
        {
            _immediateDraws.Sort(static (a, b) => a.Key.CompareTo(b.Key));
        }

        // 1. Draw opaque immediate draws (environment: Sky, Ground, GroundPolys, Mountains)
        var i = 0;
        while (i < _immediateDraws.Count && _immediateDraws[i].Key.Bucket == RenderBucket.Opaque)
        {
            _immediateDraws[i].Element.Render(camera, lighting);
            i++;
        }

        // 2. Draw instanced batches interleaved with PostOpaque:
        //    renderOrder 0 (stage) → PostOpaque (FixFlare) → renderOrder 1+ (cars, glass)
        var firstRenderOrder = true;
        foreach (var (_, innerCache) in _instancedCache)
        {
            foreach (var (renderElement, cachedGroup) in innerCache)
            {
                var instances = cachedGroup.Instances;
                if (instances.Count == 0)
                    continue;

                var oldInstances = cachedGroup.OldInstances;
                var currentHashCode = GetInstanceDataHashCode(CollectionsMarshal.AsSpan(instances));

                if (cachedGroup.VertexBuffer == null ||
                    currentHashCode != cachedGroup.HashCode ||
                    !AreInstanceDataListsEqual(
                        CollectionsMarshal.AsSpan(instances),
                        CollectionsMarshal.AsSpan(oldInstances)))
                {
                    var instanceSpan = CollectionsMarshal.AsSpan(instances);

                    if (cachedGroup.VertexBuffer == null ||
                        cachedGroup.VertexBuffer.VertexCount < instances.Count)
                    {
                        cachedGroup.VertexBuffer?.Dispose();
                        cachedGroup.VertexBuffer = new DynamicVertexBuffer(
                            graphicsDevice, InstanceData.InstanceDeclaration,
                            instances.Count, BufferUsage.WriteOnly)
                        {
                            Name = "Instance Data Vertex Buffer",
                            Tag = this
                        };
                    }

                    cachedGroup.VertexBuffer.SetDataEXT(instanceSpan, SetDataOptions.Discard);
                    cachedGroup.HashCode = currentHashCode;

                    CollectionsMarshal.SetCount(oldInstances, instances.Count);
                    instanceSpan.CopyTo(CollectionsMarshal.AsSpan(oldInstances));
                }

                renderElement.Render(camera, lighting, cachedGroup.VertexBuffer, instances.Count);
            }

            // After the first (lowest) renderOrder, flush PostOpaque immediate draws
            // so they sit on top of stage pieces but below cars
            if (firstRenderOrder)
            {
                firstRenderOrder = false;
                while (i < _immediateDraws.Count && _immediateDraws[i].Key.Bucket == RenderBucket.PostOpaque)
                {
                    _immediateDraws[i].Element.Render(camera, lighting);
                    i++;
                }
            }
        }

        // 3. Draw transparent immediate draws (effects: Flames, Dust, Chips, Sparks)
        while (i < _immediateDraws.Count)
        {
            _immediateDraws[i].Element.Render(camera, lighting);
            i++;
        }
    }

    // ── Cleanup ──

    ~RenderQueue()
    {
        Dispose(false);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        foreach (var (_, innerCache) in _instancedCache)
        foreach (var (_, group) in innerCache)
        {
            group.VertexBuffer?.Dispose();
        }

        _instancedCache.Clear();
        _immediateDraws.Clear();
    }

    // ── Hash / equality helpers ──

    private static int GetInstanceDataHashCode(ReadOnlySpan<InstanceData> data)
    {
        var hc = data.Length;
        foreach (ref readonly var val in data)
        {
            hc = unchecked(hc * 314159 + val.GetHashCode());
        }

        return hc;
    }

    private static bool AreInstanceDataListsEqual(ReadOnlySpan<InstanceData> a, ReadOnlySpan<InstanceData> b)
    {
        if (a.Length != b.Length)
            return false;

        for (var i = 0; i < a.Length; i++)
        {
            if (!a[i].Equals(b[i]))
                return false;
        }

        return true;
    }
}
