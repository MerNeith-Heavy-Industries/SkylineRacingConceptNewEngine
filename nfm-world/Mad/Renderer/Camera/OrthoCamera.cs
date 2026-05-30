using NFMWorld.Interp;

namespace NFMWorld;

public class OrthoCamera : Camera
{
    /// <summary>World units per pixel for orthographic projection.</summary>
    public float OrthoScale { get; set; } = 1f;

    public override void OnBeforeRender(float alpha)
    {
        ProjectionMatrix = Matrix.CreateOrthographic(Width * OrthoScale, Height * OrthoScale, Near, Far);
        var interpolatedPosition = Interpolation.InterpolateCoord(Position, PreviousState.Position, alpha);
        var interpolatedLookAt = Interpolation.InterpolateCoord(LookAt, PreviousState.LookAt, alpha);
        var interpolatedUp = Interpolation.InterpolateCoord(Up, PreviousState.Up, alpha);
        ViewMatrix = Matrix.CreateLookAt(interpolatedPosition, interpolatedLookAt, interpolatedUp);
        ViewProjectionMatrix = ViewMatrix * ProjectionMatrix;
    }
}