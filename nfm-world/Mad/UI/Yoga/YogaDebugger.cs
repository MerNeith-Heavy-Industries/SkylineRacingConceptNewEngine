using NFMWorld;
using NFMWorld.DriverInterface;
using NFMWorld.Reactor;

namespace WorldXaml.UI.Yoga;

public static class YogaDebugger
{
    private static Vector2 _mousePosition;

    public const int MaxPages = 2;

    public static void Render(int page = 0)
    {
        if (page == 0)
            RenderPage1();
        else if (page == 1)
            RenderPage2();
        else if (page == 2)
            RenderPage3();

        // draw two lines intersecting the mouse position
        G.SetColor(Color.Magenta);
        G.DrawLine(0, (int)_mousePosition.Y, (int)G.Viewport.X, (int)_mousePosition.Y);
        G.DrawLine((int)_mousePosition.X, 0, (int)_mousePosition.X, (int)G.Viewport.Y);
        // draw mouse position text
        G.SetFont(new Font(FontFamily.RobotoMono, FontStyle.Plain, 16));
        var mousePosText = $"Mouse: ({(int)_mousePosition.X}, {(int)_mousePosition.Y})";
        G.SetColor(Color.White);
        G.DrawStringStroke(mousePosText, (int)_mousePosition.X + 12, (int)_mousePosition.Y + 12);
        G.SetColor(Color.Magenta);
        G.DrawString(mousePosText, (int)_mousePosition.X + 12, (int)_mousePosition.Y + 12);
    }

    // ── Helper: get the deepest NativeVisual that is a Node (for layout queries) ──

    private static Node? GetLayoutNode(ReactorDebugNode debugNode)
        => debugNode.NativeVisual as Node;

    private static Node? GetLayoutNode(ReactorDebugNode[] chain)
    {
        // Walk the chain from leaf to root to find the deepest Node with layout.
        for (int i = chain.Length - 1; i >= 0; i--)
        {
            if (chain[i].NativeVisual is Node n)
                return n;
        }
        return null;
    }

    // ── Page 2: full VDOM tree with layout info text ────────────────────

    private static void RenderPage3()
    {
        var roots = NodeDebugger.VDomRoots;
        if (roots.Count == 0) return;

        var maxDepth = 0;
        foreach (var root in roots)
            maxDepth = Math.Max(maxDepth, GetReactorMaxDepth(root, 0));

        var y = 24;
        foreach (var root in roots)
        {
            DrawReactorElementAndChildren(root, ref y, 0, maxDepth);
        }

        return;

        static int GetReactorMaxDepth(ReactorDebugNode node, int depth)
        {
            var childMax = depth;
            foreach (var child in node.Children)
                childMax = Math.Max(childMax, GetReactorMaxDepth(child, depth + 1));
            return childMax;
        }

        void DrawReactorElementAndChildren(ReactorDebugNode node, ref int y, int depth, int maxDepth)
        {
            var color = maxDepth > 0
                ? Color.Lerp(Color.Red, Color.Yellow, depth / (float)maxDepth)
                : Color.Red;

            // Draw rect for VisualVNodes (which have a distinct native visual)
            if (node.Type == ReactorDebugNodeType.VisualVNode && node.NativeVisual is Node layoutNode)
            {
                G.SetColor(color);
                G.DrawRect(
                    (int)layoutNode.LayoutBorderPosition.X,
                    (int)layoutNode.LayoutBorderPosition.Y,
                    (int)layoutNode.LayoutBorderSize.X,
                    (int)layoutNode.LayoutBorderSize.Y
                );
            }

            // Build layout info string
            var display = node.ToDisplayString();
            string layoutInfo;
            if (node.NativeVisual is Node n)
            {
                layoutInfo = $"{display}  {n.LayoutBorderSize.X}px x {n.LayoutBorderSize.Y}px at ({n.LayoutBorderPosition.X}, {n.LayoutBorderPosition.Y})";
            }
            else
            {
                layoutInfo = display;
            }

            G.SetFont(new Font(FontFamily.RobotoMono, FontStyle.Plain, 22));
            var indent = new string(' ', depth * 2);
            G.SetColor(Color.White);
            G.DrawStringStroke(indent + layoutInfo, 12, y);
            G.SetColor(color);
            G.DrawString(indent + layoutInfo, 12, y);
            y += 24;

            foreach (var child in node.Children)
                DrawReactorElementAndChildren(child, ref y, depth + 1, maxDepth);
        }
    }

