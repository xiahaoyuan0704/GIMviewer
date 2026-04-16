using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace UFB
{
    /// <summary>
    /// Wrapper for native file dialogs on multiple platforms
    /// </summary>
    public static class UniversalFileBrowser
    {
        #region Wrapper
        /// <summary>
        /// Platform wrapper.
        /// </summary>
        private static readonly IUniversalFileBrowser _platform = null;

        static UniversalFileBrowser()
        {
#if UNITY_ANDROID
            _platform = new UniversalFileBrowserAndroid();
#elif UNITY_IOS
            _platform = new UniversalFileBrowserIOS();
#elif UNITY_STANDALONE_LINUX
            _platform = new UniversalFileBrowserLinux();
#elif UNITY_STANDALONE_OSX
            _platform = new UniversalFileBrowserMacOS();
#elif UNITY_WSA && ENABLE_WINMD_SUPPORT
            _platform = new UniversalFileBrowserUWP();
#elif UNITY_WEBGL
            _platform = new UniversalFileBrowserWebGL();
#elif UNITY_STANDALONE_WIN
            _platform = new UniversalFileBrowserWindows();
#endif
        }
        #endregion

        #region File
        /// <summary>
        /// Native open file dialog. Single select.
        /// </summary>
        /// <param name="title">Dialog title.</param>
        /// <param name="directory">Root directory.</param>
        /// <returns>Returns <see langword="null"/> if the user cancelled the dialog. Otherwise returns the selected path.</returns>
        public static string SingleFileDialog(string title, string directory)
        {
            return SingleFileDialog(title, directory, new[] { new Filter("All Files", "*") });
        }
        /// <summary>
        /// Native open file dialog. Single select.
        /// </summary>
        /// <param name="title">Dialog title.</param>
        /// <param name="directory">Root directory.</param>
        /// <param name="filters">Allowed extensions.</param>
        /// <returns>Returns <see langword="null"/> if the user cancelled the dialog. Otherwise returns the selected path.</returns>
        public static string SingleFileDialog(string title, string directory, Filter[] filters)
        {
            return _platform.SingleFileDialog(title, directory, filters);
        }

        /// <summary>
        /// Native open file dialog. Single select.
        /// </summary>
        /// <param name="title">Dialog title.</param>
        /// <param name="directory">Root directory.</param>
        /// <param name="path">Returns <see langword="null"/> if the user cancelled the dialog. Otherwise returns the selected path.</param>
        public static void SingleFileDialog(string title, string directory, Action<string> path)
        {
            SingleFileDialog(title, directory, new[] { new Filter("All Files", "*") }, path);
        }
        /// <summary>
        /// Native open file dialog. Single select.
        /// </summary>
        /// <param name="title">Dialog title.</param>
        /// <param name="directory">Root directory.</param>
        /// <param name="filters">Allowed extensions.</param>
        /// <param name="path">Returns <see langword="null"/> if the user cancelled the dialog. Otherwise returns the selected path.</param>
        public static void SingleFileDialog(string title, string directory, Filter[] filters, Action<string> path)
        {
            _platform.SingleFileDialog(title, directory, filters, path);
        }

        /// <summary>
        /// Native open file dialog. Multi select.
        /// </summary>
        /// <param name="title">Dialog title.</param>
        /// <param name="directory">Root directory.</param>
        /// <returns>Returns <see langword="null"/> if the user cancelled the dialog. Otherwise returns the selected path.</returns>
        public static IReadOnlyList<string> MultipleFileDialog(string title, string directory)
        {
            return MultipleFileDialog(title, directory, new[] { new Filter("All Files", "*") });
        }
        /// <summary>
        /// Native open file dialog. Multi select.
        /// </summary>
        /// <param name="title">Dialog title.</param>
        /// <param name="directory">Root directory.</param>
        /// <param name="filters">Allowed extensions.</param>
        /// <returns>Returns <see langword="null"/> if the user cancelled the dialog. Otherwise returns the selected path.</returns>
        public static IReadOnlyList<string> MultipleFileDialog(string title, string directory, Filter[] filters)
        {
            return _platform.MultipleFileDialog(title, directory, filters);
        }

        /// <summary>
        /// Native open file dialog. Multi select.
        /// </summary>
        /// <param name="title">Dialog title.</param>
        /// <param name="directory">Root directory.</param>
        /// <param name="paths">Returns <see langword="null"/> if the user cancelled the dialog. Otherwise returns the selected path.</param>
        public static void MultipleFileDialog(string title, string directory, Action<IReadOnlyList<string>> paths)
        {
            MultipleFileDialog(title, directory, new[] { new Filter("All Files", "*") }, paths);
        }
        /// <summary>
        /// Native open file dialog. Multi select.
        /// </summary>
        /// <param name="title">Dialog title.</param>
        /// <param name="directory">Root directory.</param>
        /// <param name="filters">Allowed extensions.</param>
        /// <param name="paths">Returns <see langword="null"/> if the user cancelled the dialog. Otherwise returns the selected path.</param>
        public static void MultipleFileDialog(string title, string directory, Filter[] filters, Action<IReadOnlyList<string>> paths)
        {
            _platform.MultipleFileDialog(title, directory, filters, paths);
        }
        #endregion

        #region Folder
        /// <summary>
        /// Native folder browser dialog. Single select.
        /// </summary>
        /// <param name="title">Dialog title.</param>
        /// <param name="directory">Root directory.</param>
        /// <returns>Returns <see langword="null"/> if the user cancelled the dialog. Otherwise returns the selected path.</returns>
        public static string SingleFolderDialog(string title, string directory)
        {
            return _platform.SingleFolderDialog(title, directory);
        }

        /// <summary>
        /// Native folder browser dialog. Single select.
        /// </summary>
        /// <param name="title">Dialog title.</param>
        /// <param name="directory">Root directory.</param>
        /// <param name="path">Returns <see langword="null"/> if the user cancelled the dialog. Otherwise returns the selected path.</param>
        public static void SingleFolderDialog(string title, string directory, Action<string> path)
        {
            _platform.SingleFolderDialog(title, directory, path);
        }

        /// <summary>
        /// Native folder browser dialog. Multi select.
        /// </summary>
        /// <param name="title">Dialog title.</param>
        /// <param name="directory">Root directory.</param>
        /// <returns>Returns <see langword="null"/> if the user cancelled the dialog. Otherwise returns the selected path.</returns>
        public static IReadOnlyList<string> MultipleFolderDialog(string title, string directory)
        {
            return _platform.MultipleFolderDialog(title, directory);
        }

        /// <summary>
        /// Native folder browser dialog. Multi select.
        /// </summary>
        /// <param name="title">Dialog title.</param>
        /// <param name="directory">Root directory.</param>
        /// <param name="paths">Returns <see langword="null"/> if the user cancelled the dialog. Otherwise returns the selected path.</param>
        public static void MultipleFolderDialog(string title, string directory, Action<IReadOnlyList<string>> paths)
        {
            _platform.MultipleFolderDialog(title, directory, paths);
        }
        #endregion

        #region Save
        /// <summary>
        /// Native save file dialog.
        /// </summary>
        /// <param name="title">Dialog title.</param>
        /// <param name="directory">Root directory.</param>
        /// <param name="defaultName">Default file name.</param>
        /// <returns>Returns <see langword="null"/> if the user cancelled the dialog. Otherwise returns the selected path.</returns>
        public static string SaveDialog(string title, string directory, string defaultName)
        {
            return SaveDialog(title, directory, defaultName, new[] { new Filter("All Files", "*") });
        }
        /// <summary>
        /// Native save file dialog.
        /// </summary>
        /// <param name="title">Dialog title.</param>
        /// <param name="directory">Root directory.</param>
        /// <param name="defaultName">Default file name.</param>
        /// <param name="filters">Allowed extensions.</param>
        /// <returns>Returns <see langword="null"/> if the user cancelled the dialog. Otherwise returns the selected path.</returns>
        public static string SaveDialog(string title, string directory, string defaultName, Filter[] filters)
        {
            return _platform.SaveDialog(title, directory, defaultName, filters);
        }

        /// <summary>
        /// Native save file dialog.
        /// </summary>
        /// <param name="title">Dialog title.</param>
        /// <param name="directory">Root directory.</param>
        /// <param name="defaultName">Default file name.</param>
        /// <param name="path">Returns <see langword="null"/> if the user cancelled the dialog. Otherwise returns the selected path.</param>
        public static void SaveDialog(string title, string directory, string defaultName, Action<string> path)
        {
            SaveDialog(title, directory, defaultName, new[] { new Filter("All Files", "*") }, path);
        }
        /// <summary>
        /// Native save file dialog.
        /// </summary>
        /// <param name="title">Dialog title.</param>
        /// <param name="directory">Root directory.</param>
        /// <param name="defaultName">Default file name.</param>
        /// <param name="filters">Allowed extensions.</param>
        /// <param name="path">Returns <see langword="null"/> if the user cancelled the dialog. Otherwise returns the selected path.</param>
        public static void SaveDialog(string title, string directory, string defaultName, Filter[] filters, Action<string> path)
        {
            _platform.SaveDialog(title, directory, defaultName, filters, path);
        }
        #endregion

        #region Process
        /// <summary>
        /// Native file browser.
        /// </summary>
        /// <param name="directory">Root directory.</param>
        /// <returns>Process id.</returns>
        public static uint Browser(string directory)
        {
            return _platform.Browser(directory);
        }

        /// <summary>
        /// Native open file.
        /// </summary>
        /// <param name="directory">Root directory.</param>
        public static void Open(string directory)
        {
            _platform.Open(directory);
        }

        /// <summary>
        /// Native start process.
        /// </summary>
        /// <param name="directory">Process directory.</param>
        /// <returns>Process id.</returns>
        public static uint StartProcess(string directory)
        {
            return _platform.StartProcess(directory);
        }
        #endregion
    }

    /// <summary>
    /// Gets or sets the current file name filter string, which determines the choices that appear in the "Save as file type" or "Files of type" box in the dialog box.
    /// </summary>
    public struct Filter
    {
        private static readonly char _semiColon = ';';
        private static readonly char _pipe = '|';

        /// <summary>
        /// Extension name.
        /// </summary>
        /// <example>Text files</example>
        public string DisplayName { get; }
        /// <summary>
        /// Allowed extensions.
        /// </summary>
        /// <example>.txt</example>
        public IReadOnlyList<string> Extensions { get; }

        public Filter(string displayName, IEnumerable<string> extensions)
        {
            if (extensions is null) throw new ArgumentNullException(nameof(extensions));

            Extensions = extensions
                .Select(s => s.Trim()) // Trim whitespace
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .ToList(); // make a copy to prevent possible changes

            if (Extensions.Count == 0)
            {
                throw new ArgumentException("Extensions collection must not be empty, nor can it contain only null, empty or whitespace extensions.", nameof(extensions));
            }

            if (string.IsNullOrWhiteSpace(displayName))
            {
                displayName = extensions.First();
            }
            DisplayName = displayName.Trim();
        }
        public Filter(string displayName, params string[] extensions) : this(displayName, (IEnumerable<string>)extensions) { }

        public static IReadOnlyList<Filter> ParseWindowsFormsFilter(string filter)
        {
            // https://msdn.microsoft.com/en-us/library/system.windows.forms.filedialog.filter(v=vs.110).aspx
            if (string.IsNullOrWhiteSpace(filter))
            {
                return null;
            }

            string[] components = filter.Split(_pipe, StringSplitOptions.RemoveEmptyEntries);
            if (components.Length % 2 != 0)
            {
                return null;
            }

            Filter[] filters = new Filter[components.Length / 2];
            int fi = 0;
            for (int i = 0; i < components.Length; i += 2)
            {
                string displayName = components[i];
                string extensionsCat = components[i + 1];

                string[] extensions = extensionsCat.Split(_semiColon, StringSplitOptions.RemoveEmptyEntries);

                filters[fi] = new Filter(displayName, extensions);
                fi++;
            }

            return filters;
        }

        internal string ToFilterSpecString()
        {
            StringBuilder sb = new();
            bool first = true;
            foreach (string extension in Extensions)
            {
                if (!first)
                {
                    _ = sb.Append(';');
                }

                first = false;

                _ = sb.Append("*.");
                _ = sb.Append(extension);
            }

            return sb.ToString();
        }

        internal void ToExtensionList(StringBuilder sb)
        {
            bool first = true;
            foreach (string extension in Extensions)
            {
                if (!first)
                {
                    _ = sb.Append(", ");
                }

                first = false;

                _ = sb.Append("*.");
                _ = sb.Append(extension);
            }
        }

        public override string ToString()
        {
            StringBuilder sb = new();
            _ = sb.Append(DisplayName);

            _ = sb.Append(" (");
            ToExtensionList(sb);
            _ = sb.Append(')');

            return sb.ToString();
        }
    }
}