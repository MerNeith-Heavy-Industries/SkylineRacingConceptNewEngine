using Microsoft.Xna.Framework.Graphics;
using NFMWorld.Mad;
using Stride.Core.Extensions;

public class CarMesh : Mesh
{
    public CarStats Stats;
    public Rad3dWheelDef[] Wheels;
    public Rad3dRimsDef? Rims;

    public CarMesh(GraphicsDevice graphicsDevice, Rad3d rad, string fileName) : base(graphicsDevice, rad, fileName)
    {
        Stats = CarStats.ValidateStats(rad.Stats, fileName);

        Wheels = rad.Wheels;
        Rims = rad.Rims;
    }
}