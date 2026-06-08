using NFMWorldLibrary.FixedMath;

namespace NFMWorldLibrary.Backend;

/// <summary>
/// Functions that the gamemode is allowed to call on a client car.
/// </summary>
public interface IClientCarCallbacks
{
    public CarStats Stats { get; }
    public int GroundAt { get; }
    public int MaxRadius { get; }
    public f64Euler WheelAngle { get; set; }
    public f64Euler TurningWheelAngle { get; set; }
    public bool CastsShadow { get; set; }
    public bool? GetsShadowed { get; set; }
    public float? AlphaOverride { get; set; }
    public bool? Glow { get; set; }
    public bool? Finish { get; set; }

}