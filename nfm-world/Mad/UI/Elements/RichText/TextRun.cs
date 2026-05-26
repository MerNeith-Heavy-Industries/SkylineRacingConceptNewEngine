using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Avalonia;
using Avalonia.Controls.Documents;
using Avalonia.Data;
using Avalonia.LogicalTree;
using Avalonia.Metadata;
using JetBrains.Annotations;
using NFMWorld.DriverInterface;
using NFMWorld.UI;
using NFMWorld.Util;
using WorldXaml.UI.Base;
using WorldXaml.UI.Yoga;

namespace Avalonia.Controls.Documents
{
    [WhitespaceSignificantCollection]
    public class InlineCollection(ILogical parent) : ObservableCollection<Inline>, IReadOnlyList<IInline>
    {
        private IInlineHost? _host = parent as IInlineHost;

        public void AttachHost(IInlineHost? host)
        {
            _host = host;
        }

        IEnumerator<IInline> IEnumerable<IInline>.GetEnumerator()
        {
            return GetEnumerator();
        }

        protected override void InsertItem(int index, Inline item)
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(index, 0);
            item.LogicalParent = parent;
            item.AttachHost(_host);
            base.InsertItem(index, item);
        }

        protected override void SetItem(int index, Inline item)
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(index, 0);
            var oldItem = Items[index];
            oldItem.LogicalParent = null;
            oldItem.AttachHost(null);
            item.LogicalParent = parent;
            item.AttachHost(_host);
            base.SetItem(index, item);
        }

        protected override void ClearItems()
        {
            foreach (var node in Items)
            {
                node.LogicalParent = null;
                node.AttachHost(null);
            }
            base.ClearItems();
        }

        protected override void RemoveItem(int index)
        {
            var item = Items[index];
            item.LogicalParent = null;
            item.AttachHost(null);
            base.RemoveItem(index);
        }

        protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            base.OnCollectionChanged(e);
            _host?.Invalidate();
        }

        IInline IReadOnlyList<IInline>.this[int index] => this[index];
        
        [UsedImplicitly]
        public void Add(string text)
        {
            if (_host is TextRun { HasComplexContent: false } textBlock)
            {
                textBlock.Text += text;
            }
            else
            {
                Add(new Run(text));
            }
        }
    }
}

namespace NFMWorld.UI
{
    public abstract class Inline : TextElement, IInline
    {
        protected IInlineHost? Host { get; private set; }

        public abstract override IReadOnlyList<IInline> LogicalChildren { get; }
        
        public virtual void AttachHost(IInlineHost? host)
        {
            Host = host;
            OnInlineHostChanged();
        }

        public virtual void OnInlineHostChanged()
        {
        }
    }
    
    public interface IInlineHost
    {
        /// <summary>
        /// Notify the host that a change to a contained <see cref="IInline"/> has occurred.
        /// </summary>
        void Invalidate();
    }

    public interface IInline : ILogical;
    public interface IRichTextElement
    {
        Color? Background { get; }
        Color? Foreground { get; }
        Color? Stroke { get; }
        FontFamily? FontFamily { get; }
        float? FontSize { get; }
        FontStyle? FontStyle { get; }
    }

    public interface IRichTextLeaf : IRichTextElement
    {
        string Text { get; }
    }

    public interface IRichTextContainer : IRichTextElement
    {
        IReadOnlyList<IRichTextElement> Children { get; }
    }

    /// <summary>
    /// TextElement is an  base class for content in text based controls.
    /// TextElements span other content, applying property values or providing structural information.
    /// </summary>
    public abstract partial class TextElement : BindableObject, IRichTextElement
    {
        /// <summary>
        /// Gets or sets a brush used to paint the control's background.
        /// </summary>
        [Property]
        public partial Color? Background { get; set; }

        /// <summary>
        /// Gets or sets the font family.
        /// </summary>
        [Property]
        public partial FontFamily? FontFamily { get; set; }

        /// <summary>
        /// Gets or sets the font size.
        /// </summary>
        [Property]
        public partial float? FontSize { get; set; }

        /// <summary>
        /// Gets or sets the font style.
        /// </summary>
        [Property]
        public partial FontStyle? FontStyle { get; set; }
    
        /// <summary>
        /// Gets or sets a brush used to paint the text.
        /// </summary>
        [Property]
        public partial Color? Foreground { get; set; }
    
