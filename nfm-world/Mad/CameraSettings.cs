namespace NFMWorld;

public class CameraSettings
{
    public static float Fov { get; set; } = PerspectiveCamera.DefaultFov;
    public static bool SmoothFov { get; set; } = true;
    public static float RenderDistanceSqr = int.MaxValue;
}