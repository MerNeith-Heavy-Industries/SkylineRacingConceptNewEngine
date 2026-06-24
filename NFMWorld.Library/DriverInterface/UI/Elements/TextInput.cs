using System.ComponentModel;
using Microsoft.Xna.Framework;
using NFMWorldLibrary;
using NFMWorldLibrary.Backend.Gamemodes;
using WorldXaml.UI.Base;
using WorldXaml.UI.Yoga;
using WorldXaml.UI.Yoga.Events;
using KeyCode = WorldXaml.UI.Yoga.Events.Key;

namespace NFMWorld.DriverInterface.UI;

/// <summary>
/// A single-line text input component. Extends <see cref="TextRun"/> for built-in text
/// layout and measurement, adds a blinking cursor, selection, and keyboard/mouse navigation.
/// </summary>
public partial class TextInput : TextRun
{
    // ── Cursor & selection state ──────────────────────────────────

    /// <summary>Character index where the cursor sits (0 = before first char).</summary>
    private int _cursorIndex;

    /// <summary>
    /// Selection anchor index. When non-null, text between <see cref="_selectionAnchor"/>
    /// and <see cref="_cursorIndex"/> is selected.
    /// </summary>
    private int? _selectionAnchor;

    /// <summary>Whether a mouse drag-selection is in progress.</summary>
    private bool _isDragging;

    // ── Cursor blink ──────────────────────────────────────────────

    /// <summary>Blink period for the cursor in milliseconds.</summary>
    private const float CursorBlinkPeriodMs = 530f;

    private float _cursorBlinkTimer;
    private bool _cursorVisible;

    // ── Styled properties ──────────────────────────────────────────

    [Property]
    public Color BorderColor { get; set; } = new(255, 255, 255, 255);

    [Property]
    public Color BackgroundColor { get; set; } = new(20, 20, 30, 255);

    [Property]
    public Color CursorColor { get; set; } = new(255, 255, 255, 255);

    [Property]
    public Color SelectionColor { get; set; } = new(100, 180, 255, 128);

    [Property]
    public Color PlaceholderColor { get; set; } = new(128, 128, 128, 255);

    [Property]
    public int BorderTopLeftRadius { get; set; }
    [Property]
    public int BorderTopRightRadius { get; set; }
    [Property]
    public int BorderBottomLeftRadius { get; set; }
    [Property]
    public int BorderBottomRightRadius { get; set; }

    /// <summary>
    /// Placeholder text shown when <see cref="TextRun.Text"/> is empty and the input is not focused.
    /// </summary>
    [Property]
    public string Placeholder { get; set; } = "";

    /// <summary>
    /// Raised when the user presses Enter. The current <see cref="TextRun.Text"/> is passed as the argument.
    /// </summary>
    [Property]
    public Action<string>? Submitted { get; set; }

    public TextInput()
    {
        IsFocusable = true;
    }

    // ── Property change handlers ───────────────────────────────────

    protected override void OnInvalidated()
    {
        var len = (Text ?? "").Length;
        if (_cursorIndex > len)
            _cursorIndex = len;
        _selectionAnchor = null;

        // Reset cursor blink so it is visible immediately after typing
        _cursorBlinkTimer = 0;
        _cursorVisible = true;
    }

    // ── Selection helpers ──────────────────────────────────────────

    /// <summary>Gets the start and end (inclusive-exclusive) of the current selection, or null.</summary>
    private (int start, int end)? GetSelectionRange()
    {
        if (_selectionAnchor is not { } anchor || anchor == _cursorIndex)
            return null;

        var start = Math.Min(anchor, _cursorIndex);
        var end = Math.Max(anchor, _cursorIndex);
        return (start, end);
    }

    /// <summary>Returns the current text, or empty string if null.</summary>
    private string CurrentText => Text ?? "";

    /// <summary>Clears any active selection.</summary>
    private void ClearSelection()
    {
        _selectionAnchor = null;
    }

