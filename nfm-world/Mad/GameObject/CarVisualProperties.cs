using NFMWorldLibrary.Backend;
using NFMWorldLibrary.FixedMath;

namespace NFMWorld;

/// <summary>
/// Visual-only properties that gamemodes can set on a car through <see cref="IClientCarCallbacks"/>.
/// Owned by <see cref="CarVisual"/>.
/// </summary>
public class CarVisualProperties : IClientCarCallbacks
{
    public bool CastsShadow { get; set; }
    public bool? GetsShadowed { get; set; }
    public float? AlphaOverride { get; set; }
    public bool? Glow { get; set; }
    public bool? Finish { get; set; }

    /// <summary>
    /// Copies values from a <see cref="MeshedGameObject"/>'s visual properties onto this instance.
    /// Called after the parent CarVisual's mesh is set up to seed defaults.
    /// </summary>
    public void ApplyDefaultsFrom(MeshedGameObject source)
    {
        CastsShadow = source.CastsShadow;
        GetsShadowed = source.GetsShadowed;
        AlphaOverride = source.AlphaOverride;
        Glow = source.Glow;
        Finish = source.Finish;
    }
}