        [Property]
        public partial Color? Stroke { get; set; }
    }

    /// <summary>
    /// A terminal element in text flow hierarchy - contains a uniformatted run of unicode characters
    /// </summary>
    public partial class Run : Inline, IRichTextLeaf
    {
        public override IReadOnlyList<IInline> LogicalChildren => [];

        /// <summary>
        /// Initializes an instance of Run class.
        /// </summary>
        public Run()
        {
            Text = string.Empty;
        }

        /// <summary>
        /// Initializes an instance of Run class specifying its text content.
        /// </summary>
        /// <param name="text">
        /// Text content assigned to the Run.
        /// </param>
        public Run(string? text)
        {
            Text = text ?? string.Empty;
        }

        /// <summary>
        /// The content spanned by this TextElement.
        /// </summary>
        [Content]
        [Property(DefaultMode = BindingMode.TwoWay, OnChangedMethod = nameof(OnTextChanged))]
        public partial string Text { get; set; }
    
        private partial void OnTextChanged(string newText)
        {
            Host?.Invalidate();
        }
    }

    /// <summary>
    /// Span element used for grouping other Inline elements.
    /// </summary>
    public class Span : Inline, IAddChild<Inline>, IAddChild<string>, IRichTextContainer
    {
        /// <summary>
        /// Gets or sets the inlines.
        /// </summary>
        [Content]
        public InlineCollection Inlines { get; }

        private readonly RichTextElementList _subElements;
        IReadOnlyList<IRichTextElement> IRichTextContainer.Children => _subElements;

        public Span()
        {
            Inlines = new InlineCollection(this);
            _subElements = new RichTextElementList(Inlines);
        }

        public override IReadOnlyList<IInline> LogicalChildren => Inlines;

        void IAddChild<Inline>.AddChild(Inline child)
        {
            Inlines.Add(child);
        }

        void IAddChild<string>.AddChild(string child)
        {
            Inlines.Add(new Run(child));
        }

        void IAddChild.AddChild(object child)
        {
            switch (child)
            {
                case Inline inline:
                    Inlines.Add(inline);
                    break;
                case string str:
                    Inlines.Add(new Run(str));
                    break;
                default:
                    throw new ArgumentException($"Unsupported child type: {child.GetType()}. Expected {nameof(Inline)} or string.");
            }
        }

        public override void AttachHost(IInlineHost? host)
        {
            base.AttachHost(host);
            Inlines.AttachHost(host);
            foreach (var inline in Inlines)
            {
                inline.AttachHost(host);
            }
        }

        private class RichTextElementList(InlineCollection inlines) : IReadOnlyList<IRichTextElement>
        {
            public IEnumerator<IRichTextElement> GetEnumerator()
            {
                foreach (var inline in inlines)
                {
                    if (inline is IRichTextElement element)
                    {
                        yield return element;
                    }
                }
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            public int Count => inlines.Count<ILogical>(inline => inline is IRichTextElement);

            public IRichTextElement this[int index] => inlines.OfType<IRichTextElement>().ElementAt(index);
        }
    }

    public partial class TextRun : Node, IInlineHost, IAddChild<Inline>
    {
        private IFontMetrics? _fontMetrics;
        private ComplexTextMetrics.RichTextContainer? _laidOutComplexText;

        /// <summary>
        /// Sets the fill color of the text. The default value is white.
        /// </summary>
        [Property(DefaultValueMember = nameof(DefaultColor))]
        public partial Color Color { get; set; }
    
        private static partial Color DefaultColor => new(255, 255, 255);
    
        /// <summary>
        /// Sets the stroke color of the text. Or set to null to disable the stroke.
        /// </summary>
        [Property]
        public partial Color? StrokeColor { get; set; }
    
        [Content]
        public InlineCollection Inlines { get; }

        [MemberNotNullWhen(true, nameof(Inlines))]
        public bool HasComplexContent => Inlines is { Count: > 0 };

        [Property(OnChangedMethod = nameof(OnFontChanged))]
        public partial Font Font { get; set; }
    
        private partial void OnFontChanged(Font newFont)
        {
            SetFontMetrics();
            RelayoutText();
        }
    
        /// <summary>
        /// Sets the text.
        /// </summary>
        [Property(DefaultValue = "", OnChangedMethod = nameof(OnTextChanged))]
        public partial string? Text { get; set; }