    // ── Page 1: full VDOM tree with depth-colored outlines ──────────────

    private static void RenderPage2()
    {
        var roots = NodeDebugger.VDomRoots;
        if (roots.Count == 0) return;

        var maxDepth = 0;
        foreach (var root in roots)
            maxDepth = Math.Max(maxDepth, GetReactorMaxDepth(root, 0));

        foreach (var root in roots)
            DrawReactorNodeAndChildren(root, 0, maxDepth);

        return;

        static int GetReactorMaxDepth(ReactorDebugNode node, int depth)
        {
            var childMax = depth;
            foreach (var child in node.Children)
                childMax = Math.Max(childMax, GetReactorMaxDepth(child, depth + 1));
            return childMax;
        }

        void DrawReactorNodeAndChildren(ReactorDebugNode node, int depth, int maxDepth)
        {
            var color = maxDepth > 0
                ? Color.Lerp(Color.Red, Color.Yellow, depth / (float)maxDepth)
                : Color.Red;

            if (node.NativeVisual is Node layoutNode)
            {
                G.SetColor(color);
                G.DrawRect(
                    (int)layoutNode.LayoutBorderPosition.X,
                    (int)layoutNode.LayoutBorderPosition.Y,
                    (int)layoutNode.LayoutBorderSize.X,
                    (int)layoutNode.LayoutBorderSize.Y
                );

                G.SetFont(new Font(FontFamily.RobotoMono, FontStyle.Plain, 20));
                var info = node.ToDisplayString();
                G.SetColor(Color.White);
                G.DrawStringStroke(info, (int)layoutNode.LayoutBorderPosition.X, (int)layoutNode.LayoutBorderPosition.Y - 12);
                G.SetColor(color);
                G.DrawString(info, (int)layoutNode.LayoutBorderPosition.X, (int)layoutNode.LayoutBorderPosition.Y - 12);
            }

            foreach (var child in node.Children)
                DrawReactorNodeAndChildren(child, depth + 1, maxDepth);
        }
    }

    // ── Page 0: mouse-over chain with VDOM info and box model ───────────

