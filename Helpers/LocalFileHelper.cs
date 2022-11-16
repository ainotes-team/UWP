using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FileAccess = System.IO.FileAccess;

namespace Helpers {
    public static class LocalFileHelper {
        public static void CreateAppDirectories() {
            // image components
            if (!FolderExists("plugin_data")) {
                CreateFolder("plugin_data");
            }
            if (!FolderExists("plugin_data/image")) {
                CreateFolder("plugin_data/image");
            }
            
            if (!FolderExists("component_data")) {
                CreateFolder("component_data");
            }
            if (!FolderExists("component_data/image")) {
                CreateFolder("component_data/image");
            }
        }
        
        public static string ToAbsolutePath(string path) {
            path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), path);
            path = Path.GetFullPath(path);
            return path;
        }

        public static IEnumerable<string> GetFolders(string path, bool includePath = false) {
            var dirs = Directory.GetDirectories(ToAbsolutePath(path));
            if (includePath) return dirs;

            var dirs2 = new string[dirs.Length];
            for (var i = 0; i < dirs.Length; i++) dirs2[i] = dirs[i].Replace(ToAbsolutePath(path), "");
            return dirs2;
        }

        public static IEnumerable<string> GetFiles(string path, bool includePath = false, bool includeHidden = false, string[] excludedEndings = null) {
            var allFiles = Directory.GetFiles(ToAbsolutePath(path)).ToList();
            
            List<string> tempFiles = null;
            if (!includeHidden) {
                tempFiles = allFiles.Where(t => !File.GetAttributes(t).HasFlag(FileAttributes.Hidden)).ToList();
            }
            if (tempFiles == null) tempFiles = allFiles;

            if (excludedEndings != null) {
                foreach (var file in tempFiles.ToArray()) {
                    if (excludedEndings.Any(file.EndsWith)) {
                        tempFiles.Remove(file);
                    }
                }
            }
            
            if (includePath) return tempFiles;

            var shortFiles = new string[tempFiles.Count];
            for (var i = 0; i < tempFiles.Count; i++) shortFiles[i] = tempFiles[i].Replace(ToAbsolutePath(path), "");
            return shortFiles;
        }

        public static bool FileExists(string path) {
            return File.Exists(ToAbsolutePath(path));
        }

        public static bool FolderExists(string path) {
            return Directory.Exists(ToAbsolutePath(path));
        }

        public static void CreateFolder(string path, bool checkExisting=false) {
            if (path.Replace(" ", "") == "") return;
            if (checkExisting && FolderExists(path)) return;
            Directory.CreateDirectory(ToAbsolutePath(path));
        }

        public static async Task<string> ReadFileAsync(string path) {
            var f = File.OpenText(ToAbsolutePath(path));
            var content = await f.ReadToEndAsync();
            f.Close();
            return content;
        }
        
        public static string ReadFile(string path) {
            var f = File.OpenText(ToAbsolutePath(path));
            var content = f.ReadToEnd();
            f.Close();
            return content;
        }

        public static async Task<byte[]> ReadBytes(string path) {
            // using (var stream = File.Open(ToAbsolutePath(path), FileMode.Open, FileAccess.Read, FileShare.Read)) {
            //     var result = new byte[stream.Length];
            //     await stream.ReadAsync(result, 0, (int) stream.Length);
            // }

            return await File.ReadAllBytesAsync(ToAbsolutePath(path));
        }


        public static void WriteFile(string path, string content) {
            var directoryName = Path.GetDirectoryName(ToAbsolutePath(path));
            if (!Directory.Exists(directoryName) && directoryName != null) Directory.CreateDirectory(directoryName);
            File.WriteAllText(ToAbsolutePath(path), content, Encoding.UTF8);
        }

        public static async Task WriteFileAsync(string path, string content) {
            var directoryName = Path.GetDirectoryName(ToAbsolutePath(path));
            if (!Directory.Exists(directoryName) && directoryName != null) Directory.CreateDirectory(directoryName);
            using (var sw = new StreamWriter(ToAbsolutePath(path))) {
                await sw.WriteAsync(content);
            }
        }

        public static void WriteFile(string path, byte[] content) {
            var directoryName = Path.GetDirectoryName(ToAbsolutePath(path));
            if (!Directory.Exists(directoryName) && directoryName != null) Directory.CreateDirectory(directoryName);
            File.WriteAllBytes(ToAbsolutePath(path), content);
        }
        
        public static async Task WriteFileAsync(string path, byte[] content) {
            var directoryName = Path.GetDirectoryName(ToAbsolutePath(path));
            if (!Directory.Exists(directoryName) && directoryName != null) Directory.CreateDirectory(directoryName);
            using (var stream = File.Open(ToAbsolutePath(path), FileMode.Create, FileAccess.ReadWrite)) {
                await stream.WriteAsync(content, 0, content.Length);
            }
        }

        public static FileStream GetFile(string path, FileMode fileMode = FileMode.Open) {
            return File.Open(ToAbsolutePath(path), fileMode);
        }

        public static void DeleteFile(string path) {
            File.Delete(ToAbsolutePath(path));
        }
         
        public static void DeleteFolder(string path) {
            Directory.Delete(ToAbsolutePath(path), true);
        }

        public static void MoveFile(string oldPath, string newPath) {
            File.Move(ToAbsolutePath(oldPath), ToAbsolutePath(newPath));
        }

        public static void CopyFile(string oldPath, string newPath, bool overwrite=false) {
            File.Copy(ToAbsolutePath(oldPath), ToAbsolutePath(newPath), overwrite);
        }

        public static void MoveFolder(string oldPath, string newPath) {
            Directory.Move(ToAbsolutePath(oldPath), ToAbsolutePath(newPath));
        }

        public static void CopyFolder(string oldPath, string newPath, bool copySubDirs) {
            var dir = new DirectoryInfo(oldPath);

            if (!dir.Exists) {
                Logger.Log("[LocalFileManager]", $"Exception in CopyFolder(string {oldPath}, string {newPath}, bool {copySubDirs}): " + "dir.Exists == false", logLevel: LogLevel.Error);
                return;
            }

            var dirs = dir.GetDirectories();
            if (!Directory.Exists(newPath)) Directory.CreateDirectory(newPath);

            var files = dir.GetFiles();
            foreach (var file in files) {
                var tempPath = Path.Combine(newPath, file.Name);
                file.CopyTo(tempPath, false);
            }

            if (!copySubDirs) return;
            foreach (var subDir in dirs) {
                var tempPath = Path.Combine(newPath, subDir.Name);
                CopyFolder(subDir.FullName, tempPath, true);
            }
        }

        public static DateTime GetLastChangedTime(string path) {
            return File.GetLastWriteTime(ToAbsolutePath(path));
        }
    }
}