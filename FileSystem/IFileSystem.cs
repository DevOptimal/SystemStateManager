namespace FileSystem
{
    public interface IFileSystem
    {
        void CreateDirectory(string path);

        void DeleteDirectory(string path, bool recursive);

        bool DirectoryExists(string path);

        void DeleteFile(string path);

        bool FileExists(string path);

        void CopyFile(string sourcePath, string destinationPath, bool overwrite);
    }
}
