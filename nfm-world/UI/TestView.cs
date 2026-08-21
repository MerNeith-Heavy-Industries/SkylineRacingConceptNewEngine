using NFMWorld.DriverInterface.DriverInterface;
using NFMWorld.Reactor;

namespace WorldXaml.UI.Yoga;

public static class TestView
{
    public static View Render(ref int counter)
    {
        // Text bindings
        string title = "XAML Test View";
        string subtitle = "All systems operational";

        // Color bindings
        Color titleColor = new Color(255, 255, 255, 255);
        Color subtitleColor = new Color(180, 180, 180, 255);
        Color accentColor = new Color(100, 200, 255, 255);

        // Font bindings
        Font titleFont = new Font(FontFamily.Adventure, FontStyle.Bold, 32);
        Font bodyFont = new Font(FontFamily.Adventure, FontStyle.Plain, 18);

        // Visibility binding
        Visibility badgeVisibility = Visibility.Visible;

        var counterModulo = counter % 63;

        return new View()
        {
            Name = "XamlTest",
            Styles = new()
            {
                FlexDirection = FlexDirection.Row,
                AlignItems = Align.Stretch,
                JustifyContent = Justify.FlexStart,
                GapRow = 16f,
                GapColumn = 16f,
                PaddingTop = 20f,
                PaddingBottom = 20f,
                PaddingLeft = 20f,
                PaddingRight = 20f,
            },
            Children =
            {
                // ── Left side ──────────────────────────────────────────
                new View
                {
                    Styles = new Styles()
                    {
                        FlexDirection = FlexDirection.Column,
                        AlignItems = Align.FlexStart,
                        JustifyContent = Justify.FlexStart,
                        GapColumn = 16f,
                        GapRow = 16f,
                        PaddingTop = 20f,
                        PaddingBottom = 20f,
                        PaddingLeft = 20f,
                        PaddingRight = 20f,
                    },
                    Children =
                    {
                        // Section 1: Basic text bindings
                        Section(w: title, font: titleFont, color: titleColor, strokeColor: new Color(0, 0, 0, 255)),
                        Section(w: subtitle, font: bodyFont, color: subtitleColor),

                        // Section 2: Inline text with counter
                        new View()
                        {
                            Styles = new()
                            {
                                FlexDirection = FlexDirection.Row,
                                GapColumn = 4f,
                                GapRow = 4f,
                            },
                            Children =
                            {
                                Section(w: $"Counter: {counter}", font: new Font(FontFamily.Adventure, FontStyle.Bold, 18),
                                    color: new Color(255, 255, 255, 255))
                            }
                        },

                        // Section 3: Color swatches
                        new View()
                        {
                            Styles = new Styles()
                            {
                                FlexDirection = FlexDirection.Row,
                                GapColumn = 8f,
                                GapRow = 8f,
                                AlignItems = Align.Center,
                            },
                            Children =
                            {
                                new View { Styles = new() { BackgroundColor = new Color(255, 0, 0, 255), Width = 24f, Height = 24f } },
                                new View { Styles = new() { BackgroundColor = new Color(0, 255, 0, 255), Width = 24f, Height = 24f } },
                                new View { Styles = new() { BackgroundColor = new Color(0, 0, 255, 255), Width = 24f, Height = 24f } },
                                new View { Styles = new() { BackgroundColor = new Color(255, 255, 0, 255), Width = 24f, Height = 24f } },
                                Section(w: "Color swatches", font: new Font(FontFamily.Adventure, FontStyle.Plain, 18),
                                    color: new Color(180, 180, 180, 255), marginLeft: 8f)
                            }
                        },

                        // Section 4: Visibility binding
                        new View()
                        {
                            Styles = new Styles()
                            {
                                FlexDirection = FlexDirection.Row,
                                GapColumn = 8f,
                                GapRow = 8f,
                                AlignItems = Align.Center,
                            },
                            Children =
                            {
                                new View()
                                {
                                    Styles = new Styles()
                                    {
                                        BackgroundColor = new Color(0, 255, 255, 255),
                                        Width = 16f,
                                        Height = 16f,
                                        Visibility = badgeVisibility
                                    },
                                },
                                Section(w: "Badge (visibility-bound)",
                                    font: new Font(FontFamily.Adventure, FontStyle.Plain, 18),
                                    color: new Color(255, 255, 255, 255))
                            }
                        },

                        // // Section 6: Nested layout — Row of Columns
                        // Section(w: "Nested layout:", font: new Font(FontFamily.Adventure, FontStyle.Bold, 18),
                        //     color: accentColor),
                        // FlexPanel(flexDirection: FlexDirection.Row, gap: counterModulo, children:
                        // [
                        //     FlexPanel(flexDirection: FlexDirection.Column, gap: 4f, children:
                        //     [
                        //         Section(w: "Column A", font: new Font(FontFamily.Adventure, FontStyle.Bold, 14),
                        //             color: new Color(255, 200, 100, 255)),
                        //         PaintedBox(backgroundColor: new Color(170, 68, 68, 255), width: 60f, height: 20f),
                        //         PaintedBox(backgroundColor: new Color(170, 102, 102, 255), width: 60f, height: 20f),
                        //         PaintedBox(backgroundColor: new Color(170, 136, 136, 255), width: 60f, height: 20f)
                        //     ]),
                        //     FlexPanel(flexDirection: FlexDirection.Column, gap: 4f, children:
                        //     [
                        //         Section(w: "Column B", font: new Font(FontFamily.Adventure, FontStyle.Bold, 14),
                        //             color: new Color(100, 200, 255, 255)),
                        //         PaintedBox(backgroundColor: new Color(68, 68, 170, 255), width: 60f, height: 20f),
                        //         PaintedBox(backgroundColor: new Color(102, 102, 170, 255), width: 60f, height: 20f),
                        //         PaintedBox(backgroundColor: new Color(136, 136, 170, 255), width: 60f, height: 20f)
                        //     ])
                        // ]),
                        //
                        // // Section 7: Fade-in animation on mount
                        // FlexPanel(flexDirection: FlexDirection.Row, gap: 8f, alignItems: Align.Center, opacity: 1f,
                        //     children:
                        //     [
                        //         PaintedBox(backgroundColor: new Color(255, 255, 255, 255), width: 12f, height: 12f),
                        //         Section(w: "Fade-in on mount",
                        //             font: new Font(FontFamily.Adventure, FontStyle.Plain, 18),
                        //             color: new Color(255, 255, 255, 255))
                        //     ]),
                        //
                        // // Section 8: Contents pane
                        // Section(w: "Contents panel:", font: new Font(FontFamily.Adventure, FontStyle.Bold, 18),
                        //     color: accentColor),
                        // ContentsPanel([
                        //     Section(w: "Contents panel", font: new Font(FontFamily.Adventure, FontStyle.Bold, 18),
                        //         color: accentColor)
                        // ])
                    }
                },
                // ── Middle spacer ──────────────────────────────────────
                new View { Styles = new() { Flex = 1f } },
            },
        };
    }

    private static View Section(string w, Font font, Color color, Color? strokeColor = null, float marginLeft = 0f)
    {
        return new View()
        {
            Styles = new()
            {
                FlexDirection = FlexDirection.Row,
                GapRow = 4f,
                GapColumn = 4f,
                MarginLeft = marginLeft,
            },
            Children =
            {
                new TextRun()
                {
                    TextStyles = new TextStyles()
                    {
                        FontFamily = font.FontFamily,
                        FontSize = font.Size,
                        FontStyle = font.Style,
                        ForegroundColor = color,
                        StrokeColor = strokeColor,
                    },
                    Text = w
                }
            }
        };
    }
}