using System.Numerics;
using Microsoft.Extensions.Logging;

namespace WorldXaml.UI.Yoga;

internal static class ReactorG
{
    public static float Scale
    {
        get => IReactorGraphicsBackend.Backend.Scale;
    }

    public static float Alpha
    {
        set => IReactorGraphicsBackend.Backend.Graphics.Alpha = value;
    }
}

public interface IReactorGraphicsBackend
{
    /// <summary>
    /// Assign this in your project to provide the graphics implementation for NFMWorld.Reactor. This must be set
    /// before any UI elements are created or used.
    /// </summary>
    public static IReactorGraphicsBackend Backend
    {
        internal get
        {
            return field ?? ThrowNotInitialized();

            IReactorGraphicsBackend ThrowNotInitialized()
            {
                throw new InvalidOperationException($"{nameof(IReactorGraphicsBackend)}.{nameof(Backend)} needs to be set before it can be used.");
            }
        }
        set;
    }

    /// <summary>
    /// Set this to the global scale to apply to all elements. This is useful for things like DPI scaling or in-game UI
    /// scaling.
    /// </summary>
    float Scale { get; }
    
    /// <summary>
    /// Set this to the size of your game's viewport in pixels. This is used for things like percentage-based sizes and
    /// for clipping.
    /// </summary>
    Vector2 Viewport { get; }
    
    /// <summary>
    /// Set this to an implementation of IXamlGraphics.
    /// </summary>
    IReactorGraphics Graphics { get; }
}

public interface IReactorGraphics
{
    /// <summary>
    /// We'll set this property based on the `Opacity` property of a given element, right before rendering it.
    /// </summary>
    float Alpha { set; }
}