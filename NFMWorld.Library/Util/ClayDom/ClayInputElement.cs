using System.Text;
using ClaySharp;
using ClaySharp.Plugin.TextInput;

namespace NFMWorld.ClayDom;

public class ClayInputElement : ClayElementBase
{
    public override NodeType NodeType { get; }

    public ClayTextInput.TextInputConfig TextInputConfig;
    
    public Action<Vector2>? MouseEnter;
    public Action<Vector2>? MouseLeave;

    public Action<Vector2>? MouseDown;
    public Action<Vector2>? MouseUp;

    private bool _isHovered;

    private ClayTextInput.TextInputState state;

    internal override void LayoutSelfAndChildren()
    {
        ClayTextInput.TextInput(ElementId, state, TextInputConfig);

        // ---- Hover ----
        if (Clay.PointerOver(ElementId))
        {
            var data = Clay.GetPointerState();
            if (data.State == Clay.PointerDataInteractionState.PressedThisFrame)
            {
                MouseDown?.Invoke(data.Position);
            }
            else if (data.State == Clay.PointerDataInteractionState.ReleasedThisFrame)
            {
                MouseUp?.Invoke(data.Position);
            }

            if (!_isHovered)
            {
                _isHovered = true;
                MouseEnter?.Invoke(data.Position);
            }
        }
        else
        {
            var data = Clay.GetPointerState();
            if (_isHovered)
            {
                _isHovered = false;
                MouseLeave?.Invoke(data.Position);
            }
        }
    }
}