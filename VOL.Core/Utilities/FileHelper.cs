using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using VOL.Core.Extensions;

namespace VOL.Core.Utilities
{
    /// <summary>
    /// Provides helper methods for file system operations such as reading, writing, and manipulating files and directories.
    /// This class aims to simplify common file tasks and ensure proper resource management.
    /// </summary>
    public class FileHelper
    {
        // This field seems unused. If it was intended for locking, it's not currently implemented in the methods.
        // Consider removing if it serves no purpose or implementing locking if necessary for specific methods.
        private static object _filePathObj = new object();

        /// <summary>
        /// Reads lines from a file using an iterator, allowing for paged reading.
        /// This method is suitable for reading text files in chunks.
        /// Note: Performance may degrade for very large files (e.g., files with millions of lines or hundreds of MBs).
        /// It ensures that the underlying IEnumerator is disposed after use.
        /// </summary>
        /// <param name="fullPath">The full path to the file.</param>
        /// <param name="page">The page number to read (1-based). If less than or equal to 0, it defaults to 1.</param>
        /// <param name="pageSize">The number of lines per page.</param>
        /// <param name="seekEnd">If true, reads pages from the end of the file backwards. Default is false (reads from the start).</param>
        /// <returns>An IEnumerable&lt;string&gt; that yields lines from the specified page of the file.</returns>
        public static IEnumerable<string> ReadPageLine(string fullPath, int page, int pageSize, bool seekEnd = false)
        {
            if (page <= 0)
            {
                page = 1; // Default to page 1 if invalid page number is provided
            }
            fullPath = fullPath.ReplacePath(); // Normalize path for the current environment
            var lines = File.ReadLines(fullPath, Encoding.UTF8); // File.ReadLines uses deferred execution (reads lines on demand)

            // Logic for calculating skip/take based on whether reading from start or end
            if (seekEnd)
            {
                int lineCount = lines.Count(); // This will iterate through all lines once to get the count
                int totalPages = (int)Math.Ceiling(lineCount / (double)pageSize);

                if (page > totalPages)
                {
                    // If requested page is beyond the total pages from the end, return no lines
                    page = 0;
                    pageSize = 0;
                }
                else
                {
                    // Calculate the starting position (skip count from the beginning of the file)
                    // and the number of lines to take for the current page from the end.
                    int skip = lineCount - (page * pageSize);
                    if (skip < 0) // If the page calculation goes before the start of the file
                    {
                        pageSize = pageSize + skip; // Adjust pageSize to read only available lines
                        skip = 0;
                    }
                    page = skip; // 'page' here effectively becomes the 'skip' count for LINQ
                }
            }
            else
            {
                // For reading from the start, 'page' is converted to a 'skip' count
                page = (page - 1) * pageSize;
            }

            // Apply Skip and Take. If pageSize became 0 or negative (e.g., seekEnd beyond file), Take(0) returns empty.
            lines = lines.Skip(page).Take(pageSize < 0 ? 0 : pageSize);

            // Ensure IEnumerator is disposed
            var enumerator = lines.GetEnumerator();
            try
            {
                // Yield lines one by one
                while (enumerator.MoveNext())
                {
                    yield return enumerator.Current;
                }
            }
            finally
            {
                enumerator.Dispose(); // Explicitly dispose the enumerator
            }
        }

        /// <summary>
        /// Checks if a file exists at the specified path.
        /// Normalizes the path before checking.
        /// </summary>
        /// <param name="path">The path to the file.</param>
        /// <returns>True if the file exists; otherwise, false.</returns>
        public static bool FileExists(string path)
        {
            return File.Exists(path.ReplacePath());
        }

        /// <summary>
        /// Gets the current application's "Download" directory path.
        /// The path is mapped to an absolute path within the application's context.
        /// </summary>
        /// <returns>The absolute path to the "Download" directory.</returns>
        public static string GetCurrentDownLoadPath()
        {
            // "Download\\" is a relative path, MapPath() converts it to an absolute path.
            return ("Download\\").MapPath();
        }