    /// <summary>Deletes the selected range if any, returns true if something was deleted.</summary>
    private bool DeleteSelection()
    {
        var sel = GetSelectionRange();
        if (sel is not { } range)
            return false;

        var t = CurrentText;
        Text = t[..range.start] + t[range.end..];
        _cursorIndex = range.start;
        ClearSelection();
        return true;
    }

    // ── Cursor position from character index ───────────────────────

    /// <summary>
    /// Returns the x-offset (in content space) for the given character index.
    /// Uses <see cref="TextRun.LaidOutComplexText"/> for accurate measurement.
    /// </summary>
    [ClientOnly]
    private float GetCursorXForCharIndex(int charIndex)
    {
        var laidOut = LaidOutComplexText;
        if (laidOut is not { } container || container.Elements.Count == 0)
            return 0;

        var targetIdx = Math.Clamp(charIndex, 0, CurrentText.Length);
        var accumIdx = 0;

        foreach (var elem in container.Elements)
        {
            var elemLen = elem.Text.Length;
            if (accumIdx + elemLen <= targetIdx)
            {
                accumIdx += elemLen;
                continue;
            }

            // Cursor is within this element
            var charsBefore = targetIdx - accumIdx;
            if (charsBefore <= 0)
                return elem.Position.X;

            var fontMetrics = G.GetFontMetrics(elem.Font);
            var measured = fontMetrics.MeasureText(elem.Text[..charsBefore]);
            return elem.Position.X + measured.X;
        }

        // Cursor is at the very end — after the last element
        if (container.Elements.Count > 0)
        {
            var last = container.Elements[^1];
            var fontMetrics = G.GetFontMetrics(last.Font);
            var measured = fontMetrics.MeasureText(last.Text);
            return last.Position.X + measured.X;
        }

        return 0;
    }

    /// <summary>
    /// Returns the character index closest to a given x-offset (in content space).
    /// </summary>
    [ClientOnly]
    private int GetCharIndexForCursorX(float cursorX)
    {
        var laidOut = LaidOutComplexText;
        if (laidOut is not { } container || container.Elements.Count == 0)
            return 0;

        var accumIdx = 0;

        foreach (var elem in container.Elements)
        {
            var elemStartX = elem.Position.X;
            var fontMetrics = G.GetFontMetrics(elem.Font);
            var elemWidth = fontMetrics.MeasureText(elem.Text).X;

            if (cursorX < elemStartX)
                return accumIdx;

            if (cursorX >= elemStartX && cursorX <= elemStartX + elemWidth)
            {
                // Clicked within this element — find closest character
                var relX = cursorX - elemStartX;
                var bestIdx = 0;
                var bestDist = float.MaxValue;
                for (var i = 0; i <= elem.Text.Length; i++)
                {
                    var charX = fontMetrics.MeasureText(elem.Text.AsSpan(..i)).X;
                    var dist = Math.Abs(charX - relX);
                    if (dist < bestDist)
                    {
                        bestDist = dist;
                        bestIdx = i;
                    }
                }
                return accumIdx + bestIdx;
            }

            accumIdx += elem.Text.Length;
        }

        // Clicked past all elements — cursor at end
        return accumIdx;
    }

    // ── Keyboard input ─────────────────────────────────────────────

    protected override void OnKeyTyped(FocusManager focusManager, KeyboardTypingEvent @event)
    {
        base.OnKeyTyped(focusManager, @event);

        var c = @event.KeyChar;

        // Backspace
        if (c == '\b')
        {
            if (DeleteSelection())
                return;

            if (_cursorIndex > 0)
            {
                var t = CurrentText;
                Text = t[..(_cursorIndex - 1)] + t[_cursorIndex..];
                _cursorIndex--;
            }
            return;
        }

        // Enter / Return — submit
        if (c is '\r' or '\n')
        {
            Submitted?.Invoke(Text ?? "");
            return;
        }

        // Ignore other control characters
        if (char.IsControl(c))
            return;

        // Replace selection or insert at cursor
        DeleteSelection();
        var t2 = CurrentText;
        Text = t2[.._cursorIndex] + c + t2[_cursorIndex..];
        _cursorIndex++;
    }

