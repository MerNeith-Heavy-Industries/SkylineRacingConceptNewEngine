using Maxine.Extensions.Mathematics;
using NFMWorld.Interp;

namespace NFMWorld;

public class PerspectiveCamera : Camera
{
    public const float DefaultFov = 58.715516388168026651329f;
    public float Fov { get; set; } = DefaultFov;

    /// <summary>When true, uses an orthographic projection instead of perspective.</summary>
    public bool IsOrthographic { get; set; } = false;
    /// <summary>World units per pixel for orthographic projection.</summary>
    public float OrthoScale { get; set; } = 1f;
    
    public override void OnBeforeRender(float alpha)
    {
        if (IsOrthographic)
        {
            float w = Width * OrthoScale;
            float h = Height * OrthoScale;
            ProjectionMatrix = Matrix.CreateOrthographic(w, h, Near, Far);
        }
        else
        {
            ProjectionMatrix = Matrix.CreatePerspectiveFieldOfView(MathUtil.DegreesToRadians(Fov), Width / (float)Height, Near, Far);
        }
        
        var interpolatedPosition = Interpolation.InterpolateCoord(Position, PreviousState.Position, alpha);
        var interpolatedLookAt = Interpolation.InterpolateCoord(LookAt, PreviousState.LookAt, alpha);
        var interpolatedUp = Interpolation.InterpolateCoord(Up, PreviousState.Up, alpha);
        ViewMatrix = Matrix.CreateLookAt(interpolatedPosition, interpolatedLookAt, interpolatedUp);
        ViewProjectionMatrix = ViewMatrix * ProjectionMatrix;
    }
}