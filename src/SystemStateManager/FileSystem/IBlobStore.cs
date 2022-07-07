using DevOptimal.SystemUtilities.FileSystem;

namespace DevOptimal.SystemStateManager.FileSystem
{
    public interface IBlobStore
    {
        /// <summary>
        /// The abstraction used to access the file system.
        /// </summary>
        IFileSystem FileSystem { get; }

        /// <summary>
        /// Pulls a file from the file cache and saves it to a destination location.
        /// </summary>
        /// <param name="id">The ID of the content to download.</param>
        /// <param name="destinationPath">The location to download the file to.</param>
        void DownloadFile(string id, string destinationPath);

        /// <summary>
        /// Uploads a file to the blob store and returns an ID for its content.
        /// </summary>
        /// <param name="sourcePath">The file to upload to the blob store.</param>
        /// <returns>A unique identifier for the file's content.</returns>
        string UploadFile(string sourcePath);
    }
}