    public override void OnKeyPressed(FocusManager focusManager, KeyboardEvent @event)
    {
        base.OnKeyPressed(focusManager, @event);

        var shift = @event.Keys.ShiftKey;
        var ctrl = @event.Keys.ControlKey;
        var key = @event.KeyCode;

        switch (key)
        {
            case KeyCode.Left:
                HandleArrow(-1, shift, ctrl);
                break;
            case KeyCode.Right:
                HandleArrow(+1, shift, ctrl);
                break;
            case KeyCode.Home:
                HandleHome(shift);
                break;
            case KeyCode.End:
                HandleEnd(shift);
                break;
            case KeyCode.Delete:
                HandleDelete();
                break;
        }
    }

    private void HandleArrow(int direction, bool shift, bool ctrl)
    {
        var len = CurrentText.Length;

        if (ctrl)
        {
            // Word-skip: move to next/previous word boundary
            var target = _cursorIndex;
            if (direction < 0)
            {
                while (target > 0 && char.IsWhiteSpace(CurrentText[target - 1]))
                    target--;
                while (target > 0 && !char.IsWhiteSpace(CurrentText[target - 1]))
                    target--;
            }
            else
            {
                while (target < len && !char.IsWhiteSpace(CurrentText[target]))
                    target++;
                while (target < len && char.IsWhiteSpace(CurrentText[target]))
                    target++;
            }
            MoveCursorTo(target, shift);
        }
        else
        {
            MoveCursorTo(_cursorIndex + direction, shift);
        }
    }

    private void HandleHome(bool shift)
    {
        MoveCursorTo(0, shift);
    }

    private void HandleEnd(bool shift)
    {
        MoveCursorTo(CurrentText.Length, shift);
    }

    private void HandleDelete()
    {
        if (DeleteSelection())
            return;

        var t = CurrentText;
        if (_cursorIndex < t.Length)
        {
            Text = t[.._cursorIndex] + t[(_cursorIndex + 1)..];
        }
    }

    private void MoveCursorTo(int index, bool extendSelection)
    {
        index = Math.Clamp(index, 0, CurrentText.Length);

        if (extendSelection)
        {
            _selectionAnchor ??= _cursorIndex;
        }
        else
        {
            ClearSelection();
        }

        _cursorIndex = index;

        // Reset blink so the cursor is visible immediately
        _cursorBlinkTimer = 0;
        _cursorVisible = true;
    }

    // ── Mouse input ────────────────────────────────────────────────

    [ClientOnly]
    protected override void OnMousePressed(FocusManager focusManager, MouseEvent @event)
    {
        base.OnMousePressed(focusManager, @event);

        if (@event.Button != MouseButton.Primary)
            return;

        var shift = @event.ShiftKey;
        // RelativePosition is padding-box-relative; convert to content-box-relative X
        var charIdx = GetCharIndexForCursorX(@event.RelativePosition.X - LayoutPaddingLeft);
        MoveCursorTo(charIdx, shift);
        _isDragging = true;
    }

    protected override void OnMouseReleased(FocusManager focusManager, MouseEvent @event)
    {
        base.OnMouseReleased(focusManager, @event);
        _isDragging = false;
    }

    [ClientOnly]
    protected override void OnMouseDragged(FocusManager focusManager, MouseDragEvent @event)
    {
        base.OnMouseDragged(focusManager, @event);

        if (!_isDragging)
            return;

        // RelativePosition is padding-box-relative; convert to content-box-relative X
        var charIdx = GetCharIndexForCursorX(@event.RelativePosition.X - LayoutPaddingLeft);
        // Always extend selection during drag
        _selectionAnchor ??= _cursorIndex;
        _cursorIndex = Math.Clamp(charIdx, 0, CurrentText.Length);
        _cursorBlinkTimer = 0;
        _cursorVisible = true;
    }

    // ── Cursor blink ───────────────────────────────────────────────