    private static void RenderPage1()
    {
        var roots = NodeDebugger.VDomRoots;
        if (roots.Count == 0) return;

        var mouseOverChain = roots
            .Select(FindMouseOverReactorNodeTree)
            .MaxBy(c => c.Length);

        if (mouseOverChain is not { Length: > 0 }) return;

        // Draw rects for each node in the chain (only VisualVNodes)
        for (int i = 0; i < mouseOverChain.Length; i++)
        {
            var node = mouseOverChain[i];
            if (node.NativeVisual is not Node layoutNode) continue;

            var color = Color.Lerp(Color.Red, Color.Yellow, i / (float)mouseOverChain.Length);
            G.SetColor(color);
            G.DrawRect(
                (int)layoutNode.LayoutBorderPosition.X,
                (int)layoutNode.LayoutBorderPosition.Y,
                (int)layoutNode.LayoutBorderSize.X,
                (int)layoutNode.LayoutBorderSize.Y
            );
        }

        // Draw type/input info for each node in the chain
        for (int i = 0; i < mouseOverChain.Length; i++)
        {
            var node = mouseOverChain[i];
            var color = Color.Lerp(Color.Red, Color.Yellow, i / (float)mouseOverChain.Length);

            G.SetFont(new Font(FontFamily.RobotoMono, FontStyle.Plain, 18));
            var info = node.ToDisplayString();
            var prefix = new string(' ', i * 2);

            G.SetColor(Color.White);
            G.DrawStringStroke(prefix + info, 12, 24 + (i * 24));
            G.SetColor(color);
            G.DrawString(prefix + info, 12, 24 + (i * 24));
        }

        // Box model visualization for the deepest node with a Node visual
        var lastNode = GetLayoutNode(mouseOverChain);
        if (lastNode is not null)
        {
            G.SetFont(new Font(FontFamily.RobotoMono, FontStyle.Plain, 16));
            var layoutInfo = $"""
                              Layout:
                              Margin: {lastNode.LayoutMarginSize.X}px x {lastNode.LayoutMarginSize.Y}px at ({lastNode.LayoutMarginPosition.X}, {lastNode.LayoutMarginPosition.Y})
                              Border: {lastNode.LayoutBorderSize.X}px x {lastNode.LayoutBorderSize.Y}px at ({lastNode.LayoutBorderPosition.X}, {lastNode.LayoutBorderPosition.Y})
                              Padding: {lastNode.LayoutPaddingSize.X}px x {lastNode.LayoutPaddingSize.Y}px at ({lastNode.LayoutPaddingPosition.X}, {lastNode.LayoutPaddingPosition.Y})
                              Content: {lastNode.LayoutContentSize.X}px x {lastNode.LayoutContentSize.Y}px at ({lastNode.LayoutContentPosition.X}, {lastNode.LayoutContentPosition.Y})
                              """;
            G.SetColor(Color.White);
            G.DrawStringStroke(layoutInfo, 12, 24 + (mouseOverChain.Length * 24));
            G.SetColor(Color.Cyan);
            G.DrawString(layoutInfo, 12, 24 + (mouseOverChain.Length * 24));

            // draw margin, padding, border, content
            G.SetColor(Color.Gray with { A = 128 });
            FillRectExceptForRect(
                new RectangleF(
                    lastNode.LayoutMarginPosition.X,
                    lastNode.LayoutMarginPosition.Y,
                    lastNode.LayoutMarginSize.X,
                    lastNode.LayoutMarginSize.Y
                ),
                new RectangleF(
                    lastNode.LayoutBorderPosition.X,
                    lastNode.LayoutBorderPosition.Y,
                    lastNode.LayoutBorderSize.X,
                    lastNode.LayoutBorderSize.Y
                )
            );
            G.SetColor(Color.Yellow with { A = 128 });
            FillRectExceptForRect(
                new RectangleF(
                    lastNode.LayoutBorderPosition.X,
                    lastNode.LayoutBorderPosition.Y,
                    lastNode.LayoutBorderSize.X,
                    lastNode.LayoutBorderSize.Y
                ),
                new RectangleF(
                    lastNode.LayoutPaddingPosition.X,
                    lastNode.LayoutPaddingPosition.Y,
                    lastNode.LayoutPaddingSize.X,
                    lastNode.LayoutPaddingSize.Y
                )
            );
            G.SetColor(Color.Green with { A = 128 });
            FillRectExceptForRect(
                new RectangleF(
                    lastNode.LayoutPaddingPosition.X,
                    lastNode.LayoutPaddingPosition.Y,
                    lastNode.LayoutPaddingSize.X,
                    lastNode.LayoutPaddingSize.Y
                ),
                new RectangleF(
                    lastNode.LayoutContentPosition.X,
                    lastNode.LayoutContentPosition.Y,
                    lastNode.LayoutContentSize.X,
                    lastNode.LayoutContentSize.Y
                )
            );
            G.SetColor(Color.Blue with { A = 128 });
            G.FillRect(
                (int)lastNode.LayoutContentPosition.X,
                (int)lastNode.LayoutContentPosition.Y,
                (int)lastNode.LayoutContentSize.X,
                (int)lastNode.LayoutContentSize.Y
            );

            // draw layout box in the corner with labels
            const int scale = 48;
            var margin = new RectangleF(
                (int)G.Viewport.X - 420,
                12,
                416,
                416
            );
            var border = new RectangleF(
                margin.X + scale,
                margin.Y + scale,
                margin.Width - (scale * 2),
                margin.Height - (scale * 2)
            );
            var padding = new RectangleF(
                border.X + scale,
                border.Y + scale,
                border.Width - (scale * 2),
                border.Height - (scale * 2)
            );
            var content = new RectangleF(
                padding.X + scale,
                padding.Y + scale,
                padding.Width - (scale * 2),
                padding.Height - (scale * 2)
            );

            DrawBoxWithLabels(
                Color.Gray with { A = 128 },
                "margin",
                margin, border,
                lastNode.LayoutMarginTop,
                lastNode.LayoutMarginRight,
                lastNode.LayoutMarginBottom,
                lastNode.LayoutMarginLeft
            );
            DrawBoxWithLabels(
                Color.Yellow with { A = 128 },
                "border",
                border, padding,
                lastNode.LayoutBorderTop,
                lastNode.LayoutBorderRight,
                lastNode.LayoutBorderBottom,
                lastNode.LayoutBorderLeft
            );
            DrawBoxWithLabels(
                Color.Green with { A = 128 },
                "padding",
                padding, content,
                lastNode.LayoutPaddingTop,
                lastNode.LayoutPaddingRight,
                lastNode.LayoutPaddingBottom,
                lastNode.LayoutPaddingLeft
            );

            // draw centred widthxheight in middle box
            G.SetColor(Color.Blue with { A = 128 });
            G.FillRect((int)content.X, (int)content.Y, (int)content.Width, (int)content.Height);

            G.SetFont(new Font(FontFamily.RobotoMono, FontStyle.Plain, 16));
            G.SetColor(Color.Black);
            G.DrawStringStroke("content", (int)content.X, (int)content.Y + 16);
            G.SetColor(Color.White);
            G.DrawString("content", (int)content.X, (int)content.Y + 16);

            G.SetFont(new Font(FontFamily.RobotoMono, FontStyle.Plain, 16));
            var contentSizeText = $"{(int)lastNode.LayoutContentSize.X}px x {(int)lastNode.LayoutContentSize.Y}px";
            G.SetColor(Color.Black);
            G.DrawStringStrokeAligned(
                contentSizeText,
                (int)content.X,
                (int)content.Y,
                (int)content.Width,
                (int)content.Height,
                TextHorizontalAlignment.Center,
                TextVerticalAlignment.Center
            );
            G.SetColor(Color.White);
            G.DrawStringAligned(
                contentSizeText,
                (int)content.X,
                (int)content.Y,
                (int)content.Width,
                (int)content.Height,
                TextHorizontalAlignment.Center,
                TextVerticalAlignment.Center
            );
        }
    }