        /// <summary>
        /// Checks if a directory exists at the specified path.
        /// Normalizes the path before checking.
        /// </summary>
        /// <param name="path">The path to the directory.</param>
        /// <returns>True if the directory exists; otherwise, false.</returns>
        public static bool DirectoryExists(string path)
        {
            return Directory.Exists(path.ReplacePath());
        }

        /// <summary>
        /// Reads the content of a file specified by directory path, filename, and suffix.
        /// This is a convenience method that combines path parts and then calls <see cref="ReadFile(string)"/>.
        /// </summary>
        /// <param name="fullpath">The directory path.</param>
        /// <param name="filename">The name of the file (without suffix).</param>
        /// <param name="suffix">The file suffix (e.g., ".txt").</param>
        /// <returns>The content of the file as a string. Returns an empty string if the file doesn't exist or an error occurs.</returns>
        public static string Read_File(string fullpath, string filename, string suffix)
        {
            // Path.Combine is preferred for joining path segments robustly.
            // MapPath() likely converts a virtual/relative path to an absolute physical path.
            return ReadFile(Path.Combine(fullpath, filename + suffix).MapPath());
        }

        /// <summary>
        /// Reads the entire content of a specified file into a string.
        /// Uses a <see cref="StreamReader"/> with UTF-8 encoding and a <see langword="using"/> statement for proper resource disposal.
        /// </summary>
        /// <param name="fullName">The fully qualified path to the file. This path will be normalized.</param>
        /// <returns>The content of the file as a string. Returns an empty string if the file does not exist or an error occurs during reading.</returns>
        public static string ReadFile(string fullName)
        {
            string mappedPath = fullName.MapPath().ReplacePath(); // Normalize path
            string str = "";
            if (!File.Exists(mappedPath))
            {
                return str; // Return empty if file doesn't exist
            }
            try
            {
                // The 'using' statement ensures that the StreamReader is properly closed and disposed,
                // even if an error occurs during reading.
                using (StreamReader sr = new StreamReader(mappedPath, Encoding.UTF8))
                {
                    str = sr.ReadToEnd();
                }
            }
            catch (Exception ex)
            {
                // Log the error. Depending on application requirements, this might re-throw the exception,
                // or return an empty string/null to indicate failure.
                VOL.Core.Services.Logger.Error(VOL.Core.Enums.LogLevel.Error, VOL.Core.Enums.LogEvent.Exception, $"Error reading file {mappedPath}", null, ex);
                // For a utility, re-throwing might be preferable to allow caller to handle.
                // throw new IOException($"Error reading file {mappedPath}", ex);
            }
            return str;
        }

        /// <summary>
        /// Extracts the extension (postfix) from a filename.
        /// For example, for "document.txt", it returns ".txt".
        /// </summary>
        /// <param name="filename">The name of the file.</param>
        /// <returns>The file extension including the dot. Returns an empty string if no extension is found or the filename ends with a dot.</returns>
        /// <remarks>
        /// Considers using <see cref="Path.GetExtension(string)"/> from the .NET BCL for a more robust and standard implementation.
        /// </remarks>
        public static string GetPostfixStr(string filename)
        {
            if (string.IsNullOrEmpty(filename)) return string.Empty;

            int start = filename.LastIndexOf(".");
            // If no dot, or dot is the last character, or dot is the first character (hidden file like .bashrc)
            // Path.GetExtension handles these cases, this custom logic might differ slightly.
            if (start == -1 || start == filename.Length - 1) return string.Empty;

            //string postfix = filename.Substring(start); // Simpler if just taking from 'start'
            int length = filename.Length;
            string postfix = filename.Substring(start, length - start);
            return postfix;
        }

