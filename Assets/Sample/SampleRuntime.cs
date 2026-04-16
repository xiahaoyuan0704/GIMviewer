using UnityEngine;

namespace UFB.Sample
{
    public sealed class SampleRuntime : MonoBehaviour
    {
        private const int WIDTH = 200;
        private const int HEIGHT = 30;

        private string m_console = string.Empty;

        private void OnGUI()
        {
            int x = 10;
            int y = 10;

            if (GUI.Button(new Rect(x, y              , WIDTH, HEIGHT), "Single file dialog"))
                m_console = Sample.SingleFileDialog();
            if (GUI.Button(new Rect(x, y += HEIGHT + 5, WIDTH, HEIGHT), "Multiple file dialog"))
                m_console = Sample.MultipleFileDialog();
            if (GUI.Button(new Rect(x, y += HEIGHT + 5, WIDTH, HEIGHT), "File dialog async"))
                Sample.FileDialogAsync(cb => m_console = cb);
            if (GUI.Button(new Rect(x, y += HEIGHT + 5, WIDTH, HEIGHT), "File dialog with filter"))
                m_console = Sample.FileDialogFilter();
            if (GUI.Button(new Rect(x, y += HEIGHT + 5, WIDTH, HEIGHT), "File dialog with directory"))
                m_console = Sample.FileDialogDirectory();

            if (GUI.Button(new Rect(x, y += HEIGHT + 5, WIDTH, HEIGHT), "Single folder dialog"))
                m_console = Sample.SingleFolderDialog();
            if (GUI.Button(new Rect(x, y += HEIGHT + 5, WIDTH, HEIGHT), "Multiple folder dialog"))
                m_console = Sample.MultipleFolderDialog();
            if (GUI.Button(new Rect(x, y += HEIGHT + 5, WIDTH, HEIGHT), "Folder dialog async"))
                Sample.FolderDialogAsync(cb => m_console = cb);
            if (GUI.Button(new Rect(x, y += HEIGHT + 5, WIDTH, HEIGHT), "Folder dialog with directory"))
                m_console = Sample.FolderDialogDirectory();

            if (GUI.Button(new Rect(x, y += HEIGHT + 5, WIDTH, HEIGHT), "Save dialog"))
                m_console = Sample.SaveDialog();
            if (GUI.Button(new Rect(x, y += HEIGHT + 5, WIDTH, HEIGHT), "Save dialog async"))
                Sample.SaveDialogAsync(cb => m_console = cb);
            if (GUI.Button(new Rect(x, y += HEIGHT + 5, WIDTH, HEIGHT), "Save dialog with filter"))
                m_console = Sample.SaveDialogFilter();
            if (GUI.Button(new Rect(x, y += HEIGHT + 5, WIDTH, HEIGHT), "Save dialog with directory"))
                m_console = Sample.SaveDialogDirectory();
            if (GUI.Button(new Rect(x, y += HEIGHT + 5, WIDTH, HEIGHT), "Save dialog with default name"))
                m_console = Sample.SaveDialogDefaultName();

            if (GUI.Button(new Rect(x, y += HEIGHT + 5, WIDTH, HEIGHT), "Open file browser"))
                Sample.OpenBrowser();
            if (GUI.Button(new Rect(x, y += HEIGHT + 5, WIDTH, HEIGHT), "Open file browser with directory"))
                Sample.OpenBrowserDirectory();

            if (GUI.Button(new Rect(x, y += HEIGHT + 5, WIDTH, HEIGHT), "Open file"))
                Sample.OpenFile();

            if (GUI.Button(new Rect(x, y += HEIGHT + 5, WIDTH, HEIGHT), "Start a process"))
                m_console = Sample.StartProcess();

            GUI.TextArea(new Rect(Screen.width / 3f, 0, Screen.width / 1.5f, Screen.height), m_console);
        }
    }
}