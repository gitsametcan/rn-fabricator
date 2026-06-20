namespace Fabricator.Core.Projects;

public sealed class SystemProjectFileSystem : IProjectFileSystem
{
    public bool DirectoryExists(string path)
    {
        return Directory.Exists(path);
    }

    public bool FileExists(string path)
    {
        return File.Exists(path);
    }

    public void CreateDirectory(string path)
    {
        Directory.CreateDirectory(path);
    }

    public void DeleteDirectory(string path, bool recursive)
    {
        Directory.Delete(path, recursive);
    }

    public void WriteAllText(string path, string contents)
    {
        File.WriteAllText(path, contents);
    }
}
