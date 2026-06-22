#nullable enable
[global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
internal static class __InterpolatorHook
{
    [System.Runtime.CompilerServices.ModuleInitializerAttribute]
    public static void Init()
    {
        global::WorldXaml.UI.Base.InterpolatorRegistry.RegisterInterpolator<global::WorldXaml.UI.Yoga.Node.MeasurementFlexBasis>(global::WorldXaml.UI.Yoga.YogaInterpolators.InterpolateMeasurementFlexBasis);
        global::WorldXaml.UI.Base.InterpolatorRegistry.RegisterInterpolator<global::WorldXaml.UI.Yoga.Node.MeasurementMarginPosition>(global::WorldXaml.UI.Yoga.YogaInterpolators.InterpolateMeasurementMarginPosition);
        global::WorldXaml.UI.Base.InterpolatorRegistry.RegisterInterpolator<global::WorldXaml.UI.Yoga.Node.MeasurementMultiMargin>(global::WorldXaml.UI.Yoga.YogaInterpolators.InterpolateMeasurementMultiMargin);
        global::WorldXaml.UI.Base.InterpolatorRegistry.RegisterInterpolator<global::WorldXaml.UI.Yoga.Node.MeasurementPadding>(global::WorldXaml.UI.Yoga.YogaInterpolators.InterpolateMeasurementPadding);
        global::WorldXaml.UI.Base.InterpolatorRegistry.RegisterInterpolator<global::WorldXaml.UI.Yoga.Node.MeasurementMultiPadding>(global::WorldXaml.UI.Yoga.YogaInterpolators.InterpolateMeasurementMultiPadding);
        global::WorldXaml.UI.Base.InterpolatorRegistry.RegisterInterpolator<global::WorldXaml.UI.Yoga.Node.MeasurementMultiBorder>(global::WorldXaml.UI.Yoga.YogaInterpolators.InterpolateMeasurementMultiBorder);
        global::WorldXaml.UI.Base.InterpolatorRegistry.RegisterInterpolator<global::WorldXaml.UI.Yoga.Node.MeasurementGap>(global::WorldXaml.UI.Yoga.YogaInterpolators.InterpolateMeasurementGap);
        global::WorldXaml.UI.Base.InterpolatorRegistry.RegisterInterpolator<global::WorldXaml.UI.Yoga.Node.MeasurementWidthHeight>(global::WorldXaml.UI.Yoga.YogaInterpolators.InterpolateMeasurementWidthHeight);
    }
}
