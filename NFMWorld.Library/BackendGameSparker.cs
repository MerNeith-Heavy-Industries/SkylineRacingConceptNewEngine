using System.Text;
using NFMWorld.Mad;
using NFMWorld.Util;

namespace NFMWorld.Library;

public class BackendGameSparker
{
    public static Dictionary<Collection, UnlimitedArray<Rad3d>> cars = new();
    public static UnlimitedArray<Rad3d> stage_parts = [];
    public static UnlimitedArray<Rad3d> vendor_stage_parts = [];
    public static UnlimitedArray<Rad3d> user_stage_parts = [];
    public static Rad3d error_mesh;

    public static readonly string[] CarRads =
    {
        "2000tornados", "formula7", "canyenaro", "lescrab", "nimi", "maxrevenge", "leadoxide", "koolkat", "drifter",
        "policecops", "mustang", "king", "audir8", "masheen", "radicalone", "drmonster"
    };

    public static readonly string[] StageRads =
    {
        "road", "froad", "twister2", "twister1", "turn", "offroad", "bumproad", "offturn", "nroad", "nturn",
        "roblend", "noblend", "rnblend", "roadend", "offroadend", "hpground", "ramp30", "cramp35", "dramp15",
        "dhilo15", "slide10", "takeoff", "sramp22", "offbump", "offramp", "sofframp", "halfpipe", "spikes", "rail",
        "thewall", "checkpoint", "fixpoint", "offcheckpoint", "sideoff", "bsideoff", "uprise", //45
        "riseroad",
        "sroad",
        "soffroad", "tside", "launchpad", "thenet", "speedramp", "offhill", "slider", "uphill", "roll1", "roll2",
        "roll3", "roll4", "roll5", "roll6", "opile1", "opile2", "aircheckpoint", "tree1", "tree2", "tree3", "tree4",
        "tree5", "tree6", "tree7", "tree8", "cac1", "cac2", "cac3", "8sroad", "8soffroad"
    };

    public static void Load()
    {
        cars.Add(Collection.NFMM, []);
        FileUtil.LoadFiles("./data/models/nfmm/cars", CarRads, (ais, id, fileName) =>
        {
            cars[Collection.NFMM][id] = RadParser.ParseRad(Encoding.UTF8.GetString(ais)) with
            {
                FileName = "nfmm/" + fileName
            };
        });

        FileUtil.LoadFiles("./data/models/nfmm/stage", StageRads, (ais, id, fileName) =>
        {
            stage_parts[id] = RadParser.ParseRad(Encoding.UTF8.GetString(ais)) with
            {
                FileName = "nfmm/" + fileName
            };
        });

        cars.Add(Collection.World, []);
        FileUtil.LoadFiles("./data/models/world/cars", (ais, fileName) =>
        {
            cars[Collection.World].Add(RadParser.ParseRad(Encoding.UTF8.GetString(ais)) with
            {
                FileName = "world/" + fileName
            });
        });

        cars.Add(Collection.Elo, []);
        FileUtil.LoadFiles("./data/models/elo/cars", (ais, fileName) =>
        {
            cars[Collection.Elo].Add(RadParser.ParseRad(Encoding.UTF8.GetString(ais)) with
            {
                FileName = "elo/" + fileName
            });
        });

        cars.Add(Collection.Football, []);
        FileUtil.LoadFiles("./data/models/football/cars", (ais, fileName) =>
        {
            cars[Collection.Football].Add(RadParser.ParseRad(Encoding.UTF8.GetString(ais)) with
            {
                FileName = "football/" + fileName
            });
        });

        FileUtil.LoadFiles("./data/models/world/stage", (ais, fileName) =>
        {
            vendor_stage_parts.Add(RadParser.ParseRad(Encoding.UTF8.GetString(ais)) with
            {
                FileName = "world/" + fileName
            });
        });

        FileUtil.LoadFiles("./data/models/football/stage", (ais, fileName) =>
        {
            vendor_stage_parts.Add(RadParser.ParseRad(Encoding.UTF8.GetString(ais)) with
            {
                FileName = "football/" + fileName
            });
        });

        cars.Add(Collection.User, []);
        FileUtil.LoadFiles("./data/models/user/cars", (ais, fileName) =>
        {
            try
            {
                cars[Collection.User].Add(RadParser.ParseRad(Encoding.UTF8.GetString(ais))with
                {
                    FileName = "user/" + fileName
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading user car '{fileName}': {ex.Message}\n{ex.StackTrace}");
            }
        });

        FileUtil.LoadFiles("./data/models/user/stage", (ais, fileName) =>
        {
            try
            {
                user_stage_parts.Add(RadParser.ParseRad(Encoding.UTF8.GetString(ais)) with
                {
                    FileName = "user/" + fileName
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading user stage part '{fileName}': {ex.Message}\n{ex.StackTrace}");
            }
        });

        error_mesh = RadParser.ParseRad(Encoding.UTF8.GetString(System.IO.File.ReadAllBytes("./data/models/error.rad"))) with
        {
            FileName = "error.rad"
        };
        
        for (var i = 0; i < StageRads.Length; i++) {
            if (stage_parts[i] == null) {
                throw new Exception("No valid ContO (Stage Part) has been assigned to ID " + i + " (" + StageRads[i] + ")");
            }
        }
        for (var i = 0; i < CarRads.Length; i++) {
            if (cars[Collection.NFMM][i] == null)
            {
                throw new Exception("No valid ContO (Vehicle) has been assigned to ID " + i + " (" + StageRads[i] + ")");
            }
        }
    }

    public static (int Id, Rad3d? Rad) GetCar(string name)
    {
        var total = 0;
        foreach (var t in cars.Values)
        {
            foreach (var car in t)
            {
                if (string.Equals(car.FileName, name, StringComparison.OrdinalIgnoreCase))
                {
                    return (total, car);
                }

                total++;
            }
        }

        Console.WriteLine("No results for GetCar");
        return (-1, null!);
    }

    public static (int Id, Rad3d? Rad) GetStagePart(string name)
    {
        IReadOnlyList<Rad3d>[] arrays = [stage_parts, vendor_stage_parts, user_stage_parts];

        var total = 0;
        foreach (var t in arrays)
        {
            foreach (var part in t)
            {
                if (string.Equals(part.FileName, name, StringComparison.OrdinalIgnoreCase))
                {
                    return (total, part);
                }

                total++;
            }
        }

        Console.WriteLine("No results for GetStagePart");
        return (-1, null!);
    }
    
    public static string GetModelName(int index, bool forCar = false)
    {
        var models = forCar ? CarRads : StageRads;
        
        if (index >= 0 && index < models.Length)
        {
            return models[index];
        }
        
        return "";
    }
}