    protected override void GameTick()
    {
        base.GameTick();

        _cursorBlinkTimer += 1000f / Physics.TargetTps; // approximate per-frame delta
        if (_cursorBlinkTimer >= CursorBlinkPeriodMs)
        {
            _cursorBlinkTimer -= CursorBlinkPeriodMs;
            _cursorVisible = !_cursorVisible;
        }
    }

    // ── Rendering ──────────────────────────────────────────────────

    [ClientOnly]
    protected override void RenderBackground(Vector2 position, Vector2 size)
    {
        G.SetColor(BackgroundColor);
        var radTopLeft = Math.Max(0, BorderTopLeftRadius - ((BorderTop ?? 0) + (BorderLeft ?? 0) / 2f));
        var radTopRight = Math.Max(0, BorderTopRightRadius - ((BorderTop ?? 0) + (BorderRight ?? 0) / 2f));
        var radBottomRight = Math.Max(0, BorderBottomRightRadius - ((BorderBottom ?? 0) + (BorderRight ?? 0) / 2f));
        var radBottomLeft = Math.Max(0, BorderBottomLeftRadius - ((BorderBottom ?? 0) + (BorderLeft ?? 0) / 2f));
        G.FillRoundedRect(
            (int)position.X, (int)position.Y,
            (int)size.X, (int)size.Y,
            radTopLeft * G.Scale, radTopRight * G.Scale,
            radBottomRight * G.Scale, radBottomLeft * G.Scale);
    }

    [ClientOnly]
    protected override void RenderBorder(Vector2 position, Vector2 size)
    {
        G.SetColor(IsFocused ? new Color(100, 180, 255, 255) : BorderColor);
        var avgBorder = (BorderTop ?? 0) + (BorderLeft ?? 0) + (BorderBottom ?? 0) + (BorderRight ?? 0) / 4f;
        G.SetStrokeWidth(avgBorder > 0 ? avgBorder : 2f * G.Scale);
        var radTopLeft = BorderTopLeftRadius;
        var radTopRight = BorderTopRightRadius;
        var radBottomRight = BorderBottomRightRadius;
        var radBottomLeft = BorderBottomLeftRadius;
        G.DrawRoundedRect(
            (int)(position.X + avgBorder / 2), (int)(position.Y + avgBorder / 2),
            (int)size.X, (int)size.Y,
            radTopLeft * G.Scale, radTopRight * G.Scale,
            radBottomRight * G.Scale, radBottomLeft * G.Scale);
        G.SetStrokeWidth();
    }

    [ClientOnly]
    protected override void RenderContent(Vector2 position, Vector2 size)
    {
        // Draw placeholder when empty and not focused
        if (string.IsNullOrEmpty(Text) && !string.IsNullOrEmpty(Placeholder) && !IsFocused)
        {
            G.SetFont(Font);
            G.SetColor(PlaceholderColor);
            G.DrawString(Placeholder, (int)position.X, (int)position.Y);
            return;
        }

        // Delegate text rendering to TextRun's RenderContent
        base.RenderContent(position, size);

        if (!IsFocused)
            return;

        var baseX = position.X;
        var contentTop = position.Y;
        var contentBottom = position.Y + size.Y;

        // ── Draw selection highlight ────────────────────────────
        var sel = GetSelectionRange();
        if (sel is { } range)
        {
            var selStartX = GetCursorXForCharIndex(range.start);
            var selEndX = GetCursorXForCharIndex(range.end);

            G.SetColor(SelectionColor);
            G.FillRect(
                (int)(baseX + selStartX), (int)contentTop,
                (int)(selEndX - selStartX), (int)(contentBottom - contentTop));
        }

        // ── Draw cursor ─────────────────────────────────────────
        if (_cursorVisible)
        {
            var cursorX = GetCursorXForCharIndex(_cursorIndex);

            G.SetColor(CursorColor);
            var cursorTop = contentTop + 2;
            var cursorBottom = contentBottom - 4;
            G.DrawLine(
                (int)(baseX + cursorX), (int)cursorTop,
                (int)(baseX + cursorX), (int)cursorBottom);
        }
    }
}
