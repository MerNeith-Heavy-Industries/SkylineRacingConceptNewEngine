using NFMWorld.DriverInterface.DriverInterface;
using NFMWorld.Reactor;
using NFMWorldLibrary;

namespace NFMWorld.UI;

/// <summary>
/// Builds the Yoga UI test scene: a scrollable list (overflow: scroll with a
/// scrollbar), a nested overflow:hidden clip, and interactive rows that
/// demonstrate hover/press hit-testing.
/// </summary>
public static class TestView
{
    public static View Render(ref int counter)
    {
        return new View()
        {
            Name = "YogaTest",
            Styles = new()
            {
                FlexDirection = FlexDirection.Column,
                AlignItems = Align.FlexStart,
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
                Section($"Counter: {counter} — scroll the list and click rows",
                    font: new Font(FontFamily.Adventure, FontStyle.Bold, 18),
                    color: new Color(255, 255, 255, 255)),

                ScrollList(),

                Section("Nested overflow: hidden (child is wider than the box)",
                    font: new Font(FontFamily.Adventure, FontStyle.Plain, 16),
                    color: new Color(180, 180, 180, 255)),

                new View()
                {
                    Name = "ClippedBox",
                    Styles = new()
                    {
                        Width = 200f,
                        Height = 80f,
                        Overflow = Overflow.Hidden,
                        BackgroundColor = new Color(30, 40, 60, 255),
                        BorderColor = new Color(100, 200, 255, 255),
                        BorderTop = 2f,
                        BorderBottom = 2f,
                        BorderLeft = 2f,
                        BorderRight = 2f,
                        BorderTopLeftRadius = 8f,
                        BorderTopRightRadius = 8f,
                        BorderBottomLeftRadius = 8f,
                        BorderBottomRightRadius = 8f,
                    },
                    Children =
                    {
                        new View()
                        {
                            Styles = new()
                            {
                                Width = 300f,
                                Height = 40f,
                                BackgroundColor = new Color(255, 100, 100, 255),
                            },
                        }
                    }
                }
            }
        };
    }

    private static View ScrollList()
    {
        var list = new View()
        {
            Name = "ScrollList",
            Styles = new()
            {
                FlexDirection = FlexDirection.Column,
                AlignItems = Align.Stretch,
                Width = 360f,
                Height = 260f,
                Overflow = Overflow.Scroll,
                BackgroundColor = new Color(20, 20, 30, 255),
                BorderColor = new Color(230, 128, 26, 255),
                BorderTop = 2f,
                BorderBottom = 2f,
                BorderLeft = 2f,
                BorderRight = 2f,
                BorderTopLeftRadius = 8f,
                BorderTopRightRadius = 8f,
                BorderBottomLeftRadius = 8f,
                BorderBottomRightRadius = 8f,
                PaddingTop = 4f,
                PaddingBottom = 4f,
            },
        };

        for (int i = 0; i < 100; i++)
        {
            list.Children.Add(Row($"Row {i}"));
        }

        return list;
    }

    private static View Row(string text)
    {
        var row = new View()
        {
            Name = text,
            IsFocusable = true,
            Styles = new()
            {
                FlexDirection = FlexDirection.Row,
                AlignItems = Align.Center,
                Height = 36f,
                MinHeight = 36f,
                MaxHeight = 36f,
                PaddingLeft = 12f,
                PaddingRight = 12f,
                BackgroundColor = new Color(45, 45, 60, 255),
            },
            Children =
            {
                new TextRun()
                {
                    TextStyles = new TextStyles()
                    {
                        FontFamily = FontFamily.Adventure,
                        FontSize = 16f,
                        ForegroundColor = new Color(220, 220, 220, 255),
                    },
                    Text = text,
                }
            }
        };

        row.MouseEntered = _ => row.Styles = row.Styles with { BackgroundColor = new Color(70, 70, 95, 255) };
        row.MouseLeft = _ => row.Styles = row.Styles with { BackgroundColor = new Color(45, 45, 60, 255) };
        row.MousePressed = e => Logging.Info($"[YogaTest] pressed '{text}' rel=({e.RelativePosition.X:F0},{e.RelativePosition.Y:F0})");

        return row;
    }

    private static View Section(string w, Font font, Color color)
    {
        return new View()
        {
            Styles = new()
            {
                FlexDirection = FlexDirection.Row,
                GapRow = 4f,
                GapColumn = 4f,
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
                    },
                    Text = w
                }
            }
        };
    }
}
