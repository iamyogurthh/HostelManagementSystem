using System;
using System.IO;

namespace HostelManagementSystem.Utilities
{
    internal class ImageManager
    {
        private readonly string _baseAppDataDirectory;

        public ImageManager(string appName = "HostelManagementSystem")
        {
            string appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            _baseAppDataDirectory = Path.Combine(appDataFolder, appName);
        }

        /// <summary>
        /// Saves a source image file into a specified AppData subfolder if it is a new custom file.
        /// Reuses existing path if unchanged, or returns the default image path if empty or invalid.
        /// </summary>
        /// <param name="sourcePath">The temporary or newly selected local image file path.</param>
        /// <param name="existingDbPath">The image path currently stored in the database.</param>
        /// <param name="subDirectory">Subfolder category (e.g., "BuildingImages", "RoomImages", "FloorImages").</param>
        /// <param name="defaultImageName">Default placeholder file name or pack URI path.</param>
        /// <returns>The destination path of the newly copied file, or the default image string.</returns>
        public string SaveImage(string sourcePath, string existingDbPath, string subDirectory, string defaultImageName = "default.png")
        {
            // 1. If sourcePath is null or empty when creating a new record, return the default image path
            if (string.IsNullOrWhiteSpace(sourcePath))
            {
                return !string.IsNullOrWhiteSpace(existingDbPath) ? existingDbPath : defaultImageName;
            }

            // 2. If path hasn't changed and matches existing stored AppData path, reuse existing path or default if empty
            if (sourcePath == existingDbPath)
            {
                return !string.IsNullOrWhiteSpace(existingDbPath) ? existingDbPath : defaultImageName;
            }

            // 3. If source path is already the default image or file does not exist on disk, return default image
            if (IsDefaultImage(sourcePath, defaultImageName) || !File.Exists(sourcePath))
            {
                return defaultImageName;
            }

            try
            {
                string targetFolder = Path.Combine(_baseAppDataDirectory, subDirectory);
                Directory.CreateDirectory(targetFolder);

                string extension = Path.GetExtension(sourcePath);
                string uniqueFileName = $"{subDirectory.ToLower()}_{Guid.NewGuid():N}{extension}";
                string fullDestinationPath = Path.Combine(targetFolder, uniqueFileName);

                File.Copy(sourcePath, fullDestinationPath, overwrite: true);

                return fullDestinationPath;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving image to {subDirectory}: {ex.Message}");
                return defaultImageName;
            }
        }

        /// <summary>
        /// Safely deletes an existing custom image file from AppData, while protecting default image references.
        /// </summary>
        /// <param name="imagePath">The full file path to delete.</param>
        /// <param name="defaultImageName">The default image string to protect from deletion.</param>
        public void DeleteImage(string imagePath, string defaultImageName = "default.png")
        {
            if (string.IsNullOrWhiteSpace(imagePath) || IsDefaultImage(imagePath, defaultImageName))
            {
                return;
            }

            try
            {
                if (File.Exists(imagePath))
                {
                    File.Delete(imagePath);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to delete image ({imagePath}): {ex.Message}");
            }
        }

        /// <summary>
        /// Checks if a given path or file name corresponds to the default placeholder image.
        /// </summary>
        public bool IsDefaultImage(string path, string defaultImageName = "default.png")
        {
            if (string.IsNullOrWhiteSpace(path)) return true;

            return string.Equals(path, defaultImageName, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(Path.GetFileName(path), Path.GetFileName(defaultImageName), StringComparison.OrdinalIgnoreCase);
        }
    }
}