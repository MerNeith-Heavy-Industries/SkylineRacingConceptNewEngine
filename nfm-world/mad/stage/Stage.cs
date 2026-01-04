using System.Runtime.InteropServices;
using Microsoft.Xna.Framework.Graphics;
using nfm_world.mad.collision;
using NFMWorld;
using NFMWorld.Library.mad;
using NFMWorld.Mad;
using NFMWorld.Util;
using SoftFloat;
using Stride.Core.Extensions;

/**
    Represents a stage. Holds all information relating to track pices, scenery, etc.
    But does NOT hold any information relating to the actual game being played, unless such game affects the layout or scenery of the stage.
*/
public class Stage : GameObject, IStage
{
    public UnlimitedArray<GameObject> pieces = [];
    public UnlimitedArray<IAiNode> nodes = [];
    public UnlimitedArray<CheckPoint> checkpoints = [];
    public UnlimitedArray<FixHoop> fixHoops = [];

    public int stagePartCount => pieces.Count;

    public Sky sky;
    public Ground ground;
    public GroundPolys? polys;
    public GroundPolys? clouds;
    public Mountains? mountains;

    private int? _fadeFrom = null;
    public readonly string Path;
    public int nlaps;

    // left
    public int Sx;
    // top
    public int Sz;
    // width
    public int Ncx;
    // height
    public int Ncz;

    public string Name = "hogan rewish";

    // soundtrack(folder,fileName)
    public string musicPath = "";
    // soundtrackremaster(folder,fileName)
    public string remasteredMusicPath = "";
    // soundtrackfreqmul(mul)
    public double musicFreqMul = 1.0d;
    public double musicTempoMul = 0d;

    public void ReapplyFadeFrom()
    {
        if(_fadeFrom != null)
            World.FadeFrom = (int)_fadeFrom;
    }

