using SoftFloat;
using Stride.Core.Mathematics;

namespace NFMWorld.Mad;

public class Transform
{
    public IReadOnlyList<GameObject> Children { get; set; } = [];

    public f64Vector3 Position {
        get;
        set
        {
            MatrixWorldNeedsUpdate = true;
            field = value;
        }
    } = f64Vector3.Zero;
    public f64Euler Rotation {
        get;
        set
        {
            MatrixWorldNeedsUpdate = true;
            field = value;
        }
    } = new();

    public Transform? Parent
    {
        get;
        set
        {
            MatrixWorldNeedsUpdate = true;
            field = value;
        }
    }

    public Matrix MatrixWorld
    {
        get
        {
            if (MatrixWorldNeedsUpdate)
            {
                var ownMatrixWorld = Matrix.CreateFromEuler((Euler)Rotation) * Matrix.CreateTranslation((Vector3)Position);
                if (Parent != null)
                {
                    ownMatrixWorld = ownMatrixWorld * Parent.MatrixWorld;
                }

                field = ownMatrixWorld;
                MatrixWorldNeedsUpdate = false;
            }

            return field;
        }
    }

    private bool MatrixWorldNeedsUpdate
    {
        get => field || (Parent?.MatrixWorldNeedsUpdate ?? false);
        set;
    } = true;

    public virtual void GameTick(Stage? stage = null)
    {
    }
}