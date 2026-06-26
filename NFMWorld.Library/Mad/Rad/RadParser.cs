﻿using System.Collections.Immutable;
 using System.Globalization;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using FixedMathSharp;
using NFMWorldLibrary.FixedMath;
using NFMWorldLibrary.Util;

namespace NFMWorldLibrary.Rad;

public partial class RadParser
{
    private class WheelMesh
    {
        public List<Rad3dPoly> Polys = [];
        public int? Radius;
        public int? Depth;

        public Rad3dPoly[] GetScaledPolys(float radius, float depth)
        {
            if (Radius != null && Depth != null)
            {
                return Polys
                    .Select(poly =>
                    {
                        var scaledPoints = poly.Points.Select(point => new Vector3(
                            point.X * depth,
                            point.Y * radius,
                            point.Z * radius
                        )).ToArray();
                        return poly.WithPoints(scaledPoints, poly.Triangles);
                    })
                    .ToArray();
            }

            return Polys.ToArray();
        }
    }
    
    private int _npoints = 0;
    private bool _stonecold;
    private bool _noOutline;
    private fix64 idiv = (fix64)1f, iwid = (fix64)1f, scaleX = (fix64)1f, scaleY = (fix64)1f, scaleZ = (fix64)1f;
    
    private Dictionary<Color3, int> _colors = new();
    private CarStats _stats = new();
    private List<Rad3dWheelDef> _wheels = [];
    private Rad3dRimsDef? _rims;
    private List<Rad3dBoxDef> _boxes = [];
    private List<Rad3dPoly> _mainCarPolys = [];
    private List<Vector3> _points = [];
    private List<uint> _tris = [];
    private List<Vector2> _atp = [];
    private List<Rad3dAttachmentLine> _atLines = [];
    private bool _road;
    private bool _castsShadow;

    // physhot and SRC format wheel meshes (declared before the wheel)
    private List<WheelMesh> _wheelMeshes = [];

    // phy-addons wheel meshes (declared after the wheel, wheel has )c suffix)
    private UnlimitedArray<List<Rad3dPoly>?> _phyAddonsWheelMeshes = [];

    private List<Rad3dPoly> _currentPolys;

    private Rad3dPoly _currentPoly;
    private bool _inPoly;
    
    private List<f64Vector3> _meshCollisionVerts = [];
    private List<ushort> _meshCollisionIndices = [];
    
    private List<f64Vector3> _hullVerts = [];
    private readonly string _fileName;
    private float _scaleRadius = 1f;
    private float _scaleDepth = 1f;
    private int? _phyAddonsWheelId;

    private RadParser(string fileName)
    {
        _currentPolys = _mainCarPolys;
        _fileName = fileName;
    }
    
    [GeneratedRegex("""<wheel radius="(?<radius>\d+)" depth="(?<depth>\d+)">""", RegexOptions.Compiled)]
    private static partial Regex PhyShotWheelDef { get; }

    public static Rad3d ParseRad(string radFile, string fileName = "hogan rewish")
    {
        var transaction = SentrySdk.StartTransaction("load_rad", fileName);
        var parser = new RadParser(fileName);
        var lines = radFile.AsSpan().Split("\n");
        int lineNumber = 0;
        foreach (var lineRange in lines)
        {
            lineNumber++;
            var line = radFile.AsSpan(lineRange).Trim();
            if (line.IsEmpty) continue;
            try
            {
                parser.ParseLine(line);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error parsing line {lineNumber}: '{line.ToString()}'\n{ex.Message}", ex);
            }
        }
        
        // reconcile phy-addons custom wheels
        for (var i = 0; i < parser._phyAddonsWheelMeshes.Count; i++)
        {
            if (parser._phyAddonsWheelMeshes[i] is { } wheelMesh)
            {
                parser._wheels[i] = parser._wheels[i] with
                {
                    Polys = wheelMesh.ToArray()
                };
            }
        }

        var result = RepositionCar(new Rad3d(
            Colors: parser._colors.Keys.ToArray(),
            Stats: parser._stats,
            Wheels: parser._wheels.ToArray(),
            Rims: parser._rims,
            Boxes: parser._boxes.ToArray(),
            Polys: parser._mainCarPolys.ToArray(),
            CastsShadow: parser._castsShadow,
            Atp: parser._atp.ToArray(),
            AtLines: parser._atLines.Count > 0 ? parser._atLines.ToArray() : null,
            CollisionMesh: parser._meshCollisionVerts.Count > 0 ? new SrcRad3dCollisionMesh(parser._meshCollisionVerts.ToArray(), parser._meshCollisionIndices.ToArray()) : null,
            CollisionHull: parser._hullVerts.Count > 0 ? new SrcRad3dCollisionHull(CollectionsMarshal.AsSpan(parser._hullVerts)) : null,
            FileName: fileName
        ));
        transaction.Finish();
        return result;
    }