        /// <summary>
        /// Writes string content to a specified file. Creates the directory if it doesn't exist.
        /// Uses a <see cref="FileStream"/> with UTF-8 encoding and a <see langword="using"/> statement for proper resource disposal.
        /// </summary>
        /// <param name="path">The directory path where the file should be written. This path will be normalized.</param>
        /// <param name="fileName">The name of the file. This path will be normalized.</param>
        /// <param name="content">The string content to write to the file.</param>
        /// <param name="appendToLast">If true, appends the content to the end of the file; otherwise, overwrites the file. Defaults to false (overwrite).</param>
        public static void WriteFile(string path, string fileName, string content, bool appendToLast = false)
        {
            path = path.ReplacePath();
            fileName = fileName.ReplacePath();

            try
            {
                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path); // Create directory if it does not exist

                // The 'using' statement ensures FileStream is properly closed and disposed.
                // FileMode.Append opens the file if it exists and seeks to the end, or creates a new file.
                // FileMode.Create overwrites the file if it exists, or creates a new file.
                using (FileStream stream = File.Open(Path.Combine(path, fileName), appendToLast ? FileMode.Append : FileMode.Create, FileAccess.Write))
                {
                    byte[] by = Encoding.UTF8.GetBytes(content);
                    stream.Write(by, 0, by.Length);
                }
            }
            catch (Exception ex)
            {
                VOL.Core.Services.Logger.Error(VOL.Core.Enums.LogLevel.Error, VOL.Core.Enums.LogEvent.Exception, $"Error writing to file {Path.Combine(path, fileName)}", null, ex);
                // throw new IOException($"Error writing to file {Path.Combine(path, fileName)}", ex);
            }
        }

        /// <summary>
        /// Appends strings to a file. Creates the file if it doesn't exist.
        /// Uses a <see cref="StreamWriter"/> with a <see langword="using"/> statement for proper resource disposal.
        /// Assumes UTF-8 encoding by default by <see cref="File.AppendText(string)"/>.
        /// </summary>
        /// <param name="fullPath">The full path to the file. This path will be normalized.</param>
        /// <param name="strings">The content to append.</param>
        public static void FileAdd(string fullPath, string strings)
        {
            try
            {
                // File.AppendText creates a StreamWriter that appends text to a file.
                // If the file does not exist, this method creates a new file.
                // The 'using' statement ensures the StreamWriter is properly closed and disposed.
                using (StreamWriter sw = File.AppendText(fullPath.ReplacePath()))
                {
                    sw.Write(strings);
                }
            }
            catch (Exception ex)
            {
                VOL.Core.Services.Logger.Error(VOL.Core.Enums.LogLevel.Error, VOL.Core.Enums.LogEvent.Exception, $"Error appending to file {fullPath}", null, ex);
                // throw new IOException($"Error appending to file {fullPath}", ex);
            }
        }

        /// <summary>
        /// Copies an existing file to a new file. Overwrites the new file if it already exists.
        /// Paths are normalized.
        /// </summary>
        /// <param name="orignFile">The path of the original file.</param>
        /// <param name="newFile">The path of the new file (destination).</param>
        public static void FileCoppy(string orignFile, string newFile) // Method name has a typo: "Coppy" should be "Copy"
        {
            // File.Copy handles its own resources and exceptions like FileNotFoundException, IOException, etc.
            // Consider adding try-catch here if specific logging or error handling is desired for this utility.
            File.Copy(orignFile.ReplacePath(), newFile.ReplacePath(), true); // 'true' to overwrite if newFile exists
        }

        /// <summary>
        /// Deletes the specified file.
        /// Path is normalized.
        /// </summary>
        /// <param name="path">The path of the file to delete.</param>
        public static void FileDel(string path) // Method name "FileDel" could be "FileDelete" for clarity.
        {
            // File.Delete handles its own resources. It does not throw an exception if the file does not exist.
            // Consider adding try-catch for logging or specific error handling if needed.
            File.Delete(path.ReplacePath());
        }

        /// <summary>
        /// Moves a specified file to a new location, providing the option to specify a new file name.
        /// Paths are normalized.
        /// </summary>
        /// <param name="orignFile">The path of the file to move (source).</param>
        /// <param name="newFile">The new path and/or name for the file (destination).</param>
        public static void FileMove(string orignFile, string newFile)
        {
            // File.Move handles its own resources. Will throw exceptions for common issues (e.g., file not found, destination exists).
            // Consider adding try-catch for logging or specific error handling.
            File.Move(orignFile.ReplacePath(), newFile.ReplacePath());
        }

