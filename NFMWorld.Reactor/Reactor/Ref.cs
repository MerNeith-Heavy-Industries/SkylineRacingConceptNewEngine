namespace NFMWorld.Reactor;

/// <summary>
/// A mutable reference that persists across renders (like React's useRef).
/// Mutating <see cref="Current"/> does NOT trigger a re-render.
/// Obtain via <see cref="Component.UseRef{T}"/>.
/// </summary>
public class Ref<T>(T initial)
{
    public T Current { get; set; } = initial;
}
