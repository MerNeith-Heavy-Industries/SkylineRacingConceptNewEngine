using NFMWorld.PhysicsFS;
using NFMWorldLibrary.Util;

namespace NFMWorldLibrary.Test;

/// <summary>Pure-C# tests of the require resolution rules, independent of the Lua VM.</summary>
[TestClass]
public class RequireResolverTests
{
    static (VfsModuleSource Source, string Root) CreateVfs(params (string Path, string Content)[] files)
    {
        var root = Path.Combine(Path.GetTempPath(), $"nfm-resolve-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        foreach (var (path, content) in files)
        {
            var full = Path.Combine(root, path.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(Path.GetDirectoryName(full)!);
            File.WriteAllText(full, content);
        }
        var vfs = new VirtualFS();
        vfs.MountDirectory(root);
        return (new VfsModuleSource(vfs, "vfs"), root);
    }

    [TestMethod]
    public void Resolve_SiblingRelative_ReturnsDepLua()
    {
        var (source, _) = CreateVfs(
            ("data/lib/main.luau", "x"),
            ("data/lib/dep.lua", "y"));
        var key = RequireResolver.Resolve(source, "./dep", "data/lib/main.luau");
        Assert.AreEqual("data/lib/dep.lua", key);
    }

    [TestMethod]
    public void Resolve_NonRelative_ReturnsMainLuau()
    {
        var (source, _) = CreateVfs(("data/lib/main.luau", "x"));
        var key = RequireResolver.Resolve(source, "lib/main", "entry.luau");
        Assert.AreEqual("data/lib/main.luau", key);
    }

    [TestMethod]
    public void Resolve_Alias_ReturnsDataRoot()
    {
        var (source, _) = CreateVfs(("data/mod.lua", "x"));
        var key = RequireResolver.Resolve(source, "@/mod", null);
        Assert.AreEqual("data/mod.lua", key);
    }

    [TestMethod]
    public void Resolve_Ambiguous_Throws()
    {
        var (source, _) = CreateVfs(
            ("data/mod.lua", "x"),
            ("data/mod.luau", "y"));
        Assert.ThrowsExactly<InvalidOperationException>(() => RequireResolver.Resolve(source, "mod", null));
    }

    [TestMethod]
    public void Resolve_Missing_ReturnsNull()
    {
        var (source, _) = CreateVfs(("data/mod.lua", "x"));
        var key = RequireResolver.Resolve(source, "nope", null);
        Assert.IsNull(key);
    }
}
