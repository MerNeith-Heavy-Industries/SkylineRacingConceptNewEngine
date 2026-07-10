using System.Collections;
using System.IO.Compression;
using System.Text.RegularExpressions;

namespace NFMWorld.PhysicsFS;

public interface IVirtualFileSystem
{
    Stream? OpenRead(string path);
    Stream? OpenWrite(string path);
    bool FileExists(string path);
    bool DirectoryExists(string path);
    IEnumerable<string> EnumerateFiles(string path, string searchPattern, SearchOption searchOption);
}

public class VirtualFS
{
    private readonly List<IVirtualFileSystem> _readMounts = [];
    private string? _writeRoot;

    // ── Mounting ──────────────────────────────────────────────

    public void MountDirectory(string root)
    {
        _readMounts.Add(new DirectoryFS(root));
    }

    public void MountZip(string zipFile)
    {
        _readMounts.Add(new ZipFS(zipFile));
    }

    public void MountZip(Memory<byte> zipBytes)
    {
        _readMounts.Add(new ZipFS(zipBytes));
    }

    public void Mount(IVirtualFileSystem fs)
    {
        _readMounts.Add(fs);
    }

    public void MountWriteDestination(string root)
    {
        _writeRoot = Path.GetFullPath(root);
        Directory.CreateDirectory(_writeRoot);
    }

    // ── Query ─────────────────────────────────────────────────

    public IEnumerable<string> EnumerateFiles(
        string path,
        string searchPattern = "*.*",
        SearchOption searchOption = SearchOption.TopDirectoryOnly)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Read mounts (first match wins)
        foreach (var mount in _readMounts)
        {
            foreach (var file in mount.EnumerateFiles(path, searchPattern, searchOption))
            {
                if (seen.Add(file))
                    yield return file;
            }
        }

        // Write root (lowest priority)
        if (_writeRoot is not null)
        {
            var fullPath = Path.Combine(_writeRoot, path);
            if (Directory.Exists(fullPath))
            {
                foreach (var file in Directory.EnumerateFiles(fullPath, searchPattern, searchOption))
                {
                    var relative = Path.GetRelativePath(_writeRoot, file).Replace("\\", "/");
                    if (seen.Add(relative))
                        yield return relative;
                }
            }
        }
    }

    public bool FileExists(string path)
    {
        foreach (var mount in _readMounts)
        {
            if (mount.FileExists(path))
                return true;
        }

        if (_writeRoot is not null && File.Exists(Path.Combine(_writeRoot, path)))
            return true;

        return false;
    }

    public bool DirectoryExists(string path)
    {
        foreach (var mount in _readMounts)
        {
            if (mount.DirectoryExists(path))
                return true;
        }

        if (_writeRoot is not null && Directory.Exists(Path.Combine(_writeRoot, path)))
            return true;

        return false;
    }

    public void CreateDirectory(string path)
    {
        if (_writeRoot is null)
            throw new InvalidOperationException(
                "No write destination mounted. Call MountWriteDestination first.");

        Directory.CreateDirectory(Path.Combine(_writeRoot, path));
    }

    // ── I/O ───────────────────────────────────────────────────

    public Stream OpenRead(string path)
    {
        foreach (var mount in _readMounts)
        {
            var stream = mount.OpenRead(path);
            if (stream is not null)
                return stream;
        }

        // Also check write root (so you can read back what you wrote)
        if (_writeRoot is not null)
        {
            var fullPath = Path.Combine(_writeRoot, path);
            if (File.Exists(fullPath))
                return File.OpenRead(fullPath);
        }

        throw new FileNotFoundException(
            $"File not found in any mounted filesystem: {path}");
    }

    public Stream OpenWrite(string path)
    {
        if (_writeRoot is null)
            throw new InvalidOperationException(
                "No write destination mounted. Call MountWriteDestination first.");

        var fullPath = Path.Combine(_writeRoot, path);
        var dir = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        return File.Open(fullPath, FileMode.Create, FileAccess.Write, FileShare.Read);
    }

    public byte[] ReadAllBytes(string path)
    {
        using var stream = OpenRead(path);
        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        return ms.ToArray();
    }

    public string ReadAllText(string path)
    {
        using var stream = OpenRead(path);
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    public IEnumerable<string> ReadAllLines(string path)
    {
        using var stream = OpenRead(path);
        using var reader = new StreamReader(stream);
        while (!reader.EndOfStream)
        {
            yield return reader.ReadLine()!;
        }
    }
}

internal sealed class DirectoryFS : IVirtualFileSystem
{
    private readonly string _root;

    public DirectoryFS(string root)
    {
        _root = Path.GetFullPath(root);

        if (!Directory.Exists(_root))
            throw new DirectoryNotFoundException($"Directory not found: {root}");
    }

    public Stream? OpenRead(string path)
    {
        var fullPath = Resolve(path);
        return fullPath is not null && File.Exists(fullPath)
            ? File.OpenRead(fullPath)
            : null;
    }

