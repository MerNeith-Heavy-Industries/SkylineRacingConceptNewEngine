using System;
using System.Collections.Generic;
using System.Text;
using ClaySharp;
using Lua;
using nfm_world_library.Lua;

namespace NFMWorld.ClayDom;

[LuaVisible]
public partial class ClayTextElement : ClayElementBase
{
    public override NodeType NodeType => NodeType.TextElement;

    private string _text = "";

    // TODO find a good way to cache text from children
    protected override void OnChildrenChanged()
    {
    }

    internal override void LayoutSelfAndChildren()
    {
        var sb = new StringBuilder();
        if (Children is not null)
        {
            foreach (var child in Children)
            {
                if (child is ClayTextNode textNode)
                {
                    sb.Append(textNode.Text);
                }
            }
        }
        _text = sb.ToString();
        
        Clay.Text(_text, TextConfig);
    }
}