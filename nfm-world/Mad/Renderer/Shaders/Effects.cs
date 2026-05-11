using System.Diagnostics.CodeAnalysis;
using Microsoft.Xna.Framework.Graphics;
using NFMWorld.Shaders.Generated;

namespace NFMWorld;

internal static class Effects
{
    public static PolyEffect Poly { get => CheckNotNull(field); private set; }
    public static LineEffect Line { get => CheckNotNull(field); private set; }
    public static BasicEffect Chip { get => CheckNotNull(field); private set; }
    public static BasicEffect Dust { get => CheckNotNull(field); private set; }
    public static BasicEffect Flame { get => CheckNotNull(field); private set; }
    public static BasicEffect FixHoop { get => CheckNotNull(field); private set; }
    public static SkyEffect Sky { get => CheckNotNull(field); private set; }
    public static GroundEffect Ground { get => CheckNotNull(field); private set; }
    public static MountainsEffect Mountains { get => CheckNotNull(field); private set; }

    private static T CheckNotNull<T>(T? field)
    {
        return field ?? ThrowException();

        T ThrowException()
        {
            throw new ArgumentNullException(nameof(field), $"Call {nameof(Effects)}.{nameof(Initialize)} before use.");
        }
    }

    [MemberNotNull(nameof(Poly), nameof(Line), nameof(Chip), nameof(Dust), nameof(Flame), nameof(FixHoop), nameof(Sky), nameof(Ground), nameof(Mountains))]
    public static void Initialize(GraphicsDevice graphicsDevice)
    {
        Poly = new PolyEffect(graphicsDevice, VFS.ReadAllBytes("./data/shaders/Poly.fxb"));
        Line = new LineEffect(graphicsDevice, VFS.ReadAllBytes("./data/shaders/Line.fxb"));
        Sky = new SkyEffect(graphicsDevice, VFS.ReadAllBytes("./data/shaders/Sky.fxb"));
        Ground = new GroundEffect(graphicsDevice, VFS.ReadAllBytes("./data/shaders/Ground.fxb"));
        Mountains = new MountainsEffect(graphicsDevice, VFS.ReadAllBytes("./data/shaders/Mountains.fxb"));

        Chip = new BasicEffect(graphicsDevice)
        {
            Name = "Chip Effect",
            LightingEnabled = false,
            TextureEnabled = false,
            VertexColorEnabled = true
        };
        Dust = new BasicEffect(graphicsDevice)
        {
            Name = "Dust Effect",
            LightingEnabled = false,
            TextureEnabled = false,
            VertexColorEnabled = true
        };
        Flame = new BasicEffect(graphicsDevice)
        {
            Name = "Flames Effect",
            LightingEnabled = false,
            TextureEnabled = false,
            VertexColorEnabled = true
        };
        FixHoop = new BasicEffect(graphicsDevice)
        {
            Name = "FixHoop Electricity Effect",
            LightingEnabled = false,
            TextureEnabled = false,
            VertexColorEnabled = true
        };
    }
}