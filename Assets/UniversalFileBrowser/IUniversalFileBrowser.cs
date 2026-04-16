using System;
using System.Collections.Generic;

namespace UFB
{
    internal interface IUniversalFileBrowser
    {
        #region File
        /// <summary>
        /// Native open file dialog. Single select.
        /// </summary>
        /// <param name="title">Dialog title.</param>
        /// <param name="directory">Root directory.</param>
        /// <param name="filters">Allowed extensions.</param>
        /// <returns>Returns <see langword="null"/> if the user cancelled the dialog. Otherwise returns the selected path.</returns>
        public string SingleFileDialog(string title, string directory, Filter[] filters);

        /// <summary>
        /// Native open file dialog. Single select.
        /// </summary>
        /// <param name="title">Dialog title.</param>
        /// <param name="directory">Root directory.</param>
        /// <param name="filters">Allowed extensions.</param>
        /// <param name="path">Returns <see langword="null"/> if the user cancelled the dialog. Otherwise returns the selected path.</param>
        public void SingleFileDialog(string title, string directory, Filter[] filters, Action<string> path);

        /// <summary>
        /// Native open file dialog. Multi select.
        /// </summary>
        /// <param name="title">Dialog title.</param>
        /// <param name="directory">Root directory.</param>
        /// <param name="filters">Allowed extensions.</param>
        /// <returns>Returns <see langword="null"/> if the user cancelled the dialog. Otherwise returns the selected path.</returns>
        public IReadOnlyList<string> MultipleFileDialog(string title, string directory, Filter[] filters);

        /// <summary>
        /// Native open file dialog. Multi select.
        /// </summary>
        /// <param name="title">Dialog title.</param>
        /// <param name="directory">Root directory.</param>
        /// <param name="filters">Allowed extensions.</param>
        /// <param name="paths">Returns <see langword="null"/> if the user cancelled the dialog. Otherwise returns the selected path.</param>
        public void MultipleFileDialog(string title, string directory, Filter[] filters, Action<IReadOnlyList<string>> paths);
        #endregion

        #region Folder
        /// <summary>
        /// Native folder browser dialog. Single select.
        /// </summary>
        /// <param name="title">Dialog title.</param>
        /// <param name="directory">Root directory.</param>
        /// <returns>Returns <see langword="null"/> if the user cancelled the dialog. Otherwise returns the selected path.</returns>
        public string SingleFolderDialog(string title, string directory);

        /// <summary>
        /// Native folder browser dialog. Single select.
        /// </summary>
        /// <param name="title">Dialog title.</param>
        /// <param name="directory">Root directory.</param>
        /// <param name="path">Returns <see langword="null"/> if the user cancelled the dialog. Otherwise returns the selected path.</param>
        public void SingleFolderDialog(string title, string directory, Action<string> path);

        /// <summary>
        /// Native folder browser dialog. Multi select.
        /// </summary>
        /// <param name="title">Dialog title.</param>
        /// <param name="directory">Root directory.</param>
        /// <returns>Returns <see langword="null"/> if the user cancelled the dialog. Otherwise returns the selected path.</returns>
        public IReadOnlyList<string> MultipleFolderDialog(string title, string directory);

        /// <summary>
        /// Native folder browser dialog. Multi select.
        /// </summary>
        /// <param name="title">Dialog title.</param>
        /// <param name="directory">Root directory.</param>
        /// <param name="paths">Returns <see langword="null"/> if the user cancelled the dialog. Otherwise returns the selected path.</param>
        public void MultipleFolderDialog(string title, string directory, Action<IReadOnlyList<string>> paths);
        #endregion

        #region Save
        /// <summary>
        /// Native save file dialog.
        /// </summary>
        /// <param name="title">Dialog title.</param>
        /// <param name="directory">Root directory.</param>
        /// <param name="defaultName">Default file name.</param>
        /// <param name="filters">Allowed extensions.</param>
        /// <returns>Returns <see langword="null"/> if the user cancelled the dialog. Otherwise returns the selected path.</returns>
        public string SaveDialog(string title, string directory, string defaultName, Filter[] filters);

        /// <summary>
        /// Native save file dialog.
        /// </summary>
        /// <param name="title">Dialog title.</param>
        /// <param name="directory">Root directory.</param>
        /// <param name="defaultName">Default file name.</param>
        /// <param name="filters">Allowed extensions.</param>
        /// <param name="path">Returns <see langword="null"/> if the user cancelled the dialog. Otherwise returns the selected path.</param>
        public void SaveDialog(string title, string directory, string defaultName, Filter[] filters, Action<string> path);
        #endregion

        #region Process
        /// <summary>
        /// Native file browser.
        /// </summary>
        /// <param name="directory">Root directory.</param>
        /// <returns>Process id.</returns>
        public uint Browser(string directory);

        /// <summary>
        /// Native open file.
        /// </summary>
        /// <param name="directory">Root directory.</param>
        public void Open(string directory);

        /// <summary>
        /// Native start process.
        /// </summary>
        /// <param name="directory">Process directory.</param>
        /// <returns>Process id.</returns>
        public uint StartProcess(string directory);
        #endregion
    }
}