    private static void DrawBoxWithLabels(Color color, string label, RectangleF area, RectangleF? inner, float top, float right, float bottom, float left)
    {
        G.SetColor(color);
        if (inner is {} innerRect)
        {
            FillRectExceptForRect(area, innerRect);
        }
        else
        {
            G.FillRect((int)area.X, (int)area.Y, (int)area.Width, (int)area.Height);
        }
        
        G.SetFont(new Font(FontFamily.RobotoMono, FontStyle.Plain, 16));
        G.SetColor(Color.Black);
        G.DrawStringStroke(label, (int)area.X, (int)area.Y + 16);
        G.SetColor(Color.White);
        G.DrawString(label, (int)area.X, (int)area.Y + 16);

        // Top
        G.SetColor(Color.Black);
        G.DrawStringStrokeAligned($"{top:0.00}px", (int)area.X, (int)area.Y, (int)area.Width, (int)area.Height, TextHorizontalAlignment.Center);
        G.SetColor(Color.White);
        G.DrawStringAligned($"{top:0.00}px", (int)area.X, (int)area.Y, (int)area.Width, (int)area.Height, TextHorizontalAlignment.Center);

        // Right
        G.SetColor(Color.Black);
        G.DrawStringStrokeAligned($"{right:0.00}px", (int)area.X, (int)area.Y, (int)area.Width, (int)area.Height, TextHorizontalAlignment.Right, TextVerticalAlignment.Center);
        G.SetColor(Color.White);
        G.DrawStringAligned($"{right:0.00}px", (int)area.X, (int)area.Y, (int)area.Width, (int)area.Height, TextHorizontalAlignment.Right, TextVerticalAlignment.Center);

        // Bottom
        G.SetColor(Color.Black);
        G.DrawStringStrokeAligned($"{bottom:0.00}px", (int)area.X, (int)area.Y, (int)area.Width, (int)area.Height, TextHorizontalAlignment.Center, TextVerticalAlignment.Bottom);
        G.SetColor(Color.White);
        G.DrawStringAligned($"{bottom:0.00}px", (int)area.X, (int)area.Y, (int)area.Width, (int)area.Height, TextHorizontalAlignment.Center, TextVerticalAlignment.Bottom);

        // Left
        G.SetColor(Color.Black);
        G.DrawStringStrokeAligned($"{left:0.00}px", (int)area.X, (int)area.Y, (int)area.Width, (int)area.Height, TextHorizontalAlignment.Left, TextVerticalAlignment.Center);
        G.SetColor(Color.White);
        G.DrawStringAligned($"{left:0.00}px", (int)area.X, (int)area.Y, (int)area.Width, (int)area.Height, TextHorizontalAlignment.Left, TextVerticalAlignment.Center);
    }

