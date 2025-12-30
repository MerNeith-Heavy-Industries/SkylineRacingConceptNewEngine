using Microsoft.Xna.Framework.Graphics;
using NFMWorld;
using NFMWorld.Mad;
using SoftFloat;
using Stride.Core.Mathematics;

public class CheckPoint : CollisionObject
{
    public CheckPoint(PlaceableObjectInfo placeableObjectInfo, f64Vector3 position, f64Euler rotation) : base(placeableObjectInfo, position, rotation)
    {
        Kind = AiNodeKind.CheckPoint;
    }

    public enum CheckPointRotation 
    {
        None = 1,
        RightAngle = 2
    }

    public CheckPointRotation CheckPointRot
    {
        get
        {
            if (Rotation.Yaw == f64AngleSingle.ZeroAngle || Rotation.Yaw == f64AngleSingle.StraightAngle)
                return CheckPointRotation.None;
            else
                return CheckPointRotation.RightAngle;
        } 
    }
}