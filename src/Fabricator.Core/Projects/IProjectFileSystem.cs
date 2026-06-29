namespace Fabricator.Core.Projects;

public interface IProjectFileSystem
{
    bool DirectoryExists(string path);

    bool FileExists(string path);

    void CreateDirectory(string path);

    void DeleteDirectory(string path, bool recursive);

    void WriteAllText(string path, string contents);
}
