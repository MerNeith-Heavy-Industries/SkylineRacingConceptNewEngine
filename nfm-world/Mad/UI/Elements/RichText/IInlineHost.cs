namespace NFMWorld.UI;

public interface IInlineHost
{
    /// <summary>
    /// Notify the host that a change to a contained <see cref="IInline"/> has occurred.
    /// </summary>
    void Invalidate();
}