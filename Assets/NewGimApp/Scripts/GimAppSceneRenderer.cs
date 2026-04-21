using System.Collections.Generic;
using System.IO;
using Parabox.Stl;
using UnityEngine;

namespace NewGimApp
{
    /// <summary>
    /// Build Unity scene objects from parsed NewGimApp nodes and provide selection highlight.
    /// </summary>
    public class GimAppSceneRenderer : MonoBehaviour
    {
        public GimAppDemoController demoController;
        public Transform sceneRoot;
        public Color defaultColor = new Color(0.75f, 0.75f, 0.75f, 1f);
        public Color selectedColor = new Color(1f, 0.9f, 0.2f, 1f);

        private readonly List<Renderer> highlightedRenderers = new List<Renderer>();
        private readonly Dictionary<Renderer, Color> rendererOriginalColors = new Dictionary<Renderer, Color>();

        private void Awake()
        {
            if (demoController != null)
            {
                demoController.DocumentParsed += OnDocumentParsed;
            }
        }

        private void OnDestroy()
        {
            if (demoController != null)
            {
                demoController.DocumentParsed -= OnDocumentParsed;
            }
        }

        private void OnDocumentParsed(GimDocument document)
        {
            var root = sceneRoot != null ? sceneRoot : transform;
            ClearChildren(root);

            if (document == null || document.Root == null)
            {
                return;
            }

            BuildNodeObject(document.Root, root);
        }

        public void HighlightNode(GimNode node)
        {
            ClearHighlight();
            if (node == null || node.RuntimeObject == null)
            {
                return;
            }

            var renderers = node.RuntimeObject.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                var r = renderers[i];
                if (r == null || r.sharedMaterial == null)
                {
                    continue;
                }

                if (!rendererOriginalColors.ContainsKey(r))
                {
                    rendererOriginalColors[r] = r.sharedMaterial.color;
                }

                r.sharedMaterial.color = selectedColor;
                highlightedRenderers.Add(r);
            }
        }

        private void ClearHighlight()
        {
            for (int i = 0; i < highlightedRenderers.Count; i++)
            {
                var r = highlightedRenderers[i];
                if (r == null)
                {
                    continue;
                }

                Color originalColor;
                if (rendererOriginalColors.TryGetValue(r, out originalColor) && r.sharedMaterial != null)
                {
                    r.sharedMaterial.color = originalColor;
                }
            }

            highlightedRenderers.Clear();
        }

        private GameObject BuildNodeObject(GimNode node, Transform parent)
        {
            var go = new GameObject(node.Name);
            node.RuntimeObject = go;
            go.transform.SetParent(parent, false);

            BuildVisualForNode(node, go.transform);

            for (int i = 0; i < node.Children.Count; i++)
            {
                BuildNodeObject(node.Children[i], go.transform);
            }

            return go;
        }

        private void BuildVisualForNode(GimNode node, Transform parent)
        {
            if (node.NodeType == "STL")
            {
                string stlPath;
                if (node.Properties.TryGetValue("MODEL_PATH", out stlPath) && File.Exists(stlPath))
                {
                    var meshes = Importer.Import(stlPath, CoordinateSpace.Left, UpAxis.Z);
                    foreach (var mesh in meshes)
                    {
                        var meshGo = new GameObject(Path.GetFileName(stlPath));
                        meshGo.transform.SetParent(parent, false);
                        meshGo.AddComponent<MeshFilter>().sharedMesh = mesh;
                        meshGo.AddComponent<MeshRenderer>().sharedMaterial = CreateMaterial(defaultColor);
                    }
                }
                return;
            }

            if (node.NodeType == "PHM" || node.NodeType == "MOD")
            {
                var placeholder = GameObject.CreatePrimitive(PrimitiveType.Cube);
                placeholder.name = node.NodeType + "-Placeholder";
                placeholder.transform.SetParent(parent, false);
                placeholder.transform.localScale = Vector3.one * 0.5f;
                var renderer = placeholder.GetComponent<MeshRenderer>();
                if (renderer != null)
                {
                    renderer.sharedMaterial = CreateMaterial(defaultColor * 0.9f);
                }
            }
        }

        private Material CreateMaterial(Color color)
        {
            var shader = Shader.Find("Standard");
            var mat = new Material(shader);
            mat.color = color;
            return mat;
        }

        private void ClearChildren(Transform target)
        {
            for (int i = target.childCount - 1; i >= 0; i--)
            {
                Destroy(target.GetChild(i).gameObject);
            }
        }
    }
}