    private static Rad3d RepositionCar(Rad3d rad3d)
    {
        if (rad3d.Wheels is { Length: < 4 }) return rad3d;

        // reposition car so that ground is at y=0 and the wheel x and z are equidistant from the origin
        // this fixes masheen bouncing on the big ramp
        fix64 groundTranslation = fix64.MaxValue;
        fix64 wheelXTranslation = 0;
        fix64 wheelZTranslation = 0;
        for (var i = 0; i < 4; i++)
        {
            var wheel = rad3d.Wheels[i];
            var groundY = wheel.Ground;
            if (groundY < groundTranslation)
            {
                groundTranslation = groundY;
            }

            wheelXTranslation += wheel.Position.X;
            wheelZTranslation += wheel.Position.Z;
        }

        wheelXTranslation /= (fix64)4;
        wheelZTranslation /= (fix64)4;
        
        // maxine: this code is incredibly crucial!
        // in theory we should be moving the car to the wheel center, because otherwise the car drifts off of its center
        // on every tick when rotated around, however doing this breaks hypergliding. as we want to retain vanilla
        // behavior at high tickrate, we instead move it by x/y/z * phyiscs_multiplier, which restores
        // behavior at vanilla tickrate speeds.
        
        for (var i = 0; i < rad3d.Wheels.Length; i++)
        {
            var wheel = rad3d.Wheels[i];
            rad3d.Wheels[i] = wheel with
            {
                Position = new f64Vector3(
                    wheel.Position.X - (wheelXTranslation),
                    wheel.Position.Y,// - (groundTranslation),
                    wheel.Position.Z - (wheelZTranslation)
                )
            };
        }

        for (var i = 0; i < rad3d.Polys.Length; i++)
        {
            var poly = rad3d.Polys[i];
            for (var j = 0; j < poly.Points.Length; j++)
            {
                var point = poly.Points[j];
                poly.Points[j] = new Vector3(
                    point.X - (float)(wheelXTranslation),
                    point.Y,// - (float)(groundTranslation),
                    point.Z - (float)(wheelZTranslation)
                );
            }
        }

        return rad3d;
    }

