using System.Collections.Generic;
using System.IO;
using System.Xml;
using MeshMakerNamespace;
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
        public bool enableBooleanCSG = false;
        public int maxBooleanTriangleCount = 20000;

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
            var entityMap = new Dictionary<string, GameObject>();
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
                var ring = entity.SelectSingleNode("Ring");
                var truncatedCone = entity.SelectSingleNode("TruncatedCone");
                var wire = entity.SelectSingleNode("Wire");
                var circularGasket = entity.SelectSingleNode("CircularGasket");
                var porcelainBushing = entity.SelectSingleNode("PorcelainBushing");
                var insulator = entity.SelectSingleNode("Insulator");
                var boolean = entity.SelectSingleNode("Boolean");

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
                else if (ring != null)
                {
                    var r = ParseFloat(ring, "R", 0.5f);
                    var dr = ParseFloat(ring, "DR", 0.1f);
                    shape = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    shape.transform.localScale = new Vector3((r + dr) * 2f, dr, (r + dr) * 2f);
                }
                else if (truncatedCone != null)
                {
                    var br = ParseFloat(truncatedCone, "BR", 0.5f);
                    var tr = ParseFloat(truncatedCone, "TR", 0.25f);
                    var h = ParseFloat(truncatedCone, "H", 1f);
                    shape = CreateFrustum("TruncatedCone", br, tr, h, 24);
                }
                else if (wire != null)
                {
                    var startCoord = ParseVector3(wire, "StartCoord");
                    var endCoord = ParseVector3(wire, "EndCoord");
                    var d = ParseFloat(wire, "D", 0.05f);
                    shape = CreateOrientedCylinder(startCoord, endCoord, d);
                }
                else if (circularGasket != null)
                {
                    var or = ParseFloat(circularGasket, "OR", 0.5f);
                    var h = ParseFloat(circularGasket, "H", 0.1f);
                    shape = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    shape.transform.localScale = new Vector3(or * 2f, h / 2f, or * 2f);
                }
                else if (porcelainBushing != null)
                {
                    var r = ParseFloat(porcelainBushing, "R", 0.3f);
                    var h = ParseFloat(porcelainBushing, "H", 1f);
                    var count = Mathf.Max(1, Mathf.RoundToInt(ParseFloat(porcelainBushing, "N", 4)));
                    shape = CreatePorcelainBushing(r, h, count);
                }
                else if (insulator != null)
                {
                    var n = Mathf.Max(1, Mathf.RoundToInt(ParseFloat(insulator, "N1", 6)));
                    var h1 = ParseFloat(insulator, "H1", 0.12f);
                    var r1 = ParseFloat(insulator, "R1", 0.2f);
                    var r2 = ParseFloat(insulator, "R2", 0.15f);
                    shape = CreateInsulator(r1, r2, h1, n);
                }
                else if (boolean != null)
                {
                    shape = TryApplyBoolean(boolean, entityMap, entityColor);
                }

                if (shape == null)
                {
                    shape = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    shape.transform.localScale = Vector3.one * 0.2f;
                }

                shape.name = entity.Attributes["ID"] != null ? entity.Attributes["ID"].Value : "MOD-Entity";
                shape.transform.SetParent(parent, false);
                if (wire == null)
                {
                    shape.transform.localPosition = matrix.GetT();
                    shape.transform.localRotation = matrix.GetR();
                    shape.transform.localScale = Vector3.Scale(shape.transform.localScale, matrix.GetS());
                }
                else
                {
                    ApplyMatrixToTransform(shape.transform, matrix);
                }
                var renderer = shape.GetComponent<MeshRenderer>();
                if (renderer != null)
                {
                    renderer.sharedMaterial = CreateMaterial(entityColor);
                }

                if (entity.Attributes != null && entity.Attributes["ID"] != null)
                {
                    entityMap[entity.Attributes["ID"].Value] = shape;
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

        private Vector3 ParseVector3(XmlNode node, string attr)
        {
            if (node == null || node.Attributes == null || node.Attributes[attr] == null)
            {
                return Vector3.zero;
            }

            var comps = node.Attributes[attr].Value.Split(',');
            if (comps.Length < 3)
            {
                return Vector3.zero;
            }

            return new Vector3(ParseFloat(comps[0]), ParseFloat(comps[1]), ParseFloat(comps[2]));
        }

        private GameObject CreateOrientedCylinder(Vector3 start, Vector3 end, float diameter)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            var dir = end - start;
            var length = dir.magnitude;
            if (length < 0.0001f)
            {
                length = 0.0001f;
                dir = Vector3.up;
            }
            var center = (start + end) * 0.5f;
            go.transform.localPosition = center;
            go.transform.localRotation = Quaternion.FromToRotation(Vector3.up, dir.normalized);
            go.transform.localScale = new Vector3(diameter, length * 0.5f, diameter);
            return go;
        }

        private GameObject CreateFrustum(string name, float bottomRadius, float topRadius, float height, int segments)
        {
            var go = new GameObject(name);
            var mesh = new Mesh();
            var filter = go.AddComponent<MeshFilter>();
            var renderer = go.AddComponent<MeshRenderer>();
            filter.sharedMesh = mesh;
            renderer.sharedMaterial = CreateMaterial(defaultColor);

            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            for (int i = 0; i < segments; i++)
            {
                var t = (float)i / segments * Mathf.PI * 2f;
                var cos = Mathf.Cos(t);
                var sin = Mathf.Sin(t);
                vertices.Add(new Vector3(cos * bottomRadius, -height * 0.5f, sin * bottomRadius));
                vertices.Add(new Vector3(cos * topRadius, height * 0.5f, sin * topRadius));
            }

            for (int i = 0; i < segments; i++)
            {
                var next = (i + 1) % segments;
                var b0 = i * 2;
                var t0 = b0 + 1;
                var b1 = next * 2;
                var t1 = b1 + 1;
                triangles.Add(b0); triangles.Add(t0); triangles.Add(t1);
                triangles.Add(b0); triangles.Add(t1); triangles.Add(b1);
            }

            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            return go;
        }

        private void ApplyMatrixToTransform(Transform transform, Matrix4x4 matrix)
        {
            transform.localPosition = matrix.GetT() + matrix.GetR() * transform.localPosition;
            transform.localRotation = matrix.GetR() * transform.localRotation;
            transform.localScale = Vector3.Scale(transform.localScale, matrix.GetS());
        }

        private GameObject CreatePorcelainBushing(float radius, float height, int count)
        {
            var root = new GameObject("PorcelainBushing");
            var step = height / count;
            for (int i = 0; i < count; i++)
            {
                var seg = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                seg.transform.SetParent(root.transform, false);
                var rr = radius * (0.85f + 0.15f * Mathf.Sin(i * 0.6f));
                seg.transform.localScale = new Vector3(rr * 2f, step * 0.5f, rr * 2f);
                seg.transform.localPosition = new Vector3(0f, -height * 0.5f + step * (i + 0.5f), 0f);
            }
            return root;
        }

        private GameObject CreateInsulator(float r1, float r2, float unitHeight, int count)
        {
            var root = new GameObject("Insulator");
            for (int i = 0; i < count; i++)
            {
                var disk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                disk.transform.SetParent(root.transform, false);
                var rr = (i % 2 == 0) ? r1 : r2;
                disk.transform.localScale = new Vector3(rr * 2f, unitHeight * 0.25f, rr * 2f);
                disk.transform.localPosition = new Vector3(0f, unitHeight * i, 0f);
            }
            return root;
        }

        private GameObject TryApplyBoolean(XmlNode booleanNode, Dictionary<string, GameObject> entityMap, Color color)
        {
            if (booleanNode == null || booleanNode.Attributes == null)
            {
                return CreateBooleanMarker("Unknown");
            }

            var entity1 = booleanNode.Attributes["Entity1"] != null ? booleanNode.Attributes["Entity1"].Value : string.Empty;
            var entity2 = booleanNode.Attributes["Entity2"] != null ? booleanNode.Attributes["Entity2"].Value : string.Empty;
            var type = booleanNode.Attributes["Type"] != null ? booleanNode.Attributes["Type"].Value : "Unknown";

            if (!enableBooleanCSG)
            {
                return CreateBooleanMarker(type);
            }

            GameObject go1;
            GameObject go2;
            if (!entityMap.TryGetValue(entity1, out go1) || !entityMap.TryGetValue(entity2, out go2) || ShouldSkipBoolean(go1, go2))
            {
                return CreateBooleanMarker(type);
            }

            try
            {
                Mesh result = null;
                if (type == "Difference")
                {
                    result = CSG.Subtract(go1, go2, false, false);
                }
                else if (type == "Union")
                {
                    result = CSG.Union(go1, go2, false, false);
                }

                if (result == null)
                {
                    return CreateBooleanMarker(type);
                }

                var go = new GameObject("Boolean-CSG");
                go.AddComponent<MeshFilter>().sharedMesh = result;
                go.AddComponent<MeshRenderer>().sharedMaterial = CreateMaterial(color);
                return go;
            }
            catch
            {
                return CreateBooleanMarker(type);
            }
        }

        private bool ShouldSkipBoolean(GameObject go1, GameObject go2)
        {
            var mf1 = go1 != null ? go1.GetComponent<MeshFilter>() : null;
            var mf2 = go2 != null ? go2.GetComponent<MeshFilter>() : null;
            if (mf1 == null || mf2 == null || mf1.sharedMesh == null || mf2.sharedMesh == null)
            {
                return true;
            }

            var triangles = (mf1.sharedMesh.triangles.Length + mf2.sharedMesh.triangles.Length) / 3;
            return triangles > maxBooleanTriangleCount;
        }

        private GameObject CreateBooleanMarker(string type)
        {
            var marker = GameObject.CreatePrimitive(PrimitiveType.Cube);
            marker.name = "Boolean-" + type;
            marker.transform.localScale = Vector3.one * 0.25f;
            var renderer = marker.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = CreateMaterial(type == "Difference"
                    ? new Color(1f, 0.45f, 0.45f, 1f)
                    : new Color(0.45f, 0.8f, 1f, 1f));
            }
            return marker;
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
