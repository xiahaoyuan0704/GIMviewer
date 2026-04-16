using System;
using System.Collections.Generic;
using System.Text;

using UnityEngine;

namespace UFB.Sample
{
    public static class Sample
    {
        private const string FILE = "Open File";
        private const string FILES = "Open Files";
        private const string FOLDER = "Open Folder";
        private const string FOLDERS = "Open Folders";
        private const string SAVE = "Save File";
        private const string FILE_NAME = "Default file name";
        private const string EXECUTABLE = "Select Executable";

        public static readonly Filter[] m_filters = new[]
        {
            new Filter("Image Files", "png", "jpg" ),
            new Filter("Sound Files", "mp3" ),
            new Filter("All Files", "*" ),
        };

        private static string Console(string path)
        {
            StringBuilder sb = new();

            if (path != null)
            {
                _ = sb.Append(path);
                _ = sb.Append("\n");
            }
            else
            {
                _ = sb.Append("Dialog cancelled");
            }

            return sb.ToString();
        }
        private static string Console(IEnumerable<string> paths)
        {
            StringBuilder sb = new();

            if (paths != null)
            {
                foreach (string path in paths)
                {
                    _ = sb.Append(path);
                    _ = sb.Append("\n");
                }
            }
            else
            {
                _ = sb.Append("Dialog cancelled");
            }

            return sb.ToString();
        }

        public static string SingleFileDialog()
        {
            string path = UniversalFileBrowser.SingleFileDialog(FILE, null);
            return Console(path);
        }
        public static string MultipleFileDialog()
        {
            IReadOnlyList<string> paths = UniversalFileBrowser.MultipleFileDialog(FILES, null);
            return Console(paths);
        }
        public static void FileDialogAsync(Action<string> cb)
        {
            UniversalFileBrowser.SingleFileDialog(FILES, null, path =>
            {
                cb.Invoke(Console(path));
            });
        }
        public static string FileDialogFilter()
        {
            string path = UniversalFileBrowser.SingleFileDialog(FILES, null, m_filters);
            return Console(path);
        }
        public static string FileDialogDirectory()
        {
            string path = UniversalFileBrowser.SingleFileDialog(FILE, Application.dataPath);
            return Console(path);
        }

        public static string SingleFolderDialog()
        {
            string path = UniversalFileBrowser.SingleFolderDialog(FOLDER, null);
            return Console(path);
        }
        public static string MultipleFolderDialog()
        {
            IReadOnlyList<string> paths = UniversalFileBrowser.MultipleFolderDialog(FOLDERS, null);
            return Console(paths);
        }
        public static void FolderDialogAsync(Action<string> cb)
        {
            UniversalFileBrowser.SingleFolderDialog(FOLDERS, null, path =>
            {
                cb.Invoke(Console(path));
            });
        }
        public static string FolderDialogDirectory()
        {
            string path = UniversalFileBrowser.SingleFolderDialog(FOLDERS, Application.dataPath);
            return Console(path);
        }

        public static string SaveDialog()
        {
            string path = UniversalFileBrowser.SaveDialog(SAVE, null, string.Empty);
            return Console(path);
        }
        public static void SaveDialogAsync(Action<string> cb)
        {
            UniversalFileBrowser.SaveDialog(SAVE, null, string.Empty, paths =>
            {
                cb.Invoke(Console(paths));
            });
        }
        public static string SaveDialogFilter()
        {
            string path = UniversalFileBrowser.SaveDialog(SAVE, null, string.Empty, m_filters);
            return Console(path);
        }
        public static string SaveDialogDirectory()
        {
            string path = UniversalFileBrowser.SaveDialog(SAVE, Application.dataPath, null);
            return Console(path);
        }
        public static string SaveDialogDefaultName()
        {
            string path = UniversalFileBrowser.SaveDialog(SAVE, null, FILE_NAME);
            return Console(path);
        }

        public static void OpenBrowser()
        {
            UniversalFileBrowser.Browser(string.Empty);
        }
        public static void OpenBrowserDirectory()
        {
            UniversalFileBrowser.Browser(Application.dataPath);
        }

        public static string OpenFile()
        {
            string path = UniversalFileBrowser.SingleFileDialog(FILE, null);
            if (path != null)
            {
                UniversalFileBrowser.Open(path);
            }
            return Console(path);
        }

        public static string StartProcess()
        {
            string path = UniversalFileBrowser.SingleFileDialog(EXECUTABLE, null);
            if (path != null)
            {
                UniversalFileBrowser.StartProcess(path);
            }
            return Console(path);
        }
    }
}