    private static void FillRectExceptForRect(RectangleF outer, RectangleF inner)
    {
        // Top
        G.FillRect(
            (int)outer.X,
            (int)outer.Y,
            (int)outer.Width,
            (int)(inner.Y - outer.Y)
        );
        // Bottom
        G.FillRect(
            (int)outer.X,
            (int)(inner.Y + inner.Height),
            (int)outer.Width,
            (int)(outer.Y + outer.Height - (inner.Y + inner.Height))
        );
        // Left
        G.FillRect(
            (int)outer.X,
            (int)inner.Y,
            (int)(inner.X - outer.X),
            (int)inner.Height
        );
        // Right
        G.FillRect(
            (int)(inner.X + inner.Width),
            (int)inner.Y,
            (int)(outer.X + outer.Width - (inner.X + inner.Width)),
            (int)inner.Height
        );
    }

    // ── Mouse-over traversal (VDOM tree) ──────────────────────────────────

    private static ReactorDebugNode[] FindMouseOverReactorNodeTree(ReactorDebugNode node)
    {
        // Depth-first search for the node whose margin box contains the mouse position.
        if (node.NativeVisual is Node layoutNode)
        {
            var rect = new RectangleF(
                layoutNode.LayoutMarginPosition.X,
                layoutNode.LayoutMarginPosition.Y,
                layoutNode.LayoutMarginSize.X,
                layoutNode.LayoutMarginSize.Y
            );
            if (rect.Contains(_mousePosition))
            {
                foreach (var child in node.Children)
                {
                    var childResult = FindMouseOverReactorNodeTree(child);
                    if (childResult.Length > 0)
                        return layoutNode.DebugIsContentfulNode
                            ? [node, ..childResult]
                            : childResult;
                }

                return layoutNode.DebugIsContentfulNode ? [node] : [];
            }
        }

        // Component nodes or nodes without layout: check children anyway
        foreach (var child in node.Children)
        {
            var childResult = FindMouseOverReactorNodeTree(child);
            if (childResult.Length > 0)
                return [node, ..childResult];
        }

        return [];
    }

    public static void MouseMove(int x, int y)
    {
        _mousePosition = new Vector2(x, y);
    }
}