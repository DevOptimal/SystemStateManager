namespace MachineStateManager.FileSystem
{
    internal interface IBlobStore
    {
        /// <summary>
        /// Pushes a file to the file cache and returns a hash of its content.
        /// </summary>
        /// <param name="sourcePath">The file to push to the file cache.</param>
        /// <returns>A hash of the file's content.</returns>
        string UploadFile(string sourcePath);

        /// <summary>
        /// Pulls a file from the file cache and saves it to a destination location.
        /// </summary>
        /// <param name="hash">The hash of the content to pull.</param>
        /// <param name="destinationPath">The location to save the file to.</param>
        void DownloadFile(string hash, string destinationPath);
    }
}