    /**
     * Loads stage currently set by checkpoints.stage onto stageContos
     */
    public Stage(string stageName, GraphicsDevice graphicsDevice)
    {
        Children = pieces;

        Path = stageName;
        World.ResetValues();
        try
        {
            var stageLoader = new StageLoader(stageName);

            if (stageLoader.Snap is { } snap)
            {
                World.Snap = snap;
            }

            if (stageLoader.Sky is { } sky)
            {
                World.Sky = sky;
            }

            if (stageLoader.GroundColor is { } ground)
            {
                World.GroundColor = ground;
            }

            World.DrawPolys = stageLoader.DrawPolys;
            World.HasPolys = stageLoader.DrawPolys && stageLoader.GroundPolysColor is not null;
            if (stageLoader.GroundPolysColor is { } groundPolys)
            {
                World.GroundPolysColor = groundPolys;
            }

            if (stageLoader.Fog is { } fog)
            {
                World.Fog = fog;
            }

            if (stageLoader.Texture is { } texture)
            {
                World.HasTexture = true;
                World.Texture = [..texture];
            }

            World.DrawClouds = stageLoader.DrawClouds;
            World.HasClouds = stageLoader.DrawClouds && stageLoader.Clouds is not null;
            if (stageLoader.Clouds is { } aclouds)
            {
                World.Clouds = [..aclouds];
            }

            if (stageLoader.CloudCoverage is { } cloudCoverage)
            {
                World.CloudCoverage = cloudCoverage;
            }

            if (stageLoader.FogDensity is { } fogDensity)
            {
                World.FogDensity = fogDensity;
            }

            if (stageLoader.FadeFrom is { } fadeFrom)
            {
                World.FadeFrom = fadeFrom;
                _fadeFrom = World.FadeFrom;
            }

            if (stageLoader.LightsOn)
            {
                World.LightsOn = true;
            }

            World.DrawMountains = stageLoader.DrawMountains;
            if (stageLoader.MountainSeed is { } mountainSeed)
            {
                World.MountainSeed = mountainSeed;
            }

            if (stageLoader.MountainCoverage is { } mountainCoverage)
            {
                World.MountainCoverage = mountainCoverage;
            }

            if (stageLoader.LightDirection is { } lightDirection)
            {
                World.LightDirection = lightDirection;
            }

            foreach (var piece in stageLoader.pieces)
            {
                switch (piece.Type)
                {
                    case PiecePlacementType.CollisionObject:
                    {
                        if (!TryGetPieceToPlace(piece.Set.HasName ? piece.Set.Name : piece.Set.Id.ToString()!,
                                out var mesh)) continue;

                        var obj = new CollisionObject(
                            mesh,
                            piece.Position,
                            piece.Rotation
                        );
                        pieces[stagePartCount] = obj;
                        if (piece.NodeKind is { } nodeKind)
                        {
                            nodes[nodes.Count] = obj;
                            obj.Kind = nodeKind;
                        }

                        break;
                    }
                    case PiecePlacementType.CheckPoint:
                    {
                        if (!TryGetPieceToPlace(piece.Set.HasName ? piece.Set.Name : piece.Set.Id.ToString()!,
                                out var mesh)) continue;

                        var obj = new CheckPoint(
                            mesh,
                            piece.Position,
                            piece.Rotation
                        );
                        pieces[stagePartCount] = obj;
                        nodes[nodes.Count] = obj;
                        checkpoints[checkpoints.Count] = obj;

                        break;
                    }
                    case PiecePlacementType.FixHoop:
                    {
                        if (!TryGetPieceToPlace(piece.Set.HasName ? piece.Set.Name : piece.Set.Id.ToString()!,
                                out var mesh)) continue;

                        var fix = new FixHoop(
                            mesh,
                            piece.Position,
                            piece.Rotation
                        );
                        pieces[stagePartCount] = fix;
                        if (piece.Rotation.Xz.Degrees != 0)
                        {
                            fix.Rotated = true;
                        }

                        fixHoops[fixHoops.Count] = fix;
                        nodes[nodes.Count] = fix;
                        if (piece.IsSpecial)
                        {
                            fix.IsSpecial = true;
                        }

                        break;
                    }
                    default:
                    {
                        throw new ArgumentOutOfRangeException(nameof(piece.Type), piece.Type, null);
                    }
                }
            }

            nlaps = stageLoader.nlaps;
            Name = stageLoader.Name;
            musicPath = stageLoader.musicPath;
            remasteredMusicPath = stageLoader.remasteredMusicPath;
            musicFreqMul = stageLoader.musicFreqMul;
            musicTempoMul = stageLoader.musicTempoMul;

            // stage walls
            if (stageLoader.walls.Count > 0)
            {
                pieces[stagePartCount] = new WallCollision([..stageLoader.walls]);
            }

            if (musicPath.IsNullOrEmpty())
            {
                GameSparker.Writer.WriteLine("No music is defined for this stage!", "error");
            }

            // Medium.Newpolys(maxl, maxr - maxl, maxb, maxt - maxb, stagePartCount);
            // Medium.Newmountains(maxl, maxr, maxb, maxt);
            // Medium.Newclouds(maxl, maxr, maxb, maxt);
            // Medium.Newstars();
            SetBounds(stageLoader.maxl, stageLoader.maxr - stageLoader.maxl, stageLoader.maxb,
                stageLoader.maxt - stageLoader.maxb);

            if (World.DrawPolys)
            {
                polys = NFMWorld.Mad.Environment.MakePolys(this, stageLoader.maxl, stageLoader.maxr - stageLoader.maxl,
                    stageLoader.maxb, stageLoader.maxt - stageLoader.maxb, stagePartCount, graphicsDevice);
            }

            if (World.DrawClouds)
            {
                clouds = NFMWorld.Mad.Environment.MakeClouds(stageLoader.maxl, stageLoader.maxr, stageLoader.maxb,
                    stageLoader.maxt, graphicsDevice);
            }

            if (World.DrawMountains)
            {
                mountains = NFMWorld.Mad.Environment.MakeMountains(stageLoader.maxl, stageLoader.maxr, stageLoader.maxb,
                    stageLoader.maxt, graphicsDevice);
            }
        }
        catch (StageLoadException exception)
        {
            GameSparker.Writer.WriteLine($"Error in stage: {stageName}", "error");
            GameSparker.Writer.WriteLine($"At line: {exception.Line} (number {exception.LineNumber})", "error");
            GameSparker.Writer.WriteLine(exception.ToString(), "error");
        }
        catch (Exception exception)
        {
            GameSparker.Writer.WriteLine($"Error in stage: {stageName}", "error");
            GameSparker.Writer.WriteLine(exception.ToString(), "error");
        }
        sky = new Sky(graphicsDevice);
        ground = new Ground(graphicsDevice);
    }

