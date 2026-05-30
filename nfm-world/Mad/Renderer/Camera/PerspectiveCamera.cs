using Maxine.Extensions.Mathematics;
using NFMWorld.Interp;

namespace NFMWorld;

public class PerspectiveCamera : Camera
{
    public const float DefaultFov = 58.715516388168026651329f;
    public float Fov { get; set; } = DefaultFov;
    
    public override void OnBeforeRender(float alpha)
    {
        ProjectionMatrix = Matrix.CreatePerspectiveFieldOfView(MathUtil.DegreesToRadians(Fov), Width / (float)Height, Near, Far);
        var interpolatedPosition = Interpolation.InterpolateCoord(Position, PreviousState.Position, alpha);
        var interpolatedLookAt = Interpolation.InterpolateCoord(LookAt, PreviousState.LookAt, alpha);
        var interpolatedUp = Interpolation.InterpolateCoord(Up, PreviousState.Up, alpha);
        ViewMatrix = Matrix.CreateLookAt(interpolatedPosition, interpolatedLookAt, interpolatedUp);
        ViewProjectionMatrix = ViewMatrix * ProjectionMatrix;
    }
}