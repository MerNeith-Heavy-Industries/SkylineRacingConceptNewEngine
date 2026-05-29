namespace NFMWorld.UI;

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