        /// <summary>
        /// Creates a new directory within a specified existing directory (which becomes the current directory temporarily).
        /// This method changes the application's current directory, which can have side effects.
        /// Consider using <see cref="Directory.CreateDirectory(string)"/> with a combined path instead.
        /// </summary>
        /// <param name="orignFolder">The path to an existing directory that will become the current directory. Path is normalized.</param>
        /// <param name="newFloder">The name of the new directory to create within OrignFolder. Path is normalized.</param>
        /// <remarks>
        /// Changing the current directory (<see cref="Directory.SetCurrentDirectory(string)"/>) can be problematic in multi-threaded applications
        /// or if other parts of the code rely on a stable current directory.
        /// A safer approach is: `Directory.CreateDirectory(Path.Combine(orignFolder.ReplacePath(), NewFloder.ReplacePath()));`
        /// </remarks>
        public static void FolderCreate(string orignFolder, string newFloder) // "NewFloder" has a typo, should be "NewFolder"
        {
            // Directory operations do not typically involve IDisposable streams managed by this class.
            Directory.SetCurrentDirectory(orignFolder.ReplacePath()); // Warning: Changes global state
            Directory.CreateDirectory(newFloder.ReplacePath());
        }

        /// <summary>
        /// Creates a directory at the specified path if it does not already exist.
        /// Path is normalized.
        /// </summary>
        /// <param name="path">The path of the directory to create.</param>
        public static void FolderCreate(string path)
        {
            string normalizedPath = path.ReplacePath();
            if (!Directory.Exists(normalizedPath))
                Directory.CreateDirectory(normalizedPath); // Idempotent: does nothing if directory already exists.
        }

        /// <summary>
        /// Creates a new empty file at the specified path if it does not already exist.
        /// Uses a <see cref="FileStream"/> with a <see langword="using"/> statement for proper resource disposal.
        /// </summary>
        /// <param name="path">The full path where the file should be created. This path will be normalized.</param>
        public static void FileCreate(string path)
        {
            string normalizedPath = path.ReplacePath();
            FileInfo createFile = new FileInfo(normalizedPath);
            if (!createFile.Exists)
            {
                try
                {
                    // The 'using' statement ensures that the FileStream is properly closed and disposed,
                    // even if an error occurs. FileInfo.Create() returns a FileStream.
                    using (FileStream fs = createFile.Create())
                    {
                        // fs.Close() is implicitly called by Dispose() at the end of the using block.
                        // The file is created, and the stream is immediately closed.
                    }
                }
                catch (Exception ex)
                {
                    VOL.Core.Services.Logger.Error(VOL.Core.Enums.LogLevel.Error, VOL.Core.Enums.LogEvent.Exception, $"Error creating file {normalizedPath}", null, ex);
                    // throw new IOException($"Error creating file {normalizedPath}", ex);
                }
            }
        }

        /// <summary>
        /// Recursively deletes a folder and all its contents (subfolders and files).
        /// </summary>
        /// <param name="dir">The path of the directory to delete. This path will be normalized.</param>
        public static void DeleteFolder(string dir)
        {
            string normalizedDir = dir.ReplacePath();
            if (Directory.Exists(normalizedDir))
            {
                // Process all file system entries (files and subdirectories)
                foreach (string d in Directory.GetFileSystemEntries(normalizedDir))
                {
                    if (File.Exists(d)) // If it's a file, delete it
                        File.Delete(d);
                    else // If it's a directory, call DeleteFolder recursively
                        DeleteFolder(d);
                }
                // After all contents are deleted, delete the directory itself.
                // The 'true' for recursive in Directory.Delete(path, true) is not strictly needed here
                // as contents are manually deleted, but it doesn't harm.
                Directory.Delete(normalizedDir, true);
            }
        }

