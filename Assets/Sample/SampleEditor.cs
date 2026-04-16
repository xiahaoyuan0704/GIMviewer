#if UNITY_EDITOR
using UnityEditor;

using UnityEngine;

namespace UFB.Sample
{
    public sealed class SampleEditor : EditorWindow
    {
        private string m_console = string.Empty;
        private Vector2 scrollPos = Vector2.zero;

        [MenuItem("Universal File Browser/Sample")]
        private static void Init()
        {
            SampleEditor window = (SampleEditor)GetWindow(typeof(SampleEditor));
            window.Show();
        }

        private void OnGUI()
        {
            EditorGUILayout.BeginHorizontal(GUILayout.Width(600), GUILayout.Height(400));
            EditorGUILayout.BeginScrollView(scrollPos);

            if (GUILayout.Button("Single file dialog"))
                m_console = Sample.SingleFileDialog();
            if (GUILayout.Button("Multiple file dialog"))
                m_console = Sample.MultipleFileDialog();
            if (GUILayout.Button("File dialog async"))
                Sample.FileDialogAsync(cb => m_console = cb);
            if (GUILayout.Button("File dialog with filter"))
                m_console = Sample.FileDialogFilter();
            if (GUILayout.Button("File dialog with directory"))
                m_console = Sample.FileDialogDirectory();

            if (GUILayout.Button("Single folder dialog"))
                m_console = Sample.SingleFolderDialog();
            if (GUILayout.Button("Multiple folder dialog"))
                m_console = Sample.MultipleFolderDialog();
            if (GUILayout.Button("Folder dialog async"))
                Sample.FolderDialogAsync(cb => m_console = cb);
            if (GUILayout.Button("Folder dialog with directory"))
                m_console = Sample.FolderDialogDirectory();

            if (GUILayout.Button("Save dialog"))
                m_console = Sample.SaveDialog();
            if (GUILayout.Button("Save dialog async"))
                Sample.SaveDialogAsync(cb => m_console = cb);
            if (GUILayout.Button("Save dialog with filter"))
                m_console = Sample.SaveDialogFilter();
            if (GUILayout.Button("Save dialog with directory"))
                m_console = Sample.SaveDialogDirectory();
            if (GUILayout.Button("Save dialog with default name"))
                m_console = Sample.SaveDialogDefaultName();

            if (GUILayout.Button("Open file browser"))
                Sample.OpenBrowser();
            if (GUILayout.Button("Open file browser with directory"))
                Sample.OpenBrowserDirectory();

            if (GUILayout.Button("Open file"))
                Sample.OpenFile();

            if (GUILayout.Button("Start a process"))
                m_console = Sample.StartProcess();

            EditorGUILayout.EndScrollView();
            GUILayout.TextArea(m_console, GUILayout.Width(320), GUILayout.Height(375));
            EditorGUILayout.EndHorizontal();
        }
    }
}
#endif