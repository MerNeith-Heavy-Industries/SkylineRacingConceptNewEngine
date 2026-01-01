namespace NFMWorld.Util;

public struct File
{
    public string Path { get; }

    public File(string str)
    {
        Path = str;
    }

    public File(File parent, string child)
    {
        Path = System.IO.Path.Combine(parent.Path, child);
    }

    public File Parent => new File(_getParent());
    public string NameWithoutExtension => System.IO.Path.GetFileNameWithoutExtension(Path);

    private string _getParent() => System.IO.Path.GetDirectoryName(Path);

    public bool Exists() => System.IO.File.Exists(Path);
}