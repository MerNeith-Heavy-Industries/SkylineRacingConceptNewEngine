namespace NFMWorldLibrary.Util;

/// <summary>
/// Shared path-combination logic for Luau require-by-string. Handles <c>.</c>, <c>..</c>
/// and empty segments, operating in a source's key space (slash-separated for VFS,
/// dot-separated for radpacks). Mirrors the legacy Lua-CSharp
/// <c>ModuleLibrary.CombineRelativePath</c>.
/// </summary>
internal static class PathResolver
{
    /// <summary>
    /// Resolves <paramref name="relative"/> (a <c>./</c>- or <c>../</c>-prefixed name)
    /// against the directory containing <paramref name="callerKey"/>, joining segments with
    /// <paramref name="separator"/>.
    /// </summary>
    public static bool CombineRelative(string callerKey, string relative, char separator, out string key)
    {
        // The caller's key is a full module identifier (path or dotted key). Its directory is
        // the caller key minus the last segment (the file name).
        var lastSeparator = callerKey.LastIndexOf(separator);
        var baseDir = lastSeparator < 0 ? string.Empty : callerKey[..lastSeparator];

        var segments = new List<string>();
        if (baseDir.Length > 0)
        {
            segments.AddRange(baseDir.Split(separator));
        }

        // The relative argument always uses '/' (Lua convention) regardless of the source key
        // separator; radpack sources convert the result to '.' when joining below.
        foreach (var part in relative.Split('/'))
        {
            switch (part)
            {
                case "":
                case ".":
                    continue;
                case "..":
                    if (segments.Count > 0)
                    {
                        segments.RemoveAt(segments.Count - 1);
                    }
                    break;
                default:
                    segments.Add(part);
                    break;
            }
        }

        key = segments.Count == 0 ? string.Empty : string.Join(separator, segments);
        return true;
    }
}