        /// <summary>
        /// Copies all contents of a source directory to a target directory.
        /// This includes subdirectories and their contents. Overwrites files in the target if they already exist.
        /// </summary>
        /// <param name="srcPath">The path of the source directory. This path will be normalized.</param>
        /// <param name="aimPath">The path of the target directory. It will be created if it doesn't exist. This path will be normalized.</param>
        /// <exception cref="Exception">Wraps and re-throws any exceptions that occur during the copy process, after logging.</exception>
        public static void CopyDir(string srcPath, string aimPath) // "aimPath" could be "destinationPath" for clarity
        {
            string normalizedSrcPath = srcPath.ReplacePath();
            string normalizedAimPath = aimPath.ReplacePath();

            try
            {
                // Ensure the destination path ends with a directory separator
                if (normalizedAimPath[normalizedAimPath.Length - 1] != Path.DirectorySeparatorChar)
                    normalizedAimPath += Path.DirectorySeparatorChar;

                // Create the destination directory if it doesn't exist
                if (!Directory.Exists(normalizedAimPath))
                    Directory.CreateDirectory(normalizedAimPath);

                // Get all file system entries from the source directory
                string[] fileList = Directory.GetFileSystemEntries(normalizedSrcPath);
                foreach (string file in fileList)
                {
                    string fileName = Path.GetFileName(file); // Get the name of the file or directory
                    string targetFilePath = Path.Combine(normalizedAimPath, fileName); // Construct the target path

                    if (Directory.Exists(file)) // If it's a directory, recurse
                        CopyDir(file, targetFilePath);
                    else // If it's a file, copy it, overwriting if it exists
                        File.Copy(file, targetFilePath, true);
                }
            }
            catch (Exception ex) // Catch any exception during the process
            {
                VOL.Core.Services.Logger.Error(VOL.Core.Enums.LogLevel.Error, VOL.Core.Enums.LogEvent.Exception, $"Error copying directory from {normalizedSrcPath} to {normalizedAimPath}", null, ex);
                // Re-throw the original exception to allow the caller to handle it or be aware of the failure.
                throw;
            }
        }

        /// <summary>
        /// Calculates the total size of a directory, including all files and subdirectories.
        /// </summary>
        /// <param name="dirPath">The path of the directory. This path will be normalized.</param>
        /// <returns>The total size of the directory in bytes. Returns 0 if the directory does not exist.</returns>
        public static long GetDirectoryLength(string dirPath)
        {
            string normalizedDirPath = dirPath.ReplacePath();
            if (!Directory.Exists(normalizedDirPath))
                return 0;

            long totalLength = 0;
            DirectoryInfo di = new DirectoryInfo(normalizedDirPath);

            // Add size of all files in the current directory
            foreach (FileInfo fi in di.GetFiles())
            {
                totalLength += fi.Length;
            }

            // Recursively add size of all subdirectories
            DirectoryInfo[] subDirectories = di.GetDirectories();
            if (subDirectories.Length > 0)
            {
                for (int i = 0; i < subDirectories.Length; i++)
                {
                    totalLength += GetDirectoryLength(subDirectories[i].FullName); // Recursive call
                }
            }
            return totalLength;
        }

        /// <summary>
        /// Gets a string containing detailed attributes of a specified file (e.g., full path, size, creation time).
        /// </summary>
        /// <param name="filePath">The full path to the file. This path will be normalized.</param>
        /// <returns>A string formatted with file attributes (HTML line breaks used). Returns an empty string if the file does not exist.</returns>
        public static string GetFileAttibe(string filePath) // "Attibe" has a typo, should be "Attribute" or "Attributes"
        {
            string attributesString = "";
            string normalizedFilePath = filePath.ReplacePath();

            if (!File.Exists(normalizedFilePath)) return attributesString; // Return early if file doesn't exist

            System.IO.FileInfo objFI = new System.IO.FileInfo(normalizedFilePath);
            // Using a StringBuilder might be slightly more efficient if this string were much larger or built more dynamically.
            attributesString += "详细路径:" + objFI.FullName + "<br>文件名称:" + objFI.Name + "<br>文件长度:" + objFI.Length.ToString() + "字节<br>创建时间" + objFI.CreationTime.ToString() + "<br>最后访问时间:" + objFI.LastAccessTime.ToString() + "<br>修改时间:" + objFI.LastWriteTime.ToString() + "<br>所在目录:" + objFI.DirectoryName + "<br>扩展名:" + objFI.Extension;
            return attributesString;
        }
    }
}
