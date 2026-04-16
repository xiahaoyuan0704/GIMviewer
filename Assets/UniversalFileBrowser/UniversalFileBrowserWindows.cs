#if UNITY_STANDALONE_WIN
using ShellFileDialogs;

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace UFB
{
    internal sealed class UniversalFileBrowserWindows : IUniversalFileBrowser
    {
        [DllImport("user32.dll")]
        private static extern IntPtr GetActiveWindow();

        #region File
        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public string SingleFileDialog(string title, string directory, Filter[] filters)
        {
            return FileOpenDialog.ShowSingleSelectDialog(GetActiveWindow(), title, directory, string.Empty, ParseFilter(filters), null);
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public void SingleFileDialog(string title, string directory, Filter[] filters, Action<string> path)
        {
            path.Invoke(SingleFileDialog(title, directory, filters));
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public IReadOnlyList<string> MultipleFileDialog(string title, string directory, Filter[] filters)
        {
            return FileOpenDialog.ShowMultiSelectDialog(GetActiveWindow(), title, directory, string.Empty, ParseFilter(filters), null);
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public void MultipleFileDialog(string title, string directory, Filter[] filters, Action<IReadOnlyList<string>> paths)
        {
            paths.Invoke(MultipleFileDialog(title, directory, filters));
        }
        #endregion

        #region Folder
        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public string SingleFolderDialog(string title, string directory)
        {
            return FolderBrowserDialog.ShowSingleSelectDialog(GetActiveWindow(), title, directory);
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public void SingleFolderDialog(string title, string directory, Action<string> path)
        {
            path.Invoke(SingleFolderDialog(title, directory));
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public IReadOnlyList<string> MultipleFolderDialog(string title, string directory)
        {
            return FolderBrowserDialog.ShowMultiSelectDialog(GetActiveWindow(), title, directory);
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public void MultipleFolderDialog(string title, string directory, Action<IReadOnlyList<string>> paths)
        {
            paths.Invoke(MultipleFolderDialog(title, directory));
        }
        #endregion

        #region Save
        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public string SaveDialog(string title, string directory, string defaultName, Filter[] filters)
        {
            return FileSaveDialog.ShowDialog(GetActiveWindow(), title, directory, defaultName, ParseFilter(filters), 0);
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public void SaveDialog(string title, string directory, string defaultName, Filter[] filters, Action<string> path)
        {
            path.Invoke(SaveDialog(title, directory, defaultName, filters));
        }
        #endregion

        #region Process
        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public uint Browser(string directory)
        {
            StringBuilder commandLine = new();
            _ = commandLine.Append("C:\\Windows\\explorer.exe select,");
            if (!string.IsNullOrWhiteSpace(directory))
            {
                _ = commandLine.Append(System.IO.Path.GetDirectoryName(directory.Trim()));
            }
            _ = commandLine.Replace('/', '\\'); // explorer.exe doesn't like front slashes

            return ExternalProcess.Start(commandLine.ToString(), null);
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public void Open(string directory)
        {
            StringBuilder commandLine = new();
            _ = commandLine.Append("C:\\Windows\\explorer.exe select,");
            _ = commandLine.Append(directory.Trim());
            _ = commandLine.Replace('/', '\\'); // explorer.exe doesn't like front slashes

            ExternalProcess.Start(commandLine.ToString(), null);
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public uint StartProcess(string directory)
        {
            StringBuilder commandLine = new();
            _ = commandLine.Append(directory.Trim());
            _ = commandLine.Replace('/', '\\'); // explorer.exe doesn't like front slashes

            return ExternalProcess.Start(commandLine.ToString(), null);
        }
        #endregion

        /// <summary>
        /// Parse <see cref="Filter[]"/> in a format compatible with <see cref="ShellFileDialogs"/>
        /// </summary>
        private static ShellFileDialogs.Filter[] ParseFilter(Filter[] filters)
        {
            if (filters is null) throw new ArgumentNullException(nameof(filters));

            ShellFileDialogs.Filter[] shell = new ShellFileDialogs.Filter[filters.Length];
            for (int i = 0; i < filters.Length; i++)
            {
                shell[i] = new(filters[i].DisplayName, filters[i].Extensions);
            }
            return shell;
        }
    }
}
#endif