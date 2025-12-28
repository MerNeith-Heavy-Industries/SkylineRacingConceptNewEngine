using System.Collections;
using System.IO.Compression;
using Maxine.Extensions;
using MessagePack;
using NFMWorld.Mad;
using NFMWorld.Mad.packets;
using NFMWorld.Util;

[MessagePackObject]
public class SavedTimeTrial
{
    [Key(0)] public string CarName;
    [Key(1)] public string StageName;
    [Key(2)] public Demo DemoData;
    [Key(3)] public Splits Splits;

    public static string GetDirName(string carName, string stageName)
    {
        return new FileInfo(GetPathName(carName, stageName)).Directory?.FullName ?? "";
    }

    public static string GetPathName(string carName, string stageName)
    {
        return "data/tts/" + stageName + "/" + carName + ".timetrial";
    }

    public SavedTimeTrial(string carName, string stageName)
    {
        CarName = carName;
        StageName = stageName;

        DemoData = new Demo()
        {
            Ticks = new List<DemoEntry>()
        };
        Splits = new Splits()
        {
            SplitTimes = new List<long>()
        };
    }

    public static SavedTimeTrial? Load(string carName, string stageName)
    {
        if (System.IO.File.Exists(GetPathName(carName, stageName)))
        {
            using var fileStream = System.IO.File.OpenRead(GetPathName(carName, stageName));
            using var compressedStream = new DeflateStream(fileStream, CompressionMode.Decompress);
            return MessagePackSerializer.Deserialize<SavedTimeTrial>(compressedStream, MsgPackHelpers.Options);
        }
        return null;
    }

    public void Save()
    {
        if (!Directory.Exists(GetDirName(CarName, StageName)))
        {
            Directory.CreateDirectory(GetDirName(CarName, StageName));
        }

        // compress file using DeflateStream

        using var fileStream = System.IO.File.OpenWrite(GetPathName(CarName, StageName));
        using var deflateStream = new DeflateStream(fileStream, CompressionMode.Compress);

        MessagePackSerializer.Serialize(deflateStream, this, MsgPackHelpers.Options);
    }

    public void RecordTick(InGameCar car, int checkpointInLap, int lap)
    {
        DemoEntry entry = DemoEntry.Create(car, checkpointInLap, lap);
        DemoData.AddEntry(entry);
    }
    public (bool Up, bool Down, bool Left, bool Right, bool Handb)? GetTick(int tick)
    {
        if(tick >= DemoData.Ticks.Count) return null;
        var tickData = DemoData.GetEntry(tick);
        return (tickData.TheBitFlags.Up, tickData.TheBitFlags.Down, tickData.TheBitFlags.Left, tickData.TheBitFlags.Right, tickData.TheBitFlags.Handb);
    }

    public void RecordSplit(long elapsed)
    {
        Splits.SplitTimes.Add(elapsed);
    }

    public long GetSplitDiff(SavedTimeTrial other, int sample)
    {
        return Splits.SplitTimes[sample] - other.Splits.SplitTimes[sample];
    }
}