namespace NFMWorldLibrary;

public enum DistantOutlineBehavior
{
    DistanceFalloff = 0,
    DistanceFalloffWithCutoff = 1,
    ClassicCutoff = 2,
    AlwaysRender = 3,
    HideOutlines = 4
}

public static partial class World
{
    public static DistantOutlineBehavior DistantOutlineBehavior = DistantOutlineBehavior.ClassicCutoff;
    
    public static float OutlineFalloffStartDistance = 900f; //default camera is 800 behind and 250 above the car (838 total) so we shouldn't start the falloff too early
    public static float OutlineClassicCutoffDistance = 3000f; //distance from the original game where lines would start to disappear
    public static float OutlineMinimumVisibleThickness = 0.1f; //minimum thickness at which outlines are still rendered

}
