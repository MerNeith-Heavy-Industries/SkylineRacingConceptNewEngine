namespace NFMWorld.PhysicsFS
{
    public interface IVirtualFileSystem
    {
        Stream? OpenRead(string path);
        Stream? OpenWrite(string path);
        bool FileExists(string path);
        bool DirectoryExists(string path);
        IEnumerable<string> EnumerateFiles(string path, string searchPattern, SearchOption searchOption);
    }
}