        public TextRun()
        {
            Inlines = new InlineCollection(this);
        }
    
        private partial void OnTextChanged(string? newText)
        {
            if (HasComplexContent)
            {
                Inlines.Clear();
            }

            RelayoutText();
        }

        [MemberNotNull(nameof(_fontMetrics))]
        private void SetFontMetrics()
        {
            G.SetFont(Font with { Size = Font.Size }); // Does not use scale here
            _fontMetrics = G.GetFontMetrics();
        }

        private void RelayoutText()
        {
            if (_fontMetrics == null)
            {
                SetFontMetrics();
            }

            if (!HasComplexContent)
            {
                var measurements = _fontMetrics.MeasureText(Text ?? string.Empty);
                Width = measurements.X;
                Height = measurements.Y;
            }
            else
            {
                var measurements = ComplexTextMetrics.MeasureRichText(Inlines.OfType<IRichTextElement>(), Font);
                Width = measurements.Size.X;
                Height = measurements.Size.Y;
                _laidOutComplexText = measurements;
            }
        }

        /// <summary>
        /// Sets the horizontal alignment of the text. The default value is <see cref="TextHorizontalAlignment.Left"/>.
        /// </summary>
        [Property(DefaultValue = TextHorizontalAlignment.Left)]
        public partial TextHorizontalAlignment HorizontalAlignment { get; set; }

        /// <summary>
        /// Sets the vertical alignment of the text. The default value is <see cref="TextVerticalAlignment.Top"/>.
        /// </summary>
        [Property(DefaultValue = TextVerticalAlignment.Top)]
        public partial TextVerticalAlignment VerticalAlignment { get; set; }

        protected override void RenderContent(System.Numerics.Vector2 position, System.Numerics.Vector2 size)
        {
            base.RenderContent(position, size);

            if (string.IsNullOrEmpty(Text))
            {
                return;
            }

            if (!HasComplexContent)
            {
                G.SetFont(Font with { Size = Font.Size * G.Scale });
                if (StrokeColor != null)
                {
                    G.SetColor((Color)StrokeColor);
                    G.DrawStringStrokeAligned(Text, (int)position.X, (int)position.Y, (int)size.X, (int)size.Y,
                        HorizontalAlignment, VerticalAlignment);
                }

                G.SetColor(Color);
                G.DrawStringAligned(Text, (int)position.X, (int)position.Y, (int)size.X, (int)size.Y, HorizontalAlignment, VerticalAlignment);
            }
            else
            {
                Debug.Assert(_laidOutComplexText != null, "Complex text layout should have been calculated in RelayoutText method.");
            
                var basePosition = position;
                ComplexTextMetrics.AlignBounds(_laidOutComplexText.Value.Size, (int)size.X, (int)size.Y, HorizontalAlignment, VerticalAlignment, ref basePosition.X, ref basePosition.Y);

                foreach (var element in _laidOutComplexText.Value.Elements)
                {
                    G.SetFont(element.Font with { Size = Font.Size * G.Scale });
                    if (element.Background is { } background)
                    {
                        G.SetColor(background);
                        G.FillRect((int)basePosition.X, (int)basePosition.Y, (int)element.Size.X, (int)element.Size.Y);
                    }

                    float yOff = 0;
                    if (VerticalAlignment == TextVerticalAlignment.Center)
                    {
                        yOff = -(G.GetFontMetrics(element.Font).LineHeight / 2.0f);
                    }
                    else if (VerticalAlignment == TextVerticalAlignment.Bottom)
                    {
                        yOff = -G.GetFontMetrics(element.Font).LineHeight;
                    }

                    int x = (int)(basePosition.X + element.Position.X);
                    int y = (int)(basePosition.Y + element.Position.Y + yOff);

                    if (element.Stroke is { } stroke)
                    {
                        G.SetColor(stroke);
                        G.DrawStringStroke(element.Text, x, y);
                    }
                
                    G.SetColor(Color);
                    G.DrawString(element.Text, x, y);
                }
            }
        }
        
        void IAddChild<Inline>.AddChild(Inline child)
        {
            Inlines?.Add(child);
        }

        void IAddChild.AddChild(object child)
        {
            if (child is Inline node)
            {
                Inlines.Add(node);
            }
        }

        public void Invalidate()
        {
            RelayoutText();
        }
    }
}