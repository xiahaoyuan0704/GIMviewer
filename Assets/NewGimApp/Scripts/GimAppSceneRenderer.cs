using System.Collections.Generic;
using System.IO;
using System.Xml;
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
                if (node.Properties.TryGetValue("MODEL_PATH", out stlPath))
                {
                    RenderStl(stlPath, parent, Matrix4x4.identity, defaultColor);
                }
                return;
            }

            if (node.NodeType == "PHM")
            {
                string phmPath;
                if (node.Properties.TryGetValue("PHM_PATH", out phmPath))
                {
                    RenderPhm(phmPath, parent, Matrix4x4.identity);
                }
                return;
            }

            if (node.NodeType == "MOD")
            {
                string modPath;
                if (node.Properties.TryGetValue("MODEL_PATH", out modPath))
                {
                    RenderMod(modPath, parent, Matrix4x4.identity, defaultColor);
                }
                return;
            }
        }

        private void RenderPhm(string phmPath, Transform parent, Matrix4x4 baseMatrix)
        {
            if (!File.Exists(phmPath))
            {
                return;
            }

            var phmDir = Path.GetDirectoryName(phmPath);
            var modDir = Directory.Exists(Path.Combine(Directory.GetParent(phmDir).FullName, "MOD"))
                ? Path.Combine(Directory.GetParent(phmDir).FullName, "MOD")
                : phmDir;
            var phmRootDir = Directory.Exists(Path.Combine(Directory.GetParent(phmDir).FullName, "PHM"))
                ? Path.Combine(Directory.GetParent(phmDir).FullName, "PHM")
                : phmDir;

            var lines = File.ReadAllLines(phmPath);
            for (int i = 0; i < lines.Length; i++)
            {
                var pair = SplitKeyValue(lines[i]);
                if (pair == null || pair.Length < 2 || pair[0] != "SOLIDMODELS.NUM")
                {
                    continue;
                }

                int count;
                if (!int.TryParse(pair[1], out count))
                {
                    continue;
                }

                for (int n = 0; n < count; n++)
                {
                    var modelPair = SplitKeyValue(lines[++i]);
                    var matPair = SplitKeyValue(lines[++i]);
                    var colorPair = SplitKeyValue(lines[++i]);
                    if (modelPair == null || matPair == null || colorPair == null)
                    {
                        continue;
                    }

                    var modelFile = modelPair[1];
                    var mat = ParseMatrix(matPair[1]);
                    var color = ParseColor(colorPair[1]);
                    var world = baseMatrix * mat;
                    var ext = Path.GetExtension(modelFile).ToUpperInvariant();

                    if (ext == ".STL")
                    {
                        RenderStl(Path.Combine(modDir, modelFile), parent, world, color);
                    }
                    else if (ext == ".MOD")
                    {
                        RenderMod(Path.Combine(modDir, modelFile), parent, world, color);
                    }
                    else if (ext == ".PHM")
                    {
                        RenderPhm(Path.Combine(phmRootDir, modelFile), parent, world);
                    }
                }
            }
        }

        private void RenderMod(string modPath, Transform parent, Matrix4x4 baseMatrix, Color fallbackColor)
        {
            if (!File.Exists(modPath))
            {
                return;
            }

            var xml = new XmlDocument();
            xml.Load(modPath);
            var entities = xml.SelectNodes("//Entities/Entity");
            foreach (XmlNode entity in entities)
            {
                var entityColor = fallbackColor;
                var colorNode = entity.SelectSingleNode("Color");
                if (colorNode != null && colorNode.Attributes != null)
                {
                    entityColor = ParseColor(string.Format("{0},{1},{2},{3}",
                        colorNode.Attributes["R"].Value,
                        colorNode.Attributes["G"].Value,
                        colorNode.Attributes["B"].Value,
                        colorNode.Attributes["A"].Value));
                }

                var transformNode = entity.SelectSingleNode("TransformMatrix");
                var matrix = Matrix4x4.identity;
                if (transformNode != null && transformNode.Attributes != null && transformNode.Attributes["Value"] != null)
                {
                    matrix = ParseMatrix(transformNode.Attributes["Value"].Value);
                }
                matrix = baseMatrix * matrix;

                GameObject shape = null;
                var cuboid = entity.SelectSingleNode("Cuboid");
                var cylinder = entity.SelectSingleNode("Cylinder");
                var sphere = entity.SelectSingleNode("Sphere");

                if (cuboid != null)
                {
                    var l = ParseFloat(cuboid, "L", 1f);
                    var w = ParseFloat(cuboid, "W", 1f);
                    var h = ParseFloat(cuboid, "H", 1f);
                    shape = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    shape.transform.localScale = new Vector3(l, w, h);
                }
                else if (cylinder != null)
                {
                    var r = ParseFloat(cylinder, "R", 0.5f);
                    var h = ParseFloat(cylinder, "H", 1f);
                    shape = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    shape.transform.localScale = new Vector3(r * 2f, h / 2f, r * 2f);
                }
                else if (sphere != null)
                {
                    var r = ParseFloat(sphere, "R", 0.5f);
                    shape = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    shape.transform.localScale = Vector3.one * r * 2f;
                }

                if (shape == null)
                {
                    shape = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    shape.transform.localScale = Vector3.one * 0.2f;
                }

                shape.name = entity.Attributes["ID"] != null ? entity.Attributes["ID"].Value : "MOD-Entity";
                shape.transform.SetParent(parent, false);
                shape.transform.localPosition = matrix.GetT();
                shape.transform.localRotation = matrix.GetR();
                shape.transform.localScale = Vector3.Scale(shape.transform.localScale, matrix.GetS());
                var renderer = shape.GetComponent<MeshRenderer>();
                if (renderer != null)
                {
                    renderer.sharedMaterial = CreateMaterial(entityColor);
                }
            }
        }

        private void RenderStl(string stlPath, Transform parent, Matrix4x4 matrix, Color color)
        {
            if (!File.Exists(stlPath))
            {
                return;
            }

            var meshes = Importer.Import(stlPath, CoordinateSpace.Left, UpAxis.Z);
            foreach (var mesh in meshes)
            {
                var meshGo = new GameObject(Path.GetFileName(stlPath));
                meshGo.transform.SetParent(parent, false);
                meshGo.transform.localPosition = matrix.GetT();
                meshGo.transform.localRotation = matrix.GetR();
                meshGo.transform.localScale = matrix.GetS();
                meshGo.AddComponent<MeshFilter>().sharedMesh = mesh;
                meshGo.AddComponent<MeshRenderer>().sharedMaterial = CreateMaterial(color);
            }
        }

        private string[] SplitKeyValue(string line)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                return null;
            }

            var pair = line.Split(new[] { '=' }, 2);
            if (pair.Length < 2)
            {
                return null;
            }

            pair[0] = pair[0].Trim();
            pair[1] = pair[1].Trim();
            return pair;
        }

        private Matrix4x4 ParseMatrix(string raw)
        {
            var comps = raw.Split(',');
            if (comps.Length < 16)
            {
                return Matrix4x4.identity;
            }

            return new Matrix4x4(
                new Vector4(ParseFloat(comps[0]), ParseFloat(comps[1]), ParseFloat(comps[2]), ParseFloat(comps[3])),
                new Vector4(ParseFloat(comps[4]), ParseFloat(comps[5]), ParseFloat(comps[6]), ParseFloat(comps[7])),
                new Vector4(ParseFloat(comps[8]), ParseFloat(comps[9]), ParseFloat(comps[10]), ParseFloat(comps[11])),
                new Vector4(ParseFloat(comps[12]), ParseFloat(comps[13]), ParseFloat(comps[14]), ParseFloat(comps[15]))
            );
        }

        private Color ParseColor(string raw)
        {
            var comps = raw.Split(',');
            if (comps.Length < 3)
            {
                return defaultColor;
            }

            var r = ParseFloat(comps[0]) / 255f;
            var g = ParseFloat(comps[1]) / 255f;
            var b = ParseFloat(comps[2]) / 255f;
            var a = comps.Length > 3 ? ParseFloat(comps[3]) : 255f;
            if (a > 1f) a /= 255f;
            return new Color(r, g, b, a);
        }

        private float ParseFloat(XmlNode node, string attr, float fallback)
        {
            if (node == null || node.Attributes == null || node.Attributes[attr] == null)
            {
                return fallback;
            }

            float result;
            if (float.TryParse(node.Attributes[attr].Value, out result))
            {
                return result;
            }
            return fallback;
        }

        private float ParseFloat(string raw)
        {
            float result;
            if (float.TryParse(raw, out result))
            {
                return result;
            }
            return 0f;
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
