using System.Text;
using UnityEngine;
using UFB;

namespace NewGimApp
{
    /// <summary>
    /// Demo entry for the new software workflow:
    /// open .gim -> parse hierarchy/properties -> print summary.
    /// </summary>
    public class GimAppDemoController : MonoBehaviour
    {
        private readonly GimAppParser parser = new GimAppParser();

        public async void ImportGim()
        {
            var path = UniversalFileBrowser.SingleFileDialog("加载GIM文件", null, new[] { new Filter("GIM文件", "gim") });
            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            try
            {
                var document = await parser.ParseAsync(path);
                var sb = new StringBuilder();
                sb.AppendLine("[NewGimApp] Parse completed");
                sb.AppendLine("GIM: " + document.GimPath);
                sb.AppendLine("Extracted: " + document.ExtractedDirectory);
                sb.AppendLine("Node Count: " + CountNodes(document.Root));
                sb.AppendLine("Root: " + (document.Root != null ? document.Root.Name : "<null>"));
                Debug.Log(sb.ToString());
            }
            catch (System.Exception ex)
            {
                Debug.LogError("[NewGimApp] Parse failed: " + ex);
            }
        }

        private int CountNodes(GimNode node)
        {
            if (node == null)
            {
                return 0;
            }

            var count = 1;
            for (int i = 0; i < node.Children.Count; i++)
            {
                count += CountNodes(node.Children[i]);
            }

            return count;
        }
    }
}