    private void ParseLine(ReadOnlySpan<char> line)
    {
        if (line.StartsWith("stonecold") || line.StartsWith("newstone")) _stonecold = true;
        else if (line.StartsWith("road")) _road = true;
        else if (line.StartsWith("notroad")) _road = false;
        else if (line.StartsWith("shadow")) _castsShadow = true;
        else if (line.StartsWith("gshadow")) _castsShadow = true; // used by decorative trees

        else if (line.StartsWith("1stColor("))
        {
            var color = Color3.FromSpan(BracketParser.GetShorts(line, stackalloc short[3]));
            _colors[color] = 0;
        }

        else if (line.StartsWith("2ndColor("))
        {
            var color = Color3.FromSpan(BracketParser.GetShorts(line, stackalloc short[3]));
            _colors[color] = 1;
        }

        else if (line.StartsWith("3rdColor("))
        {
            var color = Color3.FromSpan(BracketParser.GetShorts(line, stackalloc short[3]));
            _colors[color] = 2;
        }

        else if (line.StartsWith("4thColor("))
        {
            var color = Color3.FromSpan(BracketParser.GetShorts(line, stackalloc short[3]));
            _colors[color] = 3;
        }

        else if (line.StartsWith("swits(")) _stats = _stats with { Swits = Int3.FromSpan(BracketParser.GetNumbers(line, stackalloc int[3])) };
        else if (line.StartsWith("acelf(")) _stats = _stats with { Acelf = f64Vector3.FromSpan(BracketParser.GetNumbers(line, stackalloc fix64[3])) };
        else if (line.StartsWith("handb(")) _stats = _stats with { Handb = BracketParser.GetNumber<int>(line) };
        else if (line.StartsWith("airs(")) _stats = _stats with { Airs = BracketParser.GetNumber<fix64>(line) };
        else if (line.StartsWith("airc(")) _stats = _stats with { Airc = BracketParser.GetNumber<int>(line) };
        else if (line.StartsWith("turn(")) _stats = _stats with { Turn = BracketParser.GetNumber<int>(line) };
        else if (line.StartsWith("grip(")) _stats = _stats with { Grip = BracketParser.GetNumber<fix64>(line) };
        else if (line.StartsWith("bounce(")) _stats = _stats with { Bounce = BracketParser.GetNumber<fix64>(line) };
        else if (line.StartsWith("simag(")) _stats = _stats with { Simag = BracketParser.GetNumber<fix64>(line) };
        else if (line.StartsWith("moment(")) _stats = _stats with { Moment = BracketParser.GetNumber<fix64>(line) };
        else if (line.StartsWith("comprad(")) _stats = _stats with { Comprad = BracketParser.GetNumber<fix64>(line) };
        else if (line.StartsWith("push(")) _stats = _stats with { Push = BracketParser.GetNumber<fix64>(line) };
        else if (line.StartsWith("revpush(")) _stats = _stats with { Revpush = BracketParser.GetNumber<fix64>(line) };
        else if (line.StartsWith("lift(")) _stats = _stats with { Lift = BracketParser.GetNumber<int>(line) };
        else if (line.StartsWith("revlift(")) _stats = _stats with { Revlift = BracketParser.GetNumber<int>(line) };
        else if (line.StartsWith("powerloss(")) _stats = _stats with { Powerloss = BracketParser.GetNumber<int>(line) };
        else if (line.StartsWith("flipy(")) _stats = _stats with { Flipy = BracketParser.GetNumber<int>(line) };
        else if (line.StartsWith("msquash(")) _stats = _stats with { Msquash = BracketParser.GetNumber<int>(line) };
        else if (line.StartsWith("clrad(")) _stats = _stats with { Clrad = BracketParser.GetNumber<int>(line) };
        else if (line.StartsWith("dammult(")) _stats = _stats with { Dammult = BracketParser.GetNumber<fix64>(line) };
        else if (line.StartsWith("maxmag(")) _stats = _stats with { Maxmag = BracketParser.GetNumber<int>(line) };
        else if (line.StartsWith("dishandle(")) _stats = _stats with { Dishandle = BracketParser.GetNumber<fix64>(line) };
        else if (line.StartsWith("handling(")) /* physhot */ _stats = _stats with { Dishandle = BracketParser.GetNumber<fix64>(line) / (fix64)200f };
        else if (line.StartsWith("outdam(")) _stats = _stats with { Outdam = BracketParser.GetNumber<fix64>(line) };
        else if (line.StartsWith("name(")) _stats = _stats with { Name = BracketParser.GetString(line) };
        else if (line.StartsWith("enginsignature(")) _stats = _stats with { Enginsignature = BracketParser.GetNumber<sbyte>(line) };
        else if (line.StartsWith("turnradius(")) _stats = _stats with { TurnRadius = BracketParser.GetNumber<int>(line) };
        else if (line.StartsWith("roadgrip(")) _stats = _stats with { RoadGrip = BracketParser.GetNumber<fix64>(line) };
        else if (line.StartsWith("offroadgrip(")) _stats = _stats with { OffRoadGrip = BracketParser.GetNumber<fix64>(line) };
        else if (line.StartsWith("offtrackgrip(")) _stats = _stats with { OffTrackGrip = BracketParser.GetNumber<fix64>(line) };

        else if (line.StartsWith("w("))
        {
            var (cx, (cy, (cz, (rotates, (width, (height, _)))))) = BracketParser.GetNumbers(line, stackalloc int[6]);
            _wheels.Add(new Rad3dWheelDef(
                Position: new f64Vector3(
                    cx * idiv * iwid * scaleX,
                    cy * idiv * scaleY,
                    cz * idiv * scaleZ
                ),
                Rotates: rotates,
                Width: width * idiv * iwid,
                Height: height * idiv,
                Polys: _wheelMeshes.Count > _wheels.Count
                    // physhot custom wheels
                    ? _wheelMeshes[_wheels.Count].GetScaledPolys(width * (float)idiv * (float)iwid, height * (float)idiv)
                    : null
            ));

            // phy-addons custom wheels
            if (line.EndsWith(")c"))
            {
                _phyAddonsWheelMeshes[_wheels.Count - 1] = [];
            }
        }

        else if (line.StartsWith("rims("))
        {
            _rims = new Rad3dRimsDef(
                Color: new Color3(
                    (byte)BracketParser.GetNumbers(line, stackalloc int[3])[0],
                    (byte)BracketParser.GetNumbers(line, stackalloc int[3])[1],
                    (byte)BracketParser.GetNumbers(line, stackalloc int[3])[2]
                ),
                Size: BracketParser.GetNumbers(line, stackalloc int[5])[3],
                Depth: BracketParser.GetNumbers(line, stackalloc int[5])[4]
            );
        }

        else if (line.StartsWith("div(")) idiv = BracketParser.GetNumber<int>(line) / (fix64)10f;
        else if (line.StartsWith("idiv(")) idiv = BracketParser.GetNumber<int>(line) / (fix64)100f;
        else if (line.StartsWith("iwid(")) iwid = BracketParser.GetNumber<int>(line) / (fix64)100f;
        else if (line.StartsWith("ScaleX(")) scaleX = BracketParser.GetNumber<int>(line) / (fix64)100f;
        else if (line.StartsWith("ScaleY(")) scaleY = BracketParser.GetNumber<int>(line) / (fix64)100f;
        else if (line.StartsWith("ScaleZ(")) scaleZ = BracketParser.GetNumber<int>(line) / (fix64)100f;

        else if (line.StartsWith("<track>"))
        {
            _boxes.Add(new Rad3dBoxDef(
                Xy: 0,
                Zy: 0,
                Radius: new f64Vector3(),
                Translation: new f64Vector3(),
                SurfaceType: CarPhysics.SurfaceType.Road,
                Damage: 0,
                NotWall: false,
                Color: new Color3()
            ));
        }
        
        // SRC extension
        else if (line.StartsWith("mv("))
        {
            var vec = f64Vector3.FromSpan(BracketParser.GetNumbers(line, stackalloc fix64[3]));
            _meshCollisionVerts.Add(new f64Vector3(
                vec.X * idiv * iwid * scaleX,
                vec.Y * idiv * scaleY,
                vec.Z * idiv * scaleZ
            ));
        }
        
        // SRC extension
        else if (line.StartsWith("mtri("))
        {
            var tri = BracketParser.GetNumbers(line, stackalloc ushort[3]);
            foreach (var idx in tri)
            {
                _meshCollisionIndices.Add(idx);
            }
        }

        // SRC extension
        else if (line.StartsWith("hullv("))
        {
            var vec = f64Vector3.FromSpan(BracketParser.GetNumbers(line, stackalloc fix64[3]));
            _hullVerts.Add(new f64Vector3(
                vec.X * idiv * iwid * scaleX,
                vec.Y * idiv * scaleY,
                vec.Z * idiv * scaleZ
            ));
        }
        
        // NFMW extension
        else if (line.StartsWith("atp("))
        {
            var (x, (z, _)) = BracketParser.GetNumbers(line, stackalloc fix64[2]);
            _atp.Add(new Vector2((float)x, (float)z));
        }
        
        // SRC extension
        else if (line.StartsWith("atline("))
        {
            var (direction, (offset, _)) = BracketParser.GetStrings(line, 2);
            var dir = direction switch
            {
                "x" => AttachmentLineDirection.X,
                "z" => AttachmentLineDirection.Z,
                _ => throw new ArgumentOutOfRangeException(nameof(direction), direction, "Invalid attachment line direction")
            };
            _atLines.Add(new Rad3dAttachmentLine(dir, fix64.Parse(offset, CultureInfo.InvariantCulture)));
        }

        if (_boxes.Count > 0)
        {
            ref var currentBox = ref _boxes.GetValueRef(^1);
            if (line.StartsWith("c("))
            {
                var color = Color3.FromSpan(BracketParser.GetShorts(line, stackalloc short[3]));
                currentBox = currentBox with { Color = color };
            }
            else if (line.StartsWith("xy("))
                currentBox = currentBox with { Xy = BracketParser.GetNumber<int>(line) };
            else if (line.StartsWith("zy("))
                currentBox = currentBox with { Zy = BracketParser.GetNumber<int>(line) };
            else if (line.StartsWith("radx("))
                currentBox = currentBox with
                {
                    Radius = currentBox.Radius with
                    {
                        X = BracketParser.GetNumber<int>(line) * idiv * iwid * scaleX
                    }
                };
            else if (line.StartsWith("rady("))
                currentBox = currentBox with
                {
                    Radius = currentBox.Radius with
                    {
                        Y = BracketParser.GetNumber<int>(line) * idiv * scaleY
                    }
                };
            else if (line.StartsWith("radz("))
                currentBox = currentBox with
                {
                    Radius = currentBox.Radius with
                    {
                        Z = BracketParser.GetNumber<int>(line) * idiv * scaleZ
                    }
                };
            else if (line.StartsWith("tx("))
                currentBox = currentBox with
                {
                    Translation = currentBox.Translation with
                    {
                        X = BracketParser.GetNumber<int>(line) * idiv * iwid * scaleX
                    }
                };
            else if (line.StartsWith("ty("))
                currentBox = currentBox with
                {
                    Translation = currentBox.Translation with
                    {
                        Y = BracketParser.GetNumber<int>(line) * idiv * scaleY
                    }
                };
            else if (line.StartsWith("tz("))
                currentBox = currentBox with
                {
                    Translation = currentBox.Translation with
                    {
                        Z = BracketParser.GetNumber<int>(line) * idiv * scaleZ
                    }
                };
            else if (line.StartsWith("skid("))
                currentBox = currentBox with { SurfaceType = (CarPhysics.SurfaceType)BracketParser.GetNumber<int>(line) };
            else if (line.StartsWith("dam"))
                currentBox = currentBox with { Damage = 3 };
            else if (line.StartsWith("notwall("))
                currentBox = currentBox with { NotWall = true };
            else if (line.StartsWith("gripmul("))
                currentBox = currentBox with { TractionMultiplier = BracketParser.GetNumber<fix64>(line) };
        }

        // SRC custom wheel format
        if (line.StartsWith("<wheel>"))
        {
            _wheelMeshes.Add(new WheelMesh { Polys = _currentPolys = [] });
        }
        else if (line.StartsWith("<wheel") && PhyShotWheelDef.Match(new string(line)) is { Success: true } wheelMatch)
        {
            var radius = int.Parse(wheelMatch.Groups["radius"].ValueSpan);
            var depth = int.Parse(wheelMatch.Groups["depth"].ValueSpan);
            _wheelMeshes.Add(new WheelMesh
            {
                Polys = _currentPolys = [],
                Radius = radius,
                Depth = depth
            });
            _scaleRadius = 1f/radius;
            _scaleDepth = 1f/depth;
        }

        // SRC custom wheel format
        else if (line.StartsWith("</wheel>"))
        {
            _currentPolys = _mainCarPolys;
            _scaleRadius = 1;
            _scaleDepth = 1;
        }

        else if (line.StartsWith("<p>") || line.StartsWith("[p]"))
        {
            _currentPoly = new Rad3dPoly(new Color3(), null, PolyType.Flat, LineType.Flat, 0.0f, []);
            _inPoly = true;
            _noOutline = false;
            _phyAddonsWheelId = null;
        }
        
        if (_inPoly)
        {
            if (line.StartsWith("c(g)")) // SRC extension
            {
                _currentPoly = _currentPoly with { PolyType = PolyType.CGround };
            }
            else if (line.StartsWith("c("))
            {
                var color = Color3.FromSpan(BracketParser.GetShorts(line, stackalloc short[3]));
                _currentPoly = _currentPoly with { Color = color };
                if (_colors.TryGetValue(color, out var colNum))
                {
                    _currentPoly = _currentPoly with { ColNum = colNum };
                }
            }

            else if (line.StartsWith("glass")) _currentPoly = _currentPoly with { PolyType = PolyType.Glass };
            else if (line.StartsWith("lightB")) _currentPoly = _currentPoly with { PolyType = PolyType.BrakeLight };
            else if (line.StartsWith("lightR")) _currentPoly = _currentPoly with { PolyType = PolyType.ReverseLight };
            else if (line.StartsWith("light")) _currentPoly = _currentPoly with { PolyType = PolyType.Light };
            else if (line.StartsWith("gr(-10)")) _currentPoly = _currentPoly with { LineType = LineType.BrightColored };
            else if (line.StartsWith("gr(-18)")) _currentPoly = _currentPoly with { LineType = LineType.Charged };
            else if (line.StartsWith("gr(-13)")) _currentPoly = _currentPoly with { PolyType = PolyType.Finish };
            // SRC extension
            else if (line.StartsWith("proad")) _currentPoly = _currentPoly with { LineType = LineType.Colored };
            // NFMW extension
            else if (line.StartsWith("decal"))
            {
                // Parse decal with optional value: decal or decal(value)
                float decalValue = -1.0f; // default (no offset)
                if (line.Length > 5 && line[5] == '(')
                {
                    decalValue = BracketParser.GetNumber<float>(line);
                }
                _currentPoly = _currentPoly with { DecalOffset = decalValue };
            }
            else if (line.StartsWith("p("))
            {
                var position = Int3.FromSpan(BracketParser.GetNumbers(line, stackalloc int[3]));
                var transformedPoint = new Vector3(
                    position.X * (float)idiv * (float)iwid * (float)scaleX * _scaleDepth,
                    position.Y * (float)idiv * (float)scaleY * _scaleRadius,
                    position.Z * (float)idiv * (float)scaleZ * _scaleRadius
                );
                _points.Add(transformedPoint);
            }
            else if (line.StartsWith("tri("))
            {
                var tri = BracketParser.GetNumbers(line, stackalloc uint[3]);
                _tris.AddRange(tri);
            }
            
            else if (line.StartsWith("noOutline")) _noOutline = true;
            
            else if (line.StartsWith("wheel("))
            {
                _phyAddonsWheelId = BracketParser.GetNumber<int>(line);
            }
            else if (line.StartsWith("wheel"))
            {
                _phyAddonsWheelId = -1;
            }

            else if (line.StartsWith("</p>") || line.StartsWith("[/p]"))
            {
                _currentPoly = _currentPoly.WithPoints(_points.ToArray(), _tris.Count > 0 ? _tris.ToImmutableArray() : null);
                _points.Clear();
                _tris.Clear();
                if (_stonecold || _noOutline)
                {
                    if (_currentPoly.LineType == LineType.Flat)
                    {
                        if (_road)
                        {
                            _currentPoly = _currentPoly with { LineType = LineType.Colored };
                        }
                        else
                        {
                            _currentPoly = _currentPoly with { LineType = null };
                        }
                    }
                }

                if (_phyAddonsWheelId is { } wheelId)
                {
                    if (wheelId != -1 && _phyAddonsWheelMeshes[wheelId] is { } list)
                    {
                        list.Add(_currentPoly);
                    }
                    else
                    {
                        foreach (var otherList in _phyAddonsWheelMeshes)
                        {
                            otherList?.Add(_currentPoly);
                        }
                    }
                }
                else
                {
                    _currentPolys.Add(_currentPoly);
                }
            }
        }
    }
}
