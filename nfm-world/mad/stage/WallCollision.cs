using NFMWorld.Mad;
using SoftFloat;

public class WallCollision(Rad3dBoxDef[] boxes) : GameObject, ICollidable
{
    public Rad3dBoxDef[] Boxes { get; } = boxes;

    public int MaxRadius
    {
        get
        {
            int maxRadius = 0;
            foreach (var box in Boxes)
            {
                int boxMax = (int)fix64.Ceiling(fix64.Max(box.Radius.X, fix64.Max(box.Radius.Y, box.Radius.Z)));
                if (boxMax > maxRadius)
                {
                    maxRadius = boxMax;
                }
            }
            return maxRadius;
        }
    }
}