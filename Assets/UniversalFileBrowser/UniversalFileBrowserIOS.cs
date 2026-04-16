#if UNITY_IOS
using System;
using System.Collections.Generic;

namespace UFB
{
    internal sealed class UniversalFileBrowserIOS : IUniversalFileBrowser
    {
        #region File
        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public string SingleFileDialog(string title, string directory, Filter[] filters)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public void SingleFileDialog(string title, string directory, Filter[] filters, Action<string> path)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public IReadOnlyList<string> MultipleFileDialog(string title, string directory, Filter[] filters)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public void MultipleFileDialog(string title, string directory, Filter[] filters, Action<IReadOnlyList<string>> paths)
        {
            throw new NotImplementedException();
        }
        #endregion

        #region Folder
        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public string SingleFolderDialog(string title, string directory)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public void SingleFolderDialog(string title, string directory, Action<string> path)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public IReadOnlyList<string> MultipleFolderDialog(string title, string directory)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public void MultipleFolderDialog(string title, string directory, Action<IReadOnlyList<string>> paths)
        {
            throw new NotImplementedException();
        }
        #endregion

        #region Save
        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public string SaveDialog(string title, string directory, string defaultName, Filter[] filters)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public void SaveDialog(string title, string directory, string defaultName, Filter[] filters, Action<string> path)
        {
            throw new NotImplementedException();
        }
        #endregion

        #region Process
        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public uint Browser(string directory)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public void Open(string directory)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public uint StartProcess(string directory)
        {
            throw new NotImplementedException();
        }
        #endregion
    }
}
#endif