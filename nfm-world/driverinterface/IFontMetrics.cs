namespace NFMWorld.DriverInterface;

public interface IFontMetrics
{
    public float StringWidth(string astring);
    public int Height(string astring);
}