using NFMWorldLibrary.Collision;
using NFMWorldLibrary.FixedMath;
using NFMWorldLibrary.Util;

namespace NFMWorldLibrary.Helpers;

public class CheckPointHelper
{
    public static void CalculatePositions(
        IStage currentStage,
        IReadOnlyList<IInGameCar> carsInRace
    )
    {
        foreach (var car in carsInRace)
        {
            car.Placement = 0;
        }

        for (int i = 0; i < carsInRace.Count; i++)
        {
            var car1 = carsInRace[i];
            for (int j = i + 1; j < carsInRace.Count; j++)
            {
                var car2 = carsInRace[j];
                if (car1.TotalCheckpoint != car2.TotalCheckpoint)
                {
                    if (car1.TotalCheckpoint < car2.TotalCheckpoint)
                    {
                        carsInRace[i].Placement++;
                    }
                    else
                    {
                        carsInRace[j].Placement++;
                    }
                }
                else
                {
                    int c = carsInRace[i].CurrentCheckpoint + 1;
                    if (c >= currentStage.checkpoints.Count)
                    {
                        c = 0;
                    }

                    if (UMath.Py(
                            carsInRace[i].Position.X / 100,
                            currentStage.checkpoints[c].Position.X / 100,
                            carsInRace[i].Position.Z / 100,
                            currentStage.checkpoints[c].Position.Z / 100
                        ) >
                        UMath.Py(
                            carsInRace[j].Position.X / 100,
                            currentStage.checkpoints[c].Position.X / 100,
                            carsInRace[j].Position.Z / 100,
                            currentStage.checkpoints[c].Position.Z / 100
                        )
                       )
                    {
                        carsInRace[i].Placement++;
                    }
                    else
                    {
                        carsInRace[j].Placement++;
                    }
                }
            }
        }
    }

    public static bool HandleCheckPoint(
        IStage currentStage,
        IInGameCar car)
    {
        if (car.CurrentCheckpoint >= currentStage.checkpoints.Count)
            return false;

        var nextCheckpoint = currentStage.checkpoints[car.CurrentCheckpoint];
        f64Vector3 carPos = car.Position;
        var mad = car.CarPhysics;
        f64Vector3 velocity = new f64Vector3(
            mad.Scx[0] + mad.Scx[1] + mad.Scx[2] + mad.Scx[3],
            mad.Scy[0] + mad.Scy[1] + mad.Scy[2] + mad.Scy[3],
            mad.Scz[0] + mad.Scz[1] + mad.Scz[2] + mad.Scz[3]) / 4;
        f64Vector3 zDir = new f64Vector3(0, 0, 1);
        f64Vector3 rad = new f64Vector3(700, 450,
            60 + fix64.Abs(f64Vector3.Dot(velocity, zDir.RotateXz(nextCheckpoint.Rotation.Xz.Degrees))));
        f64Vector3 trackersPosition = new f64Vector3(0, -350, 0);
        var box = new CollisionBox(rad, trackersPosition, nextCheckpoint.Rotation.Xz.Degrees, nextCheckpoint.Position);

        if (box.ResolveCollision(carPos) is not null)
        {
            car.CurrentCheckpoint++;
            if (car.CurrentCheckpoint >= currentStage.checkpoints.Count)
            {
                car.LastCheckpointNode = -1;
                car.CurrentCheckpoint = 0;
                car.CurrentLap++;
            }
            else
            {
                car.LastCheckpointNode = currentStage.nodes.IndexOf(nextCheckpoint);
            }

            car.TotalCheckpoint = car.CurrentCheckpoint + car.CurrentLap * currentStage.checkpoints.Count;
            return true;
        }

        return false;
    }
}