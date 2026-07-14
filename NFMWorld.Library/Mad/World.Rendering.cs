namespace NFMWorldLibrary;

public enum DistantOutlineBehavior
{
    // Perspective-like mode: outlines keep shrinking with distance, but never get hard-hidden by the minimum visible thickness.
    DistanceFalloff = 0,
    // Same perspective-like sizing as DistanceFalloff, but skips lines once they become too thin to matter visually.
    DistanceFalloffWithCutoff = 1,
    // Original NFM-style behavior render outlines at full width until a fixed distance, then hide them.
    ClassicCutoff = 2,
    // debug / simple mode, always render outlines
    AlwaysRender = 3,
    // Do not render outlines
    HideOutlines = 4
}

public static partial class World
{
    public static DistantOutlineBehavior DistantOutlineBehavior = DistantOutlineBehavior.ClassicCutoff;

    // The default follow camera is 838 units from the car, 900 prevents shrinking too early
    // Past this point, falloff modes use inverse-depth sizing
    public static float OutlineFalloffStartDistance = 900f;

    // ClassicCutoff is a sharp cutoff that matches the original game's approximate outline cutoff distance.
    // It does not scale with user outline width; thicker lines only survive farther in falloff with cutoff mode.
    public static float OutlineClassicCutoffDistance = 3000f;

    // Below this screen-space thickness, falloff with cutoff stops drawing a line because it is visually negligible.
    public static float OutlineMinimumVisibleThickness = 0.1f;
}