    private void SetBounds(int sx, int ncx, int sz, int ncz)
    {
        Sx = sx;
        Sz = sz;
        Ncx = ncx;
        if (Ncx <= 0)
        {
            Ncx = 1;
        }
        Ncz = ncz;
        if (Ncz <= 0)
        {
            Ncz = 1;
        }
        
        CollisionQuadTree = new QuadTree<CollisionBoxRef>(sx, sz, ncx, ncz);
        foreach (var piece in pieces)
        {
            if (piece is ICollidable collidable)
            {
                AddToQuadTree(collidable);
            }
        }
        CollisionQuadTree.TrimExcess();
    }

    private static bool TryGetPieceToPlace(string setstring, out PlaceableObjectInfo mesh)
    {
        if (int.TryParse(setstring, out var setindex))
        {
            mesh = GameSparker.stage_parts[setindex];
            if (mesh == null!)
            {
                GameSparker.Writer.WriteLine($"Stage part '{setstring}' not found.", "error");
                mesh = GameSparker.error_mesh;
                return true;
            }
        }
        else
        {
            var stagePart = GameSparker.GetStagePart(setstring);
            if (stagePart.Mesh == null)
            {
                GameSparker.Writer.WriteLine($"Stage part '{setstring}' not found.", "error");
                mesh = GameSparker.error_mesh;
                return true;
            }
            mesh = stagePart.Mesh;
        }

        return true;
    }

    public GameObject CreateObject(string objectName, int x, int y, int z, int r)
    {
        var part = GameSparker.GetStagePart(objectName);
        if (part.Mesh == null)
        {
            GameSparker.devConsole.Log($"Object '{objectName}' not found.", "warning");
            part = (-1, GameSparker.error_mesh);
        }

        var mesh = pieces[stagePartCount] = new CollisionObject(
            part.Mesh,
            new f64Vector3(x,
            250 - y,
            z), 
            new f64Euler(f64AngleSingle.FromDegrees(r), f64AngleSingle.ZeroAngle, f64AngleSingle.ZeroAngle)
        );

        GameSparker.devConsole.Log($"Created {objectName} at ({x}, {y}, {z}), rotation: {r}", "info");

        AddToQuadTree((mesh as ICollidable)!);

        return mesh;
    }
    
    private QuadTree<CollisionBoxRef> CollisionQuadTree = new(0,0,0,0);
    private int _quadTreeInsertionIndex = 0;

    private void AddToQuadTree(ICollidable mesh)
    {
        fix64 x = 0;
        fix64 y = 0;
        fix64 z = 0;
        fix64 xz = 0;
        if (mesh is GameObject gameObject)
        {
            x = (fix64)gameObject.Position.X;
            y = (fix64)gameObject.Position.Y;
            z = (fix64)gameObject.Position.Z;
            xz = gameObject.Rotation.Xz.Degrees;
        }
        
        foreach (var box in mesh.Boxes)
        {
            var maxR = fix64.Max(
                mesh.MaxRadius,
                fix64.Max(
                    fix64.Max(
                        fix64.Abs(box.Translation.X) + fix64.Abs(box.Radius.X),
                        fix64.Abs(box.Translation.Z) + fix64.Abs(box.Radius.Z)
                    ),
                    fix64.Abs(box.Translation.Y) + fix64.Abs(box.Radius.Y)
                )
            );
            CollisionQuadTree.Insert(new CollisionBoxRef(
                gameObjectX: x,
                gameObjectY: y,
                gameObjectZ: z,
                gameObjectRotXz: xz,
                box: box,
                maxR,
                index: _quadTreeInsertionIndex++
            ));
        }
    }
    
    private List<CollisionBoxRef> _tempTrackers = new();

    public ReadOnlySpan<CollisionBoxRef> RetrievePointCollidables(fix64 x, fix64 z)
    {
        _tempTrackers.Clear();
        CollisionQuadTree.RetrievePoint(_tempTrackers, x, z);
        var span = CollectionsMarshal.AsSpan(_tempTrackers);
        span.Sort(static (a, b) => a.Index.CompareTo(b.Index));
        return span;
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
}