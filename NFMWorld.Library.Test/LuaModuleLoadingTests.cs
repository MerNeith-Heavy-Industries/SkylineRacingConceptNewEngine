using NFMWorld.PhysicsFS;
using NFMWorldLibrary.Radpack;
using NFMWorldLibrary.Util;
using NuLua;
using NuLua.Luau;

namespace NFMWorldLibrary.Test;

/// <summary>
/// Tests for the game-layer Luau require-by-string implementation
/// (<see cref="LuaModuleLoading"/> / <see cref="RequireResolver"/> / <see cref="IModuleSource"/>).
/// Mirrors the legacy Lua-CSharp <c>RequireByStringTests</c> plus radpack, alias and caching cases.
/// </summary>
[TestClass]
public class LuaModuleLoadingTests
{
    // ── Helpers ───────────────────────────────────────────────────

    static string CreateTempDir()
    {
        var root = Path.Combine(Path.GetTempPath(), $"nfm-require-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        return root;
    }

    static void WriteFile(string root, string relative, string content)
    {
        var full = Path.Combine(root, relative.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(full)!);
        File.WriteAllText(full, content);
    }

    /// <summary>Creates a non-sandboxed state with require installed and a VFS source rooted at <c>data</c>.</summary>
    static (LuauState State, string Root) CreateVfsState(params (string Path, string Content)[] files)
    {
        var root = CreateTempDir();
        foreach (var (path, content) in files)
        {
            WriteFile(root, path, content);
        }
        var vfs = new VirtualFS();
        vfs.MountDirectory(root);
        var state = LuauState.Create();
        state.OpenLibraries();
        LuaModuleLoading.Install(state, new VfsModuleSource(vfs, "vfs"));
        return (state, root);
    }

    static RadpackLua CreateRadpack(string name, Dictionary<string, string> files)
    {
        return new RadpackLua
        {
            Metadata = new RadpackMetadata { Name = name, CreationDate = DateTimeOffset.UtcNow },
            Kind = LuaScriptKind.Gamemode,
            Files = files,
        };
    }

    /// <summary>Creates a state with require installed, a default VFS source, and a registered radpack source.</summary>
    static (LuauState State, RadpackLua Radpack) CreateRadpackState(string id, Dictionary<string, string> files)
    {
        var state = LuauState.Create();
        state.OpenLibraries();
        LuaModuleLoading.Install(state);
        var radpack = CreateRadpack(id, files);
        LuaModuleLoading.RegisterRadpackSource(state, radpack, id);
        return (state, radpack);
    }

    // ── VFS relative resolution ───────────────────────────────────

    [TestMethod]
    public void Require_SiblingRelative_ResolvesAgainstRequiringFile()
    {
        var (state, _) = CreateVfsState(
            ("data/lib/main.luau", "return require('./dep')"),
            ("data/lib/dep.lua", "return 42"));

        using (state)
        {
            var results = state.DoString("return require('lib/main')", "entry.luau");
            Assert.AreEqual(42, results[0].Read<double>());
        }
    }

    [TestMethod]
    public void Require_ParentRelative_ResolvesParentDirectory()
    {
        var (state, _) = CreateVfsState(
            ("data/lib/sub/main.luau", "return require('../shared')"),
            ("data/lib/shared.lua", "return 'ok'"));

        using (state)
        {
            var results = state.DoString("return require('lib/sub/main')", "entry.luau");
            Assert.AreEqual("ok", results[0].Read<string>());
        }
    }

    [TestMethod]
    public void Require_OnlyLuauExists_ResolvesLuau()
    {
        var (state, _) = CreateVfsState(
            ("data/dir/main.luau", "return require('./mod')"),
            ("data/dir/mod.luau", "return 'luau'"));

        using (state)
        {
            var results = state.DoString("return require('dir/main')", "entry.luau");
            Assert.AreEqual("luau", results[0].Read<string>());
        }
    }

    [TestMethod]
    public void Require_OnlyLuaExists_ResolvesLua()
    {
        var (state, _) = CreateVfsState(
            ("data/dir/main.luau", "return require('./mod')"),
            ("data/dir/mod.lua", "return 'lua'"));

        using (state)
        {
            var results = state.DoString("return require('dir/main')", "entry.luau");
            Assert.AreEqual("lua", results[0].Read<string>());
        }
    }

    [TestMethod]
    public void Require_AmbiguousExtension_Throws()
    {
        var (state, _) = CreateVfsState(
            ("data/dir/main.luau", "return require('./mod')"),
            ("data/dir/mod.luau", "return 1"),
            ("data/dir/mod.lua", "return 2"));

        using (state)
        {
            Assert.ThrowsExactly<LuaException>(
                () => state.DoString("return require('dir/main')", "entry.luau"));
        }
    }

    [TestMethod]
    public void Require_DirectoryInit_ResolvesInitLua()
    {
        var (state, _) = CreateVfsState(
            ("data/dir/main.luau", "return require('./pkg')"),
            ("data/dir/pkg/init.lua", "return 7"));

        using (state)
        {
            var results = state.DoString("return require('dir/main')", "entry.luau");
            Assert.AreEqual(7, results[0].Read<double>());
        }
    }

    [TestMethod]
    public void Require_NestedRelative_Chains()
    {
        var (state, _) = CreateVfsState(
            ("data/a/main.luau", "return require('./b')"),
            ("data/a/b.lua", "return require('./c')"),
            ("data/a/c.lua", "return 99"));

        using (state)
        {
            var results = state.DoString("return require('a/main')", "entry.luau");
            Assert.AreEqual(99, results[0].Read<double>());
        }
    }

    [TestMethod]
    public void Require_NonFileChunk_Throws()
    {
        var (state, _) = CreateVfsState();
        using (state)
        {
            Assert.ThrowsExactly<LuaException>(
                () => state.DoString("return require('./x')", "=stdin"));
        }
    }

    // ── Aliases and root resolution ───────────────────────────────

    [TestMethod]
    public void Require_Alias_ResolvesToDataRoot()
    {
        var (state, _) = CreateVfsState(
            ("data/mod.lua", "return 5"));

        using (state)
        {
            var results = state.DoString("return require('@/mod')", "entry.luau");
            Assert.AreEqual(5, results[0].Read<double>());
        }
    }

    [TestMethod]
    public void Require_NonRelative_ResolvesAtDataRoot()
    {
        var (state, _) = CreateVfsState(
            ("data/gamemodes/pvp/util.luau", "return 3"));

        using (state)
        {
            var results = state.DoString("return require('gamemodes/pvp/util')", "entry.luau");
            Assert.AreEqual(3, results[0].Read<double>());
        }
    }

    // ── Caching and callback-time require ─────────────────────────

    [TestMethod]
    public void Require_Caching_ReturnsSameInstanceOnce()
    {
        var (state, _) = CreateVfsState(
            ("data/counter.lua", "_G._count = (_G._count or 0) + 1; return _G._count"));

        using (state)
        {
            var results = state.DoString(
                "local a = require('counter'); local b = require('counter'); return a == b, _G._count",
                "entry.luau");
            Assert.IsTrue(results[0].Read<bool>());
            Assert.AreEqual(1, results[1].Read<double>());
        }
    }

    [TestMethod]
    public void Require_FromCallback_ResolvesAgainstDefiningChunk()
    {
        var (state, _) = CreateVfsState(
            ("data/main/main.luau", "return { tick = function() return require('./dep') end }"),
            ("data/main/dep.luau", "return 42"));

        using (state)
        {
            var results = state.DoString("return require('main/main')", "entry.luau");
            state["m"] = results[0];
            var tick = state.DoString("return m.tick()", "entry.luau");
            Assert.AreEqual(42, tick[0].Read<double>());
        }
    }

    // ── Radpack resolution ────────────────────────────────────────

    [TestMethod]
    public void Radpack_RequireRelative_ResolvesInRadpack()
    {
        var (state, radpack) = CreateRadpackState("pvp", new()
        {
            ["client"] = "return require('./dep')",
            ["dep"] = "return 42",
        });

        using (state)
        {
            var results = state.DoString(radpack.Files["client"], "@radpack/pvp/client");
            Assert.AreEqual(42, results[0].Read<double>());
        }
    }

    [TestMethod]
    public void Radpack_RequireDotted_ResolvesNonRelative()
    {
        var (state, radpack) = CreateRadpackState("pvp", new()
        {
            ["client"] = "return require('pvp.util')",
            ["pvp.util"] = "return 7",
        });

        using (state)
        {
            var results = state.DoString(radpack.Files["client"], "@radpack/pvp/client");
            Assert.AreEqual(7, results[0].Read<double>());
        }
    }

    [TestMethod]
    public void Radpack_RequireRelative_InDottedSubdir()
    {
        var (state, radpack) = CreateRadpackState("pvp", new()
        {
            ["pvp.client"] = "return require('./helper')",
            ["pvp.helper"] = "return 9",
        });

        using (state)
        {
            var results = state.DoString(radpack.Files["pvp.client"], "@radpack/pvp/pvp.client");
            Assert.AreEqual(9, results[0].Read<double>());
        }
    }

    [TestMethod]
    public void Radpack_RequireInit_ResolvesDottedInit()
    {
        var (state, radpack) = CreateRadpackState("pvp", new()
        {
            ["client"] = "return require('pkg')",
            ["pkg.init.luau"] = "return 7",
        });

        using (state)
        {
            var results = state.DoString(radpack.Files["client"], "@radpack/pvp/client");
            Assert.AreEqual(7, results[0].Read<double>());
        }
    }

    [TestMethod]
    public void Radpack_RequireAmbiguous_Throws()
    {
        var (state, radpack) = CreateRadpackState("pvp", new()
        {
            ["client"] = "return require('mod')",
            ["mod.luau"] = "return 1",
            ["mod.lua"] = "return 2",
        });

        using (state)
        {
            Assert.ThrowsExactly<LuaException>(
                () => state.DoString(radpack.Files["client"], "@radpack/pvp/client"));
        }
    }

    [TestMethod]
    public void Radpack_Alias_ResolvesToRadpackRoot()
    {
        var (state, radpack) = CreateRadpackState("pvp", new()
        {
            ["client"] = "return require('@/util')",
            ["util"] = "return 5",
        });

        using (state)
        {
            var results = state.DoString(radpack.Files["client"], "@radpack/pvp/client");
            Assert.AreEqual(5, results[0].Read<double>());
        }
    }

    // ── Game entry points ─────────────────────────────────────────

    [TestMethod]
    public void OpenState_CanSetGlobalsAndInstallRequire()
    {
        // Exercises the real game path: game state creation + global writes + require install.
        using var state = LuaHelpers.OpenState();
        state["test"] = LuaRefValue.FromNumber(1);
        var results = state.DoString("return test", "chunk");
        Assert.AreEqual(1, results[0].Read<double>());
    }

    [TestMethod]
    public void DoFile_LoadsFromMountedVfs()
    {
        var root = CreateTempDir();
        WriteFile(root, "data/test_do.luau", "return 11");
        TheVFS.VFS.MountDirectory(root);

        using var state = LuauState.Create();
        state.OpenLibraries();
        LuaModuleLoading.Install(state);

        var results = state.DoFile("data/test_do.luau");
        Assert.AreEqual(11, results[0].Read<double>());
    }
}
