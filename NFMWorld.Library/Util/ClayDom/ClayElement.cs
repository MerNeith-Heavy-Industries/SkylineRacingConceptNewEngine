using System.Globalization;
using System.Text;
using ClaySharp;
using Lua;
using nfm_world_library.Lua;
using NFMWorld.ClayDom.Events;

namespace NFMWorld.ClayDom;

[LuaVisible]
public partial class ClayElement : ClayElementBase
{
    public override NodeType NodeType => NodeType.Element;

    [LuaName]
    public ClayElement()
    {
    }

    // ---------------- Parsers ----------------

    internal override void LayoutSelfAndChildren()
    {
        var scope = Clay.Element(ElementId, ElementDeclaration);

        if (Children is not null)
        {
            StringBuilder? sb = null;

            foreach (var child in Children)
            {
                if (child is ClayElement element)
                {
                    // Allow text embedded directly in ClayElement without ClayTextElement, like HTML
                    if (sb?.Length > 0)
                    {
                        Clay.Text(sb.ToString(), TextConfig);
                        sb.Clear();
                    }

                    element.LayoutSelfAndChildren();
                }
                else if (child is ClayTextNode textNode)
                {
                    (sb ??= new StringBuilder()).Append(textNode.Text);
                }
            }
        }

        scope.Close(); // not `using` as that is technically slightly slower
    }

    public Clay.RenderCommandArray DoLayout(float deltaTime)
    {
        Clay.BeginLayout();

        LayoutSelfAndChildren();

        Clay.RenderCommandArray renderCommands = Clay.EndLayout(deltaTime);
        return renderCommands;
    }
}