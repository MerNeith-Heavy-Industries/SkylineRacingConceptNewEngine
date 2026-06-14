using Microsoft.Xna.Framework.Graphics;
using NFMWorldLibrary;
using NFMWorldLibrary.Backend;
using NFMWorldLibrary.Util;
using Environment = NFMWorld.Environment;

namespace NFMWorld;

/**
Represents a stage. Holds all information relating to track pices, scenery, etc.
But does NOT hold any information relating to the actual game being played, unless such game affects the layout or scenery of the stage.
*/
public class ClientStageRenderer : GameObject
{
    public UnlimitedArray<StageObjectGameObject> checkpoints = [];
    public UnlimitedArray<StageObjectGameObject> fixhoops = [];
    
    public Sky? sky;
    public Ground? ground;
    public GroundPolys? polys;
    public GroundPolys? clouds;
    public Mountains? mountains;

    private readonly BackendStage backendStage;

    /**
     * Loads stage currently set by checkpoints.stage onto stageContos
     */
    public ClientStageRenderer(GraphicsDevice graphicsDevice, BackendStage backendStage)
    {
        this.backendStage = backendStage;
        var children = new List<GameObject>();
        Children = children;
        World.ResetValues();
        try
        {
            var stageLoader = backendStage.stageLoader;

            ApplyValues();

            // Medium.Newpolys(maxl, maxr - maxl, maxb, maxt - maxb, stagePartCount);
            // Medium.Newmountains(maxl, maxr, maxb, maxt);
            // Medium.Newclouds(maxl, maxr, maxb, maxt);
            // Medium.Newstars();
            if (stageLoader.DrawPolys)
            {
                polys = Environment.MakePolys(backendStage, stageLoader.maxl, stageLoader.maxr - stageLoader.maxl,
                    stageLoader.maxb, stageLoader.maxt - stageLoader.maxb, backendStage.stagePartCount, graphicsDevice);
            }

            if (stageLoader.DrawClouds)
            {
                clouds = Environment.MakeClouds(stageLoader.maxl, stageLoader.maxr, stageLoader.maxb,
                    stageLoader.maxt, graphicsDevice);
            }

            if (stageLoader.DrawMountains)
            {
                mountains = Environment.MakeMountains(stageLoader.maxl, stageLoader.maxr, stageLoader.maxb,
                    stageLoader.maxt, graphicsDevice);
            }
            
            foreach (var piece in backendStage.pieces)
            {
                if (piece is StageObject obj)
                {
                    var mesh = GameSparker.GetStagePartMesh(obj.Rad);
                    if (obj.Kind == AiNodeKind.CheckPoint)
                    {
                        var clientObj = new StageObjectGameObject(mesh, obj)
                        {
                            Parent = this
                        };
                        children.Add(clientObj);

                        checkpoints.Add(clientObj);
                    }
                    else if (obj.Kind == AiNodeKind.FixHoop)
                    {
                        var clientObj = new FixHoop(mesh, obj)
                        {
                            Parent = this
                        };
                        children.Add(clientObj);

                        fixhoops.Add(clientObj);
                    }
                    else
                    {
                        var clientObj = new StageObjectGameObject(mesh, obj)
                        {
                            Parent = this
                        };
                        children.Add(clientObj);
                    }
                }
            }
        }
        catch (Exception exception)
        {
            SentrySdk.CaptureException(exception);
            Logging.Error($"Error in stage: {backendStage.Name}");
            Logging.Error(exception.ToString());
        }
        sky = new Sky(graphicsDevice);
        ground = new Ground(graphicsDevice);
    }

    public void ApplyValues()
    {
        foreach (var instruction in backendStage.stageLoader.EnvironmentInstructions)
        {
            switch (instruction)
            {
                case CloudsInstruction clouds:
                    World.HasClouds = true;
                    break;
                case FogInstruction fog:
                    World.Fog = fog.Color;
                    break;
                case GroundInstruction ground:
                    World.SetGround(ground.Color);
                    break;
                case PolysInstruction polys:
                    World.GroundPolysColor = polys.Color;
                    World.HasPolys = true;
                    break;
                case SkyInstruction sky:
                    World.SetSky(sky.Color);
                    break;
                case SnapInstruction snap:
                    World.Snap = snap.Color;
                    break;
                case TextureInstruction texture:
                    World.SetTexture(texture.Texture);
                    World.HasTexture = true;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(instruction), instruction, null);
            }
        }

        World.DrawPolys = backendStage.stageLoader.DrawPolys;
        World.HasPolys = backendStage.stageLoader.DrawPolys && World.HasPolys;

        World.DrawClouds = backendStage.stageLoader.DrawClouds;
        World.HasClouds = backendStage.stageLoader.DrawClouds && World.HasClouds;

        if (backendStage.stageLoader.CloudCoverage is { } cloudCoverage)
        {
            World.CloudCoverage = cloudCoverage;
        }

        if (backendStage.stageLoader.FogDensity is { } fogDensity)
        {
            World.FogDensity = fogDensity;
        }

        if (backendStage.stageLoader.FadeFrom is { } fadeFrom)
        {
            World.FadeFrom = fadeFrom;
        }

        if (backendStage.stageLoader.LightsOn)
        {
            World.LightsOn = true;
        }

        World.DrawMountains = backendStage.stageLoader.DrawMountains;
        if (backendStage.stageLoader.MountainSeed is { } mountainSeed)
        {
            World.MountainSeed = mountainSeed;
        }

        if (backendStage.stageLoader.MountainCoverage is { } mountainCoverage)
        {
            World.MountainCoverage = mountainCoverage;
        }

        if (backendStage.stageLoader.LightDirection is { } lightDirection)
        {
            World.LightDirection = lightDirection;
        }
    }

    public override void Render(Camera camera, Lighting? lighting)
    {
        sky?.Render(camera, lighting);
        ground?.Render(camera, lighting);
        polys?.Render(camera, lighting);
        clouds?.Render(camera, lighting);
        mountains?.Render(camera, lighting);
        base.Render(camera, lighting);
    }

    public void ResetCheckpointGlow()
    {
        foreach (var checkpoint in checkpoints)
        {
            checkpoint.Glow = false;
            checkpoint.Finish = false;
        }
    }

    public void UpdateCheckpointGlow(ushort currentCheckpoint, bool isFinish)
    {
        if (checkpoints.Count > 0)
        {
            if (isFinish)
            {
                checkpoints[^1].Finish = true;
            }
            else
            {
                checkpoints[^1].Finish = false;
            }

            if (currentCheckpoint > 0)
            {
                checkpoints[currentCheckpoint - 1].Glow = false;
            }
            else
            {
                checkpoints[^1].Glow = false;
            }

            if (currentCheckpoint < checkpoints.Count)
            {
                checkpoints[currentCheckpoint].Glow = true;
            }
        }
    }
}