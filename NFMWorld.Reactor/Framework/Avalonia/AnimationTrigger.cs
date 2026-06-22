namespace WorldXaml.UI;

/// <summary>
/// A trigger that fires when a condition is met (e.g. element mounted/unmounted).
/// Used by the animation system.
/// </summary>
public class AnimationTrigger
{
    private bool _triggered;

    public void Trigger() => _triggered = true;
    public void Reset() => _triggered = false;
    public bool IsTriggered => _triggered;
}
