using NFMWorld.DriverInterface;
using NFMWorld.Reactor;
using WorldXaml.UI.Yoga;
using static WorldXaml.UI.Yoga.Nodes;
using static NFMWorld.DriverInterface.UI.Nodes;
using static NFMWorld.UI.Test.Nodes;

namespace NFMWorld.UI.Test;

public class XamlTestView : Component
{
    private readonly XamlTestViewModel _vm = new();

    public XamlTestView()
    {
        DisableMemo(); // always re-render, bindings are external
    }

    public void Tick() => _vm.Tick();

    protected override VNode Render()
    {
        var vm = UseObservable(_vm);

        return View(
            name: "XamlTest",
            flexDirection: YgFlexDirection.Row,
            alignItems: YgAlign.Stretch,
            justifyContent: YgJustify.FlexStart,
            gap: 16f,
            padding: 20f,
            children: [
                // ── Left side ──────────────────────────────────────────
                FlexPanel(
                    flexDirection: YgFlexDirection.Column,
                    alignItems: YgAlign.FlexStart,
                    justifyContent: YgJustify.FlexStart,
                    gap: 16f,
                    padding: 20f,
                    children: [
                        // Section 1: Basic text bindings
                        S(w: vm.Title, font: vm.TitleFont, color: vm.TitleColor, strokeColor: new Color(0, 0, 0, 255)),
                        S(w: vm.Subtitle, font: vm.BodyFont, color: vm.SubtitleColor),

                        // Section 2: Inline text with counter
                        FlexPanel(flexDirection: YgFlexDirection.Row, gap: 4f, children:
                            S(w: $"Counter: {vm.Counter}", font: new Font(FontFamily.Adventure, FontStyle.Bold, 18), color: new Color(255, 255, 255, 255))
                        ),

                        // Section 3: Color swatches
                        FlexPanel(flexDirection: YgFlexDirection.Row, gap: 8f, alignItems: YgAlign.Center, children: [
                            SolidBox(backgroundColor: new Color(255, 0, 0, 255), width: 24f, height: 24f),
                            SolidBox(backgroundColor: new Color(0, 255, 0, 255), width: 24f, height: 24f),
                            SolidBox(backgroundColor: new Color(0, 0, 255, 255), width: 24f, height: 24f),
                            SolidBox(backgroundColor: new Color(255, 255, 0, 255), width: 24f, height: 24f),
                            S(w: "Color swatches", font: new Font(FontFamily.Adventure, FontStyle.Plain, 18), color: new Color(180, 180, 180, 255), marginLeft: 8f)
                        ]),

                        // Section 4: Visibility binding
                        FlexPanel(flexDirection: YgFlexDirection.Row, gap: 8f, alignItems: YgAlign.Center, children: [
                            SolidBox(backgroundColor: new Color(0, 255, 255, 255), width: 16f, height: 16f, visibility: vm.BadgeVisibility),
                            S(w: "Badge (visibility-bound)", font: new Font(FontFamily.Adventure, FontStyle.Plain, 18), color: new Color(255, 255, 255, 255))
                        ]),

                        // Section 5: HStack — horizontal and vertical
                        S(w: "HStack (Horizontal):", font: new Font(FontFamily.Adventure, FontStyle.Bold, 18), color: vm.AccentColor),
                        HStack(orientation: StackOrientation.Horizontal, gapColumn: 8f, child: FlexPanel(children: [
                            SolidBox(backgroundColor: new Color(255, 136, 0, 255), width: 40f, height: 40f),
                            SolidBox(backgroundColor: new Color(0, 136, 255, 255), width: 40f, height: 40f),
                            SolidBox(backgroundColor: new Color(136, 255, 0, 255), width: 40f, height: 40f)
                        ])),

                        S(w: "HStack (Vertical):", font: new Font(FontFamily.Adventure, FontStyle.Bold, 18), color: vm.AccentColor),
                        HStack(orientation: StackOrientation.Vertical, gapRow: 8f, child: FlexPanel(children: [
                            SolidBox(backgroundColor: new Color(255, 136, 0, 255), width: 40f, height: 40f),
                            SolidBox(backgroundColor: new Color(0, 136, 255, 255), width: 40f, height: 40f),
                            SolidBox(backgroundColor: new Color(136, 255, 0, 255), width: 40f, height: 40f)
                        ])),

                        // Section 6: Nested layout — Row of Columns
                        S(w: "Nested layout:", font: new Font(FontFamily.Adventure, FontStyle.Bold, 18), color: vm.AccentColor),
                        FlexPanel(flexDirection: YgFlexDirection.Row, gap: vm.CounterModulo, children: [
                            FlexPanel(flexDirection: YgFlexDirection.Column, gap: 4f, children: [
                                S(w: "Column A", font: new Font(FontFamily.Adventure, FontStyle.Bold, 14), color: new Color(255, 200, 100, 255)),
                                SolidBox(backgroundColor: new Color(170, 68, 68, 255), width: 60f, height: 20f),
                                SolidBox(backgroundColor: new Color(170, 102, 102, 255), width: 60f, height: 20f),
                                SolidBox(backgroundColor: new Color(170, 136, 136, 255), width: 60f, height: 20f)
                            ]),
                            FlexPanel(flexDirection: YgFlexDirection.Column, gap: 4f, children: [
                                S(w: "Column B", font: new Font(FontFamily.Adventure, FontStyle.Bold, 14), color: new Color(100, 200, 255, 255)),
                                SolidBox(backgroundColor: new Color(68, 68, 170, 255), width: 60f, height: 20f),
                                SolidBox(backgroundColor: new Color(102, 102, 170, 255), width: 60f, height: 20f),
                                SolidBox(backgroundColor: new Color(136, 136, 170, 255), width: 60f, height: 20f)
                            ])
                        ]),

                        // Section 7: Fade-in animation on mount
                        FlexPanel(flexDirection: YgFlexDirection.Row, gap: 8f, alignItems: YgAlign.Center, opacity: 1f, children: [
                            SolidBox(backgroundColor: new Color(255, 255, 255, 255), width: 12f, height: 12f),
                            S(w: "Fade-in on mount", font: new Font(FontFamily.Adventure, FontStyle.Plain, 18), color: new Color(255, 255, 255, 255))
                        ]),

                        // Section 8: Contents panel (placeholder)
                        S(w: "Contents panel:", font: new Font(FontFamily.Adventure, FontStyle.Bold, 18), color: vm.AccentColor),
                        S(w: "Contents panel", font: new Font(FontFamily.Adventure, FontStyle.Bold, 18), color: vm.AccentColor)
                    ]
                ),
                // ── Middle spacer ──────────────────────────────────────
                Node(flex: 1f)
            ]
        );
    }

    // Convenience shorthand for TextRun string — the w: namespace in XAML maps to NFMWorld native elements.
    // Since we're in Reactor now, we use the factory methods directly.
    // This helper creates styled text inline.
    private static VNode S(string w, Font font, Color color, Color? strokeColor = null, float marginLeft = 0f)
    {
        return FlexPanel(flexDirection: YgFlexDirection.Row, gap: 4f, marginLeft: marginLeft, children:
            TextBlock(
                font: font,
                color: color,
                strokeColor: strokeColor,
                text: w
            )
        );
    }

    // Static swatch colors as Color objects
    private static Color Swatch(string c) => Color.FromHex(c);
}
