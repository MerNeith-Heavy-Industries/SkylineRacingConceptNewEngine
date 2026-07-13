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
    public static float OutlineCullDistance = 3000f;
    public static float OutlineMinimumVisibleThickness = 0.1f;
    public const float OutlineCullDistanceReferenceThickness = 0.5f;

    public static float EffectiveOutlineCullDistance =>
        OutlineCullDistance * (OutlineThickness < 0f ? 0f : OutlineThickness) / OutlineCullDistanceReferenceThickness;

    public static float OutlineFalloffReferenceDistance =>
        OutlineCullDistance * OutlineMinimumVisibleThickness / OutlineCullDistanceReferenceThickness;
}
