using NFMWorld.Mad;

public class WallCollision(Rad3dBoxDef[] boxes) : GameObject, ICollidable
{
    public Rad3dBoxDef[] Boxes { get; } = boxes;
}