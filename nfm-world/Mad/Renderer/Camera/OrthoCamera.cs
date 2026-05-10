using NFMWorld.Interp;

namespace NFMWorld;

public class OrthoCamera : Camera
{
    public override void OnBeforeRender(float alpha)
    {
        ProjectionMatrix = Matrix.CreateOrthographic(Width, Height, Near, Far);
        var interpolatedPosition = Interpolation.InterpolateCoord(Position, PreviousState.Position, alpha);
        var interpolatedLookAt = Interpolation.InterpolateCoord(LookAt, PreviousState.LookAt, alpha);
        var interpolatedUp = Interpolation.InterpolateCoord(Up, PreviousState.Up, alpha);
        ViewMatrix = Matrix.CreateLookAt(interpolatedPosition, interpolatedLookAt, interpolatedUp);
        ViewProjectionMatrix = ViewMatrix * ProjectionMatrix;
    }
}