    public Stream? OpenWrite(string path)
    {
        var fullPath = Resolve(path);
        if (fullPath is null) return null;

        var dir = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        return File.Open(fullPath, FileMode.Create, FileAccess.Write, FileShare.Read);
    }

    public bool FileExists(string path)
    {
        var fullPath = Resolve(path);
        return fullPath is not null && File.Exists(fullPath);
    }

    public bool DirectoryExists(string path)
    {
        var fullPath = Resolve(path);
        return fullPath is not null && Directory.Exists(fullPath);
    }

    public IEnumerable<string> EnumerateFiles(
        string path, string searchPattern, SearchOption searchOption)
    {
        var fullPath = Resolve(path);
        if (fullPath is null || !Directory.Exists(fullPath))
            yield break;

        foreach (var file in Directory.EnumerateFiles(fullPath, searchPattern, searchOption))
            yield return Path.GetRelativePath(_root, file).Replace("\\", "/");
    }

    /// <summary>
    /// Resolves a virtual path and validates it stays within the mount root.
    /// Returns null if the path escapes the root (path traversal attempt).
    /// </summary>
    private string? Resolve(string path)
    {
        var full = Path.GetFullPath(Path.Combine(_root, path));
        return full.StartsWith(_root, StringComparison.OrdinalIgnoreCase)
            ? full
            : null;
    }
}

internal sealed class ZipFS : IVirtualFileSystem
{
    private readonly ZipArchive _archive;
    private readonly Stream? _backingStream; // kept alive for in-memory zips
    private readonly HashSet<string> _entries;
    private readonly HashSet<string> _directories;

    public ZipFS(string zipPath)
    {
        if (!File.Exists(zipPath))
            throw new FileNotFoundException($"Zip file not found: {zipPath}");

        _archive = ZipFile.OpenRead(zipPath);
        (_entries, _directories) = BuildIndex();
    }

    public ZipFS(Memory<byte> zipBytes)
    {
        _backingStream = new MemoryStream(zipBytes.ToArray());
        _archive = new ZipArchive(_backingStream, ZipArchiveMode.Read, leaveOpen: true);
        (_entries, _directories) = BuildIndex();
    }

    private (HashSet<string> entries, HashSet<string> dirs) BuildIndex()
    {
        var entries = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var dirs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var entry in _archive.Entries)
        {
            var fullName = entry.FullName.Replace('\\', '/').TrimEnd('/');
            if (fullName.Length == 0) continue;

            if (string.IsNullOrEmpty(entry.Name))
            {
                // Directory entry (e.g. "fonts/")
                dirs.Add(fullName);
            }
            else
            {
                // File entry
                entries.Add(fullName);
            }

            // Walk up and register all ancestor directories
            var parent = GetParentDirectory(fullName);
            while (parent is not null)
            {
                dirs.Add(parent);
                parent = GetParentDirectory(parent);
            }
        }

        return (entries, dirs);

        static string? GetParentDirectory(string path)
        {
            var idx = path.LastIndexOf('/');
            return idx > 0 ? path[..idx] : null;
        }
    }

    // ── IVirtualFileSystem ────────────────────────────────────

    public Stream? OpenRead(string path)
    {
        var normalized = Normalize(path);
        var entry = _archive.GetEntry(normalized);
        if (entry is null) return null;

        // Copy to a MemoryStream so callers get an independent, seekable stream.
        // The ZipArchive entry stream must be disposed promptly.
        using var src = entry.Open();
        var ms = new MemoryStream((int)entry.Length);
        src.CopyTo(ms);
        ms.Position = 0;
        return ms;
    }

    public Stream? OpenWrite(string path) => null; // Read-only

    public bool FileExists(string path)
    {
        return _entries.Contains(Normalize(path));
    }

    public bool DirectoryExists(string path)
    {
        var normalized = Normalize(path).TrimEnd('/');
        return normalized.Length == 0 || _directories.Contains(normalized);
    }

    public IEnumerable<string> EnumerateFiles(
        string path, string searchPattern, SearchOption searchOption)
    {
        var prefix = Normalize(path).TrimEnd('/');
        if (prefix.Length > 0) prefix += "/";

        var regex = WildcardToRegex(searchPattern);

        foreach (var entry in _entries)
        {
            if (!entry.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                continue;

            var relative = entry[prefix.Length..];

            // TopDirectoryOnly: skip entries nested in subdirectories
            if (searchOption == SearchOption.TopDirectoryOnly && relative.Contains('/'))
                continue;

            // Match the filename portion against the search pattern
            var fileName = searchOption == SearchOption.AllDirectories
                ? Path.GetFileName(entry)   // final segment
                : relative;                 // already just the filename

            if (regex.IsMatch(fileName))
                yield return entry;
        }
    }

    // ── Helpers ───────────────────────────────────────────────

    private static string Normalize(string path) =>
        path.Replace('\\', '/').TrimStart('/');

    private static Regex WildcardToRegex(string pattern)
    {
        // Escape regex metacharacters, then translate * → .*  and ? → .
        var escaped = Regex.Escape(pattern)
            .Replace(@"\*", ".*")
            .Replace(@"\?", ".");
        return new Regex($"^{escaped}$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    }
}