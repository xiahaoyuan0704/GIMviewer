using System.Collections.Generic;
using System.Linq;
using System.Xml;
using UnityEngine;
using System.IO;
using System;
using UnityEngine.UI;
using System.Diagnostics;
using UnityEngine.ProBuilder;
using UnityEngine.ProBuilder.MeshOperations;
using Math = System.Math;
using Parabox.Stl;
using System.Collections;
using UnityEngine.Events;
using static UniStorm.Example.GimUIController;
using UniStorm.Example;
using Unity.VisualScripting;

public class CreateModel : MonoBehaviour
{
    public int smooth = 20;
    [Header("Large Model Performance")]
    [Tooltip("Maximum milliseconds spent building GIM geometry before yielding to the next frame.")]
    public float maxBuildMillisecondsPerFrame = 8f;
    [Tooltip("Generate MeshCollider components for combined GIM meshes. Disable for smoother loading and rendering when click/physics picking is not required.")]
    public bool generateMeshColliders = true;
    //public TreeView TreeView;
    public Material material;
    public List<GameObject> ProBuilderMeshItem;
    public Dictionary<string, ProBuilderMesh> ProBuilderMeshDictionary = new Dictionary<string, ProBuilderMesh>();
    public GameObject item;
    public GameObject content;

    private Dictionary<Color, Material> matDic = new Dictionary<Color, Material>();
    private GameObject rootGameObject;
    private float ratio = 0.001f;
    private const int MaxRecursiveDepth = 64;
    private readonly HashSet<string> phmRecursionStack = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> devRecursionStack = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, Dictionary<string, string>> propertiesCache = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, Dictionary<string, string>> devPropertiesCache = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, Dictionary<string, string>> famPropertiesCache = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
    private float frameBudgetStartTime;

    private static string _7zExeUrl
    {
        get
        {
            return Path.Combine(Application.streamingAssetsPath, "7-Zip/7z.exe");
        }
    }
    private static string _Ifc2XbimUrl
    {
        get
        {
            return Path.Combine(Application.streamingAssetsPath, "ifc2xbim/ifc2xbim.exe");
        }
    }
    private string fileUrl = "";
    private List<Dictionary<string, string>> items = new List<Dictionary<string, string>>();
    public GameObject viewpoint;
    public Button selectButton;
    private string path;

    public List<MyCustomData> TreeGameObjects = new List<MyCustomData>();
    //public Table table;
    public double CalculateDistance(string coord1, string coord2)
    {
        // 将字符串坐标解析为浮点数数组
        double[] coordArray1 = Array.ConvertAll(coord1.Split(','), double.Parse);
        double[] coordArray2 = Array.ConvertAll(coord2.Split(','), double.Parse);

        // 计算两个坐标之间的距离
        double distance = Math.Sqrt(Math.Pow(coordArray2[0] - coordArray1[0], 2) + Math.Pow(coordArray2[1] - coordArray1[1], 2) + Math.Pow(coordArray2[2] - coordArray1[2], 2));
        return distance;
    }

    private void ResetFrameBudget()
    {
        frameBudgetStartTime = Time.realtimeSinceStartup;
    }

    private bool ShouldYieldForFrameBudget()
    {
        if (maxBuildMillisecondsPerFrame <= 0f)
        {
            return false;
        }

        return (Time.realtimeSinceStartup - frameBudgetStartTime) * 1000f >= maxBuildMillisecondsPerFrame;
    }

    private void MarkMeshAsStatic(GameObject gameObject)
    {
        gameObject.isStatic = true;
    }

    private Material GetMaterialByColor(Color color)
    {
        Material mat;
        if (matDic.ContainsKey(color))
        {
            mat = matDic[color];
        }
        else
        {
            mat = new Material(Shader.Find("Standard"));
            mat.SetColor("_Color", color);
            matDic.Add(color, mat);
        }
        return mat;
    }

    private List<ProBuilderMesh> proBuilderMeshes = new List<ProBuilderMesh>(20);
    private Dictionary<string, ProBuilderMesh> proBuilderMeshDic = new Dictionary<string, ProBuilderMesh>(20);
    private List<ProBuilderMesh> proBuilderMeshes2 = new List<ProBuilderMesh>(20);
    private List<ProBuilderMesh> proBuilderMeshes3 = new List<ProBuilderMesh>(20);

    IEnumerator getModel3(string path, Matrix4x4 matrix, string modelName, UnityAction<ProBuilderMesh> action)
    {
        proBuilderMeshes.Clear();
        proBuilderMeshDic.Clear();
        path = path.Replace("\\", "/");
        Toast.Show("加载 " + string.Format("{0}...{1}", path.Substring(0, path.IndexOf("/")), path.Substring(path.LastIndexOf("/"))));
        var m4 = matrix;
        XmlDocument xmlObj = new XmlDocument();
        xmlObj.Load(path);
        XmlNode root = xmlObj.DocumentElement;
        XmlNodeList entityNodes = root.SelectNodes("Entities/Entity");
        foreach (XmlNode entityNode in entityNodes)
        {
            string id = entityNode.Attributes["ID"].Value;
            string type = entityNode.Attributes["Type"].Value;
            string visible = entityNode.Attributes["Visible"].Value;
            // ��ȡColor�ڵ������ֵ
            XmlNode colorNode = entityNode.SelectSingleNode("Color");
            string red = colorNode.Attributes["R"].Value;
            string green = colorNode.Attributes["G"].Value;
            string blue = colorNode.Attributes["B"].Value;
            string alpha = colorNode.Attributes["A"].Value;
            UnityEngine.Color color = new UnityEngine.Color(float.Parse(red) / 255, float.Parse(green) / 255, float.Parse(blue) / 255, float.Parse(alpha)); // ����RGBA��ɫֵ
            var mat = GetMaterialByColor(color);
            // ��ȡStretchedBody�ڵ������ֵ
            XmlNode TransformMatrix = entityNode.SelectSingleNode("TransformMatrix");
            XmlNode TerminalBlock = entityNode.SelectSingleNode("TerminalBlock");
            XmlNode Cylinder = entityNode.SelectSingleNode("Cylinder");
            XmlNode stretchedBodyNode = entityNode.SelectSingleNode("StretchedBody");
            XmlNode Cuboid = entityNode.SelectSingleNode("Cuboid");
            XmlNode Ring = entityNode.SelectSingleNode("Ring");
            XmlNode TruncatedCone = entityNode.SelectSingleNode("TruncatedCone");
            XmlNode PorcelainBushing = entityNode.SelectSingleNode("PorcelainBushing");
            XmlNode Sphere = entityNode.SelectSingleNode("Sphere");
            XmlNode Wire = entityNode.SelectSingleNode("Wire");
            XmlNode Insulator = entityNode.SelectSingleNode("Insulator");
            XmlNode Boolean = entityNode.SelectSingleNode("Boolean");
            XmlNode CircularGasket = entityNode.SelectSingleNode("CircularGasket");
            XmlNode OffsetRectangularTable = entityNode.SelectSingleNode("OffsetRectangularTable");
            XmlNode RotationalEllipsoid = entityNode.SelectSingleNode("RotationalEllipsoid");
            XmlNode RoundSteelTube = entityNode.SelectSingleNode("RoundSteelTube");
            XmlNode EquilateralAngleSteel = entityNode.SelectSingleNode("EquilateralAngleSteel");
            XmlNode FlatSteel = entityNode.SelectSingleNode("FlatSteel");
            var v = TransformMatrix.Attributes["Value"].Value;
            string[] stringsv = v.Split(",");
            var m = new Matrix4x4(new Vector4(float.Parse(stringsv[0]), float.Parse(stringsv[1]), float.Parse(stringsv[2]), float.Parse(stringsv[3])), new Vector4(float.Parse(stringsv[4]), float.Parse(stringsv[5]), float.Parse(stringsv[6]), float.Parse(stringsv[7])), new Vector4(float.Parse(stringsv[8]), float.Parse(stringsv[9]), float.Parse(stringsv[10]), float.Parse(stringsv[11])), new Vector4(float.Parse(stringsv[12]) * ratio, float.Parse(stringsv[13]) * ratio, float.Parse(stringsv[14]) * ratio, float.Parse(stringsv[15])));
            m = m4 * m;
            if (stretchedBodyNode != null)
            {
                string length = stretchedBodyNode.Attributes["L"].Value;
                string normal = stretchedBodyNode.Attributes["Normal"].Value;
                string array = stretchedBodyNode.Attributes["Array"].Value;
                string[] strings = array.Split(";");
                string[] normals = normal.Split(",");
                Vector3 vector3Normal = new Vector3(float.Parse(normals[0]), float.Parse(normals[1]), float.Parse(normals[2]));
                Vector3[] vector3s = new Vector3[strings.Length];
                for (int i = 0; i < strings.Length; i++)
                {
                    string[] strings1 = strings[i].Split(",");
                    vector3s[i] = new Vector3(float.Parse(strings1[0]), float.Parse(strings1[1]), float.Parse(strings1[2])) * ratio;
                }
                ProBuilderMesh proBuilderMesh = ProBuilderMesh.Create();
                proBuilderMesh.gameObject.transform.SetParent(rootGameObject.transform, false);
                proBuilderMesh.gameObject.transform.localPosition = m.GetT();
                proBuilderMesh.gameObject.transform.localRotation = m.GetR();
                proBuilderMesh.gameObject.transform.localScale = m.GetS();

                proBuilderMesh.CreateShapeFromPolygon(vector3s, 0f, false);

                UnityEngine.ProBuilder.Vertex[] vertices = proBuilderMesh.GetVertices();

                if (Vector3.Dot(vertices[0].normal, vector3Normal) < 0)
                {
                    proBuilderMesh.faces[0].Reverse();
                }

                proBuilderMesh.ToMesh();


                IList<Face> faces = proBuilderMesh.faces;
                Face[] facesFace = new Face[faces.Count];
                Face[] facesEx = new Face[1];
                for (int i = 0; i < faces.Count; i++)
                {
                    facesFace[i] = faces[i];
                }
                facesEx[0] = facesFace[0];
                proBuilderMesh.DuplicateAndFlip(facesFace);
                proBuilderMesh.Extrude(facesEx, ExtrudeMethod.IndividualFaces, float.Parse(length) * ratio);
                proBuilderMesh.ToMesh();
                proBuilderMesh.Refresh();

                proBuilderMesh.gameObject.GetComponent<MeshRenderer>().material = mat;

                proBuilderMesh.name = visible;
                proBuilderMeshes.Add(proBuilderMesh);
                proBuilderMeshDic.Add(id, proBuilderMesh);
                MarkMeshAsStatic(proBuilderMesh.gameObject);
                if (ShouldYieldForFrameBudget())
                {
                    yield return null;
                    ResetFrameBudget();
                }
            }
            else if (Cuboid != null)
            {
                float L = float.Parse(Cuboid.Attributes["L"].Value) * ratio;
                float W = float.Parse(Cuboid.Attributes["W"].Value) * ratio;
                float H = float.Parse(Cuboid.Attributes["H"].Value) * ratio;


                ProBuilderMesh proBuilderMesh = ProBuilderMesh.Create();
                proBuilderMesh.gameObject.transform.SetParent(rootGameObject.transform, false);
                proBuilderMesh.gameObject.transform.localPosition = m.GetT();
                proBuilderMesh.gameObject.transform.localRotation = m.GetR();
                proBuilderMesh.gameObject.transform.localScale = m.GetS();
                proBuilderMesh.CreateShapeFromPolygon(new Vector3[]
                {
                    new Vector3(L/2,-W/2,0),
                    new Vector3(L/2,-W/2,H),
                    new Vector3(-L/2,-W/2,H),
                    new Vector3(-L/2,-W/2,0)
                }, W, false);
                proBuilderMesh.ToMesh();
                proBuilderMesh.gameObject.GetComponent<MeshRenderer>().material = GetMaterialByColor(color);
                proBuilderMesh.name = visible;
                proBuilderMeshes.Add(proBuilderMesh);
                proBuilderMeshDic.Add(id, proBuilderMesh);
                MarkMeshAsStatic(proBuilderMesh.gameObject);
                if (ShouldYieldForFrameBudget())
                {
                    yield return null;
                    ResetFrameBudget();
                }
            }
            else if (TerminalBlock != null)
            {
                var L = float.Parse(TerminalBlock.Attributes["L"].Value) * ratio;
                var W = float.Parse(TerminalBlock.Attributes["W"].Value) * ratio;
                var T = float.Parse(TerminalBlock.Attributes["T"].Value) * ratio;
                var CL = float.Parse(TerminalBlock.Attributes["CL"].Value) * ratio;
                var CS = float.Parse(TerminalBlock.Attributes["CS"].Value) * ratio;
                var RS = float.Parse(TerminalBlock.Attributes["RS"].Value) * ratio;
                var R = float.Parse(TerminalBlock.Attributes["R"].Value) * ratio;
                var CN = float.Parse(TerminalBlock.Attributes["CN"].Value);
                var RN = float.Parse(TerminalBlock.Attributes["RN"].Value);
                var BL = float.Parse(TerminalBlock.Attributes["BL"].Value) * ratio;
                //var Phase = float.Parse(TerminalBlock.Attributes["Phase"].Value);
                ProBuilderMesh proBuilderMesh = ProBuilderMesh.Create();
                proBuilderMesh.gameObject.transform.SetParent(rootGameObject.transform, false);
                IList<IList<Vector3>> list = new List<IList<Vector3>>();
                float cW = W / 2;
                if (CN > 1)
                {
                    cW = W / (CS + 1);
                }

                float J = Mathf.Deg2Rad * (360 / smooth);
                for (int row = 0; row < RN; row++)
                {
                    for (int col = 0; col < CN; col++)
                    {
                        Vector3 o = new Vector3();
                        o.x = W / 2 - cW * (col + 1);
                        o.y = -T / 2;
                        o.z = BL + RS * row;
                        var points = new List<Vector3>();
                        for (int i = 0; i < 10; i++)
                        {
                            Vector3 p = new Vector3();
                            p.y = -T / 2;
                            p.z = R * Mathf.Cos(J * i) + o.z;
                            p.x = R * Mathf.Sin(J * i) + o.x;
                            points.Add(p);
                        }
                        list.Add(points);
                    }
                }

                proBuilderMesh.CreateShapeFromPolygon(new Vector3[]
                {
                    new Vector3( W / 2,-T/2, 0),
                    new Vector3( W / 2,-T/2, L - CL),
                    new Vector3( W / 2 - CL,-T/2, L),
                    new Vector3( -W / 2 + CL,-T/2, L),
                    new Vector3( -W / 2,-T/2, L - CL),
                    new Vector3( -W / 2,-T/2, 0)
                }, T, false);
                proBuilderMesh.name = "TerminalBlock";

                proBuilderMesh.gameObject.transform.localPosition = m.GetT();
                proBuilderMesh.gameObject.transform.localRotation = m.GetR();
                proBuilderMesh.gameObject.transform.localScale = m.GetS();
                proBuilderMesh.gameObject.GetComponent<MeshRenderer>().material = GetMaterialByColor(color);
                proBuilderMesh.name = visible;
                proBuilderMeshes.Add(proBuilderMesh);
                proBuilderMeshDic.Add(id, proBuilderMesh);
                MarkMeshAsStatic(proBuilderMesh.gameObject);
                if (ShouldYieldForFrameBudget())
                {
                    yield return null;
                    ResetFrameBudget();
                }
            }
            else if (Cylinder != null)
            {
                var R = float.Parse(Cylinder.Attributes["R"].Value) * ratio;
                var H = float.Parse(Cylinder.Attributes["H"].Value) * ratio;
                ProBuilderMesh proBuilderMesh = ProBuilderMesh.Create();
                proBuilderMesh.gameObject.transform.SetParent(rootGameObject.transform, false);

                float J = Mathf.Deg2Rad * (360 / smooth);
                var points = new List<Vector3>();
                for (int i = 0; i < smooth; i++)
                {
                    Vector3 p = new Vector3();
                    p.z = 0;
                    p.x = R * Mathf.Cos(J * i);
                    p.y = R * Mathf.Sin(J * i);
                    //points.Add(m1.MultiplyPoint(p));
                    points.Add(p);
                }
                proBuilderMesh.CreateShapeFromPolygon(points, H, false);


                proBuilderMesh.gameObject.transform.localPosition = m.GetT();
                proBuilderMesh.gameObject.transform.localRotation = m.GetR();
                proBuilderMesh.gameObject.transform.localScale = m.GetS();

                proBuilderMesh.gameObject.GetComponent<MeshRenderer>().material = GetMaterialByColor(color);
                proBuilderMesh.name = visible;
                proBuilderMeshes.Add(proBuilderMesh);
                proBuilderMeshDic.Add(id, proBuilderMesh);
                MarkMeshAsStatic(proBuilderMesh.gameObject);
                if (ShouldYieldForFrameBudget())
                {
                    yield return null;
                    ResetFrameBudget();
                }
            }
            else if (Ring != null)
            {
                var R = float.Parse(Ring.Attributes["R"].Value) * ratio;
                var DR = float.Parse(Ring.Attributes["DR"].Value) * ratio;
                var Rad = float.Parse(Ring.Attributes["Rad"].Value);
                m = m * Matrix4x4.Rotate(Quaternion.Euler(90, 0, 0));
                ProBuilderMesh proBuilderMesh = ShapeGenerator.GenerateTorus(PivotLocation.FirstCorner, 16, 24, R + DR, DR, true, 90, 360);
                proBuilderMesh.gameObject.transform.SetParent(rootGameObject.transform, false);

                proBuilderMesh.gameObject.GetComponent<MeshRenderer>().material = GetMaterialByColor(color);


                proBuilderMesh.SetPivot(Vector3.zero);


                proBuilderMesh.gameObject.transform.localPosition = m.GetT();
                proBuilderMesh.gameObject.transform.localRotation = m.GetR();
                proBuilderMesh.gameObject.transform.localScale = m.GetS();
                proBuilderMesh.ToMesh();

                proBuilderMesh.name = visible;
                proBuilderMeshes.Add(proBuilderMesh);
                proBuilderMeshDic.Add(id, proBuilderMesh);
                MarkMeshAsStatic(proBuilderMesh.gameObject);
                if (ShouldYieldForFrameBudget())
                {
                    yield return null;
                    ResetFrameBudget();
                }
            }
            else if (TruncatedCone != null)
            {
                var BR = float.Parse(TruncatedCone.Attributes["BR"].Value) * ratio;
                var TR = float.Parse(TruncatedCone.Attributes["TR"].Value) * ratio;
                var H = float.Parse(TruncatedCone.Attributes["H"].Value) * ratio;
                //m = m4 * m;
                float J = Mathf.Deg2Rad * (360 / smooth);
                var pointsR = new List<Vector3>();
                for (int i = 0; i < smooth; i++)
                {
                    Vector3 p = new Vector3();
                    p.z = 0;
                    p.x = TR * Mathf.Cos(J * i);
                    p.y = TR * Mathf.Sin(J * i);
                    pointsR.Add(p);
                }
                ProBuilderMesh proBuilderMeshMin = ProBuilderMesh.Create();
                proBuilderMeshMin.gameObject.transform.SetParent(rootGameObject.transform, false);
                proBuilderMeshMin.CreateShapeFromPolygon(pointsR, 0f, false);

                ProBuilderMesh proBuilderMeshMax = ProBuilderMesh.Create();
                proBuilderMeshMax.gameObject.transform.SetParent(rootGameObject.transform, false);
                proBuilderMeshMax.CreateShapeFromPolygon(pointsR, 0f, false);
                Matrix4x4 matrix4x4S = Matrix4x4.Scale(new Vector3(BR / TR, BR / TR, 1));
                Matrix4x4 matrix4x4R = Matrix4x4.Translate(new Vector3(0, 0, H));
                UnityEngine.ProBuilder.Vertex[] verticesMax = proBuilderMeshMax.GetVertices();
                UnityEngine.ProBuilder.Vertex[] verticesMin = proBuilderMeshMax.GetVertices();
                for (int i = 0; i < verticesMax.Length; i++)
                {
                    verticesMax[i].position = matrix4x4S.MultiplyPoint3x4(verticesMax[i].position);
                }
                proBuilderMeshMax.SetVertices(verticesMax);
                proBuilderMeshMax.ToMesh();
                for (int i = 0; i < verticesMin.Length; i++)
                {
                    verticesMin[i].position = matrix4x4R.MultiplyPoint3x4(verticesMin[i].position);
                }
                proBuilderMeshMin.SetVertices(verticesMin);
                proBuilderMeshMin.ToMesh();
                proBuilderMeshes2.Clear();
                proBuilderMeshes2.Add(proBuilderMeshMin);
                proBuilderMeshes2.Add(proBuilderMeshMax);
                CombineMeshes.Combine(proBuilderMeshes2, proBuilderMeshMin);
                Destroy(proBuilderMeshes2[1]);
                for (int i = 0; i < smooth; i++)
                {
                    Face face = proBuilderMeshMin.Bridge(proBuilderMeshMin.faces[1].edges[i], proBuilderMeshMin.faces[0].edges[i]);
                    face.Reverse();
                }
                /*proBuilderMeshMin.faces[0].Reverse();*/
                proBuilderMeshMin.faces[1].Reverse();
                Destroy(proBuilderMeshMax.gameObject);
                proBuilderMeshMin.ToMesh();
                proBuilderMeshMin.Refresh();
                proBuilderMeshMin.gameObject.transform.localPosition = m.GetT();
                proBuilderMeshMin.gameObject.transform.localRotation = m.GetR();
                proBuilderMeshMin.gameObject.transform.localScale = m.GetS();
                proBuilderMeshMin.gameObject.GetComponent<MeshRenderer>().material = GetMaterialByColor(color);
                proBuilderMeshes.Add(proBuilderMeshMin);

                proBuilderMeshMin.name = visible;
                proBuilderMeshes.Add(proBuilderMeshMin);
                proBuilderMeshDic.Add(id, proBuilderMeshMin);
                MarkMeshAsStatic(proBuilderMeshMin.gameObject);
                if (ShouldYieldForFrameBudget())
                {
                    yield return null;
                    ResetFrameBudget();
                }
            }
            else if (PorcelainBushing != null)
            {
                var R1 = float.Parse(PorcelainBushing.Attributes["R1"].Value) * ratio;
                var R2 = float.Parse(PorcelainBushing.Attributes["R2"].Value) * ratio;
                var R = float.Parse(PorcelainBushing.Attributes["R"].Value) * ratio;
                var H = float.Parse(PorcelainBushing.Attributes["H"].Value) * ratio;
                var N = float.Parse(PorcelainBushing.Attributes["N"].Value);
                float J = Mathf.Deg2Rad * (360 / smooth);
                var pointsH = new List<Vector3>();
                for (int i = 0; i < smooth; i++)
                {
                    Vector3 p = new Vector3();
                    p.z = 0;
                    p.x = R * Mathf.Cos(J * i);
                    p.y = R * Mathf.Sin(J * i);
                    pointsH.Add(p);
                }

                ProBuilderMesh proBuilderMeshH = ProBuilderMesh.Create();
                proBuilderMeshH.CreateShapeFromPolygon(pointsH, H, false);
                proBuilderMeshH.ToMesh();
                proBuilderMeshH.Refresh();
                proBuilderMeshH.gameObject.transform.SetParent(rootGameObject.transform, false);
                float weizhi = 0;
                float nmax = H / (N * 4);
                proBuilderMeshes2.Clear();
                for (int i = 0; i < N; i++)
                {
                    if (i != 0)
                    {
                        weizhi = weizhi + nmax * 2;
                    }
                    ProBuilderMesh proBuilderMesh = createPorcelainBushing(R, R2, nmax);
                    proBuilderMesh.name = "min1";
                    Matrix4x4 matrix4x40 = Matrix4x4.Translate(new Vector3(0, 0, weizhi));
                    UnityEngine.ProBuilder.Vertex[] vertices0 = proBuilderMesh.GetVertices();
                    for (int j = 0; j < vertices0.Length; j++)
                    {
                        vertices0[j].position = matrix4x40.MultiplyPoint3x4(vertices0[j].position);
                    }
                    proBuilderMesh.SetVertices(vertices0);
                    proBuilderMesh.ToMesh();
                    proBuilderMesh.gameObject.GetComponent<MeshRenderer>().material = GetMaterialByColor(color);
                    weizhi = weizhi + nmax;


                    ProBuilderMesh proBuilderMesh2 = createPorcelainBushing(R, R1, nmax);
                    proBuilderMesh2.name = "min2";
                    Matrix4x4 matrix4x41 = Matrix4x4.Translate(new Vector3(0, 0, weizhi + nmax));
                    UnityEngine.ProBuilder.Vertex[] vertices = proBuilderMesh2.GetVertices();
                    for (int j = 0; j < vertices.Length; j++)
                    {
                        vertices[j].position = matrix4x41.MultiplyPoint3x4(vertices[j].position);
                    }
                    proBuilderMesh2.SetVertices(vertices);
                    proBuilderMesh2.ToMesh();

                    proBuilderMesh2.gameObject.GetComponent<MeshRenderer>().material = GetMaterialByColor(color);
                    weizhi = weizhi + nmax;

                    proBuilderMeshes2.Add(proBuilderMesh);
                    proBuilderMeshes2.Add(proBuilderMesh2);
                }

                proBuilderMeshH.gameObject.GetComponent<MeshRenderer>().material = GetMaterialByColor(color);
                proBuilderMeshes2.Add(proBuilderMeshH);
                CombineMeshes.Combine(proBuilderMeshes2, proBuilderMeshes2[proBuilderMeshes2.Count - 1]);
                for (int i = 0; i < proBuilderMeshes2.Count - 1; i++)
                {

                    Destroy(proBuilderMeshes2[i].gameObject);
                }
                proBuilderMeshH.gameObject.transform.localPosition = m.GetT();
                proBuilderMeshH.gameObject.transform.localRotation = m.GetR();
                proBuilderMeshH.gameObject.transform.localScale = m.GetS();

                proBuilderMeshH.name = visible;
               proBuilderMeshes.Add(proBuilderMeshH);
               proBuilderMeshDic.Add(id, proBuilderMeshH);
                    MarkMeshAsStatic(proBuilderMeshH.gameObject);
                    if (ShouldYieldForFrameBudget())
                    {
                        yield return null;
                        ResetFrameBudget();
                    }
            }
            else if (Sphere != null)
            {
                var R = float.Parse(Sphere.Attributes["R"].Value) * ratio;

                ProBuilderMesh proBuilderMesh = ShapeGenerator.GenerateTorus(PivotLocation.Center, 16, 24, R, R, true, 360, 360);
                proBuilderMesh.gameObject.transform.SetParent(rootGameObject.transform, false);
                proBuilderMesh.gameObject.GetComponent<MeshRenderer>().material = GetMaterialByColor(color);
                proBuilderMesh.gameObject.transform.localPosition = m.GetT();
                proBuilderMesh.gameObject.transform.localRotation = m.GetR();
                proBuilderMesh.gameObject.transform.localScale = m.GetS();

                proBuilderMesh.name = visible;
                proBuilderMeshes.Add(proBuilderMesh);
                proBuilderMeshDic.Add(id, proBuilderMesh);
                MarkMeshAsStatic(proBuilderMesh.gameObject);
                if (ShouldYieldForFrameBudget())
                {
                    yield return null;
                    ResetFrameBudget();
                }
            }
            else if (Wire != null)
            {
                var StartCoord = Wire.Attributes["StartCoord"].Value;
                var REndCoord = Wire.Attributes["EndCoord"].Value;
                var StartVector = Wire.Attributes["StartVector"].Value;
                var EndVector = Wire.Attributes["EndVector"].Value;
                var Sag = Wire.Attributes["Sag"].Value;
                var D = Wire.Attributes["D"].Value;
                var FitCoordArray = Wire.Attributes["FitCoordArray"].Value;
                string[] StartVectors = StartVector.Split(",");
                string[] EndVectors = EndVector.Split(",");
                string[] StartCoords = StartCoord.Split(",");
                string[] REndCoords = REndCoord.Split(",");

                Vector3 vector3 = new Vector3(float.Parse(StartCoords[0]), float.Parse(StartCoords[1]), float.Parse(StartCoords[2])) * ratio;

                Vector3 vector31 = new Vector3(float.Parse(REndCoords[0]), float.Parse(REndCoords[1]), float.Parse(REndCoords[2])) * ratio;

                var length = (vector3 - vector31).magnitude;
                var dir = (vector31 - vector3).normalized;
                var center = vector3 + dir * length / 2;
                Quaternion q = Quaternion.LookRotation(dir);
                m = m * Matrix4x4.Translate(center);
                m = m * Matrix4x4.Rotate(q);
                ProBuilderMesh proBuilderMesh = ShapeGenerator.GenerateCylinder(PivotLocation.Center, 3, float.Parse(D) * ratio / 2, length, 1);
                proBuilderMesh.gameObject.transform.SetParent(rootGameObject.transform, false);
                var vertices = proBuilderMesh.GetVertices();
                foreach (var vertex in vertices)
                {
                    vertex.position = Quaternion.Euler(90, 0, 0) * vertex.position;
                    vertex.normal = Quaternion.Euler(90, 0, 0) * vertex.normal;
                }
                proBuilderMesh.SetVertices(vertices);
                proBuilderMesh.ToMesh();
                proBuilderMesh.Refresh();
                proBuilderMesh.name = modelName;
                proBuilderMesh.gameObject.GetComponent<MeshRenderer>().material = GetMaterialByColor(color);

                proBuilderMesh.transform.localPosition = m.GetT();
                proBuilderMesh.transform.localRotation = m.GetR();
                proBuilderMesh.transform.localScale = m.GetS();

                proBuilderMesh.name = visible;
                proBuilderMeshes.Add(proBuilderMesh);
                proBuilderMeshDic.Add(id, proBuilderMesh);
                MarkMeshAsStatic(proBuilderMesh.gameObject);
                if (ShouldYieldForFrameBudget())
                {
                    yield return null;
                    ResetFrameBudget();
                }
            }
            else if (Insulator != null)
            {
                var N = int.Parse(Insulator.Attributes["N"].Value);
                var D = float.Parse(Insulator.Attributes["D"].Value) * ratio;
                var N1 = int.Parse(Insulator.Attributes["N1"].Value);//绝缘子数量
                var H1 = float.Parse(Insulator.Attributes["H1"].Value) * ratio;
                var R1 = float.Parse(Insulator.Attributes["R1"].Value) * ratio;
                var R2 = float.Parse(Insulator.Attributes["R2"].Value) * ratio;
                var R = float.Parse(Insulator.Attributes["R"].Value) * ratio;
                var FL = float.Parse(Insulator.Attributes["FL"].Value) * ratio;
                var AL = float.Parse(Insulator.Attributes["AL"].Value) * ratio;
                var LN = int.Parse(Insulator.Attributes["LN"].Value);
                m = m * Matrix4x4.Rotate(Quaternion.Euler(0, -90, 0));
                //单串绝缘子
                if (N == 1)
                {
                    float J = Mathf.Deg2Rad * (360 / smooth);
                    var pointsH = new List<Vector3>();
                    for (int i = 0; i < smooth; i++)
                    {
                        Vector3 p = new Vector3();
                        p.z = 0;
                        p.x = R * Mathf.Sin(J * i);
                        p.y = R * Mathf.Cos(J * i);
                        pointsH.Add(p);
                    }
                    ProBuilderMesh proBuilderMeshH = ProBuilderMesh.Create();
                    proBuilderMeshH.gameObject.transform.SetParent(rootGameObject.transform, false);
                    proBuilderMeshH.CreateShapeFromPolygon(pointsH, FL + N1 * H1 + AL, false);

                    proBuilderMeshH.gameObject.GetComponent<MeshRenderer>().material = GetMaterialByColor(color);
                    proBuilderMeshH.ToMesh();
                    proBuilderMeshH.Refresh();

                    proBuilderMeshH.transform.localRotation = Quaternion.Euler(0, 180, 0);
                    //m = m4 * m;
                    float weizhi = FL;
                    float nmax = (N1 * H1) / (N1);
                    proBuilderMeshes2.Clear();
                    proBuilderMeshes2.Add(proBuilderMeshH);
                    for (int i = 0; i < N1; i++)
                    {
                        if (i != 0)
                        {
                            weizhi = weizhi + nmax / 2;
                        }
                        ProBuilderMesh proBuilderMesh = createPorcelainBushing(R, i % 2 == 0 ? R2 : R1, nmax / 2);
                        proBuilderMesh.name = "min1";
                        Matrix4x4 matrix4x40 = Matrix4x4.Translate(new Vector3(0, 0, weizhi));
                        Vertex[] vertices0 = proBuilderMesh.GetVertices();
                        for (int j = 0; j < vertices0.Length; j++)
                        {
                            vertices0[j].position = matrix4x40.MultiplyPoint3x4(vertices0[j].position);
                        }
                        proBuilderMesh.SetVertices(vertices0);
                        proBuilderMesh.ToMesh();
                        proBuilderMesh.gameObject.GetComponent<MeshRenderer>().material = GetMaterialByColor(color);
                        weizhi = weizhi + nmax / 2;
                        proBuilderMesh.name = id;
                        proBuilderMeshes2.Add(proBuilderMesh);
                    }
                    CombineMeshes.Combine(proBuilderMeshes2, proBuilderMeshH);
                    for (int i = 1; i < proBuilderMeshes2.Count; i++)
                    {
                        Destroy(proBuilderMeshes2[i].gameObject);
                    }

                    proBuilderMeshH.gameObject.transform.localPosition = m.GetT();
                    proBuilderMeshH.gameObject.transform.localRotation = m.GetR();
                    proBuilderMeshH.gameObject.transform.localScale = m.GetS();

                    proBuilderMeshH.name = visible;
                    proBuilderMeshes.Add(proBuilderMeshH);
                    proBuilderMeshDic.Add(id, proBuilderMeshH);
                    MarkMeshAsStatic(proBuilderMeshH.gameObject);
                    if (ShouldYieldForFrameBudget())
                    {
                        yield return null;
                        ResetFrameBudget();
                    }
                }
            }
            else if (CircularGasket != null)
            {
                var OR = float.Parse(CircularGasket.Attributes["OR"].Value) * ratio;
                var IR = float.Parse(CircularGasket.Attributes["IR"].Value) * ratio;
                var H = float.Parse(CircularGasket.Attributes["H"].Value) * ratio;
                ProBuilderMesh proBuilderMesh = ShapeGenerator.GeneratePipe(PivotLocation.Center, OR, H, OR - IR, smooth, smooth);
                proBuilderMesh.gameObject.transform.SetParent(rootGameObject.transform, false);

                proBuilderMesh.gameObject.transform.localPosition = new Vector3(m.GetT().x, m.GetT().y, m.GetT().z + (H / 2));
                proBuilderMesh.gameObject.transform.localRotation = Quaternion.Euler(-90, 0, 0);
                proBuilderMesh.gameObject.transform.localScale = m.GetS();

                proBuilderMesh.name = visible;
                proBuilderMeshes.Add(proBuilderMesh);
                proBuilderMeshDic.Add(id, proBuilderMesh);
                MarkMeshAsStatic(proBuilderMesh.gameObject);
                if (ShouldYieldForFrameBudget())
                {
                    yield return null;
                    ResetFrameBudget();
                }
            }
            //偏移矩形台
            else if (OffsetRectangularTable != null) {
                var TL = OffsetRectangularTable.Attributes["TL"].Value;
                var TW = OffsetRectangularTable.Attributes["TW"].Value;
                var LL = OffsetRectangularTable.Attributes["LL"].Value;
                var LW = OffsetRectangularTable.Attributes["LW"].Value;
                var H = OffsetRectangularTable.Attributes["H"].Value;
                var XOFF = OffsetRectangularTable.Attributes["XOFF"].Value;
                var YOFF = OffsetRectangularTable.Attributes["YOFF"].Value;

            }
            else if (Boolean != null)
            {
                /*string Entity1 = Boolean.Attributes["Entity1"].Value;
                string Entity2 = Boolean.Attributes["Entity2"].Value;
                if (proBuilderMeshDic.ContainsKey(Entity1) && proBuilderMeshDic.ContainsKey(Entity2))
                {
                    string Type = Boolean.Attributes["Type"].Value;
                    var e1 = proBuilderMeshDic[Entity1];
                    var e2 = proBuilderMeshDic[Entity2];
                    var mesh = new Mesh();
                    MeshUtility.Compile(e1, mesh);
                    e1.GetComponent<MeshFilter>().mesh = mesh;
                    mesh = new Mesh();
                    MeshUtility.Compile(e2, mesh);
                    e2.GetComponent<MeshFilter>().mesh = mesh;

                    Mesh result = null;
                    if (Type.Equals("Difference"))
                    {
                        result = CSG.Subtract(e1.gameObject, e2.gameObject, true, true);
                    }
                    else if (Type.Equals("Union"))
                    {
                        result = CSG.Union(e1.gameObject, e2.gameObject, true, true);
                    }
                    if (result != null)
                    {
                        var p = ProBuilderMesh.Create();
                        p.gameObject.transform.SetParent(rootGameObject.transform, false);
                        p.GetComponent<MeshRenderer>().material = GetMaterialByColor(color);
                        var meshImporter = new MeshImporter(result, new Material[] { GetMaterialByColor(color) }, p);
                        meshImporter.Import();

                        p.name = visible;
                        proBuilderMeshes.Add(p);
                        proBuilderMeshDic.Add(id, p);
                    }
                }*/
            }
            else if (RotationalEllipsoid != null)
            {
                var LR = float.Parse(RotationalEllipsoid.Attributes["LR"].Value);
                var WR = float.Parse(RotationalEllipsoid.Attributes["WR"].Value);
                var H = float.Parse(RotationalEllipsoid.Attributes["H"].Value);
                var latSegments = 32;
                var lonSegments = 16;

                /*GameObject gameObject1 = new GameObject();
                MeshFilter meshFilter = gameObject1.AddComponent<MeshFilter>();
                MeshRenderer meshRenderer = gameObject1.AddComponent<MeshRenderer>();*/
                Mesh mesh = new Mesh();
                // 创建一个新的网格
                //meshFilter.mesh = mesh;

                // 生成顶点和三角形
                int numVertices = (latSegments + 1) * (lonSegments + 1);
                int numTriangles = latSegments * lonSegments * 6;

                Vector3[] vertices = new Vector3[numVertices];
                int[] triangles = new int[numTriangles];

                int index = 0;

                // 生成顶点
                for (int lat = 0; lat <= latSegments; lat++)
                {
                    float theta = lat * Mathf.PI / latSegments;
                    float sinTheta = Mathf.Sin(theta);
                    float cosTheta = Mathf.Cos(theta);

                    for (int lon = 0; lon <= lonSegments; lon++)
                    {
                        float phi = lon * 2 * Mathf.PI / lonSegments;
                        float sinPhi = Mathf.Sin(phi);
                        float cosPhi = Mathf.Cos(phi);

                        float x = LR * sinTheta * cosPhi;
                        float y = WR * sinTheta * sinPhi;
                        float z = H * cosTheta;

                        vertices[index++] = new Vector3(x, y, z);
                    }
                }

                // 生成三角形
                index = 0;
                for (int lat = 0; lat < latSegments; lat++)
                {
                    for (int lon = 0; lon < lonSegments; lon++)
                    {
                        int currentVert = lat * (lonSegments + 1) + lon;
                        int nextVert = currentVert + (lonSegments + 1);

                        triangles[index++] = currentVert;
                        triangles[index++] = nextVert + 1;
                        triangles[index++] = currentVert + 1;

                        triangles[index++] = currentVert;
                        triangles[index++] = nextVert;
                        triangles[index++] = nextVert + 1;
                    }
                }

                // 将顶点和三角形应用到网格
                mesh.vertices = vertices;
                mesh.triangles = triangles;

                // 计算法线和切线（如果需要）
                mesh.RecalculateNormals();
                mesh.RecalculateTangents();

                // 可选：设置材质，调整渲染设置等
                //meshFilter.mesh = mesh;

                /*                meshFilter.mesh = mesh;
                                gameObject1.name = "RotationalEllipsoid";
                                gameObject1.GetComponent<MeshFilter>().mesh = meshFilter.mesh;
                                gameObject1.gameObject.GetComponent<MeshRenderer>().material = GetMaterialByColor(color);
                                gameObject1.gameObject.transform.localScale = new Vector3(0.0025f, 0.001f, 0.002f);
                                gameObject1.gameObject.transform.localPosition = m.GetT();
                                gameObject1.gameObject.transform.localRotation = m.GetR();
                */
                ProBuilderMesh proBuilderMesh = ProBuilderMesh.Create();
                proBuilderMesh.GetComponent<MeshFilter>().mesh = mesh;
                proBuilderMesh.gameObject.GetComponent<MeshRenderer>().material = GetMaterialByColor(color);
                proBuilderMesh.gameObject.transform.localScale = new Vector3(0.0025f, 0.001f, 0.002f);
                proBuilderMesh.gameObject.transform.localPosition = m.GetT();
                proBuilderMesh.gameObject.transform.localRotation = m.GetR();
                proBuilderMesh.name = visible;



/*
                proBuilderMeshes.Add(proBuilderMesh);
                proBuilderMeshDic.Add(id, proBuilderMesh);
                MarkMeshAsStatic(proBuilderMesh.gameObject);
                if (ShouldYieldForFrameBudget())
                {
                    yield return null;
                    ResetFrameBudget();
                }*/

            }
            else if (RoundSteelTube != null)
            {

            } else if (EquilateralAngleSteel != null)
            {

            }else if (FlatSteel!=null)
            {

            }
            else
            {

            }




        }
        /*for (int i = 0; i < proBuilderMeshes.Count(); i++)
        {
            var p = proBuilderMeshes[i];
            if (p.name.Equals("False"))
            {
                proBuilderMeshes.RemoveAt(i);
                i--;
                Destroy(p.gameObject);
            }
        }*/
        if (proBuilderMeshes.Count > 1)
        {
            proBuilderMeshes[0].name = modelName;
            CombineMeshes.Combine(proBuilderMeshes, proBuilderMeshes[0]);
            for (int i = 1; i < proBuilderMeshes.Count; i++)
            {
                //proBuilderMeshes[i].transform.parent = proBuilderMeshes[0].transform;
                Destroy(proBuilderMeshes[i].gameObject);
            }
            action.Invoke(proBuilderMeshes[0]);
            yield return new WaitForEndOfFrame();
        } 
        else if (proBuilderMeshes.Count == 1)
        {
            proBuilderMeshes[0].name = modelName;
            action.Invoke(proBuilderMeshes[0]);
            yield return new WaitForEndOfFrame();
        }
        action.Invoke(null);
        yield return null;
    }

    internal void Clear()
    {
        if (rootGameObject != null)
        {
            Destroy(rootGameObject);
        }
    }

    IEnumerator LoadStl(string path, Matrix4x4 m4, Color color, UnityAction<GameObject> action)
    {
        path = path.Replace("\\", "/");
        Toast.Show("加载 " + string.Format("{0}...{1}", path.Substring(0, path.IndexOf("/")), path.Substring(path.LastIndexOf("/"))));
        var meshes = Importer.Import(path, CoordinateSpace.Left, UpAxis.Z).ToArray();
        Material mat = GetMaterialByColor(color);
        if (meshes.Length > 1)
        {
            var p = new GameObject();
            p.transform.SetParent(rootGameObject.transform, false);
            p.transform.localPosition = m4.GetT();
            p.transform.localRotation = m4.GetR();
            p.transform.localScale = m4.GetS() * ratio;
            foreach (var item in meshes)
            {
                var go = new GameObject();
                go.transform.SetParent(p.transform, false);
                go.AddComponent<MeshFilter>().mesh = item;
                go.AddComponent<MeshRenderer>().material = mat;
                if (generateMeshColliders)
                {
                    go.AddComponent<MeshCollider>().sharedMesh = item;
                }
                MarkMeshAsStatic(go);
                if (ShouldYieldForFrameBudget())
                {
                    yield return null;
                    ResetFrameBudget();
                }
            }
            action.Invoke(p);
        }
        else
        {
            var go = new GameObject();
            go.transform.SetParent(rootGameObject.transform, false);
            go.transform.localPosition = m4.GetT();
            go.transform.localRotation = m4.GetR();
            go.transform.localScale = m4.GetS() * ratio;
            go.AddComponent<MeshFilter>().mesh = meshes[0];
            go.AddComponent<MeshRenderer>().material = mat;
            if (generateMeshColliders)
            {
                go.AddComponent<MeshCollider>().sharedMesh = meshes[0];
            }
            MarkMeshAsStatic(go);
            action.Invoke(go);
        }
        
        yield return new WaitForEndOfFrame();
    }
    IEnumerator loadGim(string path)
    {
        //proBuilder = ProBuilderMesh.Create();
        List<string> list = loadFiles(path + "\\CBM");
        if (list.Count > 0)
        {
			Dictionary<string, string> rootList = getProperties(list[0]);
			Dictionary<string, string> devList = getProperties(path + "\\DEV\\" + rootList["OBJECTMODELPOINTER"]);
			rootGameObject.name = devList["SYMBOLNAME"];
			//������������
			Dictionary<string, string> dictionaryFam = getPropertiesFam(path + "\\DEV\\" + devList["BASEFAMILYPOINTER"]);
			//�������
			int partNum = int.Parse(devList["SUBDEVICES.NUM"]);
			for (int i = 0; i < partNum; i++)
			{
				//ÿ�������phm
				Dictionary<string, string> devDictionary = getProperties(path + "\\DEV\\" + devList["SUBDEVICE" + i]);

				items.Add(getPropertiesFam(path + "\\DEV\\" + devDictionary["BASEFAMILYPOINTER"]));

				int PhmNum = int.Parse(devDictionary["SOLIDMODELS.NUM"]);
				for (int j = 0; j < PhmNum; j++)
				{
					Dictionary<string, string> phmDictionary = getProperties(path + "\\PHM\\" + devDictionary["SOLIDMODEL" + j]);
					int ModNum = int.Parse(phmDictionary["SOLIDMODELS.NUM"]);
					for (int d = 0; d < ModNum; d++)
					{
						//string modpth = path + "MOD\\" + phmDictionary["SOLIDMODEL" + d];
						string[] devTra = devList["TRANSFORMMATRIX" + i].Split(",");
						var m = new Matrix4x4(new Vector4(float.Parse(devTra[0]), float.Parse(devTra[1]), float.Parse(devTra[2]), float.Parse(devTra[3])), new Vector4(float.Parse(devTra[4]), float.Parse(devTra[5]), float.Parse(devTra[6]), float.Parse(devTra[7])), new Vector4(float.Parse(devTra[8]), float.Parse(devTra[9]), float.Parse(devTra[10]), float.Parse(devTra[11])), new Vector4(float.Parse(devTra[12]), float.Parse(devTra[13]), float.Parse(devTra[14]), float.Parse(devTra[15])));

						yield return getModel3(path + "\\MOD\\" + phmDictionary["SOLIDMODEL" + d], m, phmDictionary["ENTITYNAME"], result =>
						{
							if (result != null)
							{
								var mesh = new Mesh();
								MeshUtility.Compile(result, mesh);
								result.gameObject.GetComponent<MeshFilter>().mesh = mesh;
								result.gameObject.transform.SetParent(rootGameObject.transform, false);
								ProBuilderMeshItem.Add(result.gameObject);
								Destroy(result);
							}
						});
					}
				}
			}
		}
    }
    public Dictionary<string, string> getPropertiesDev(string path)
    {
        string normalizedPath = Path.GetFullPath(path);
        Dictionary<string, string> cachedMap;
        if (devPropertiesCache.TryGetValue(normalizedPath, out cachedMap))
        {
            return cachedMap;
        }

        Dictionary<string, string> map = new Dictionary<string, string>();
        string[] Properties = File.ReadAllLines(path);
        for (int i = 0; i < Properties.Length; i++)
        {
            string[] item = Properties[i].Split("=");

            if (item[0].Equals("SUBDEVICES.NUM"))
            {

                int num = int.Parse(item[1]);
                map.Add("SUBDEVICES.NUM", num.ToString());
                for (int n = 0; n < num; n++)
                {
                    i += 1;
                    string[] items = Properties[i].Split("=");
                    map.Add(items[0], items[1]);
                    i += 1;
                    items = Properties[i].Split("=");
                    map.Add("SUBDEVICES." + items[0], items[1]);

                }
            }
            else
            if (item[0].Equals("SOLIDMODELS.NUM"))
            {
                int num = int.Parse(item[1]);
                map.Add("SOLIDMODELS.NUM", num.ToString());
                for (int n = 0; n < num; n++)
                {
                    i += 1;
                    string[] items = Properties[i].Split("=");
                    map.Add(items[0], items[1]);
                    i += 1;
                    items = Properties[i].Split("=");
                    map.Add("SOLIDMODEL." + items[0], items[1]);

                }
            }
            else
            if (item[0].Equals("TRANSFORMMATRIX" + i))
            {
                continue;
            }
            else { map.Add(item[0], item[1]); }

        }
        devPropertiesCache[normalizedPath] = map;
        return map;
    }
    public Dictionary<string, string> getProperties(string paths)
    {
        string normalizedPath = Path.GetFullPath(paths);
        Dictionary<string, string> cachedMap;
        if (propertiesCache.TryGetValue(normalizedPath, out cachedMap))
        {
            return cachedMap;
        }

        Dictionary<string, string> map = new Dictionary<string, string>();
        string[] Properties = File.ReadAllLines(paths);
        for (int i = 0; i < Properties.Length; i++)
        {
            string[] item = Properties[i].Split('=');
            if (item.Length >= 2)
            {
                map[item[0].Trim()] = item[1].Trim();
            }
        }
        propertiesCache[normalizedPath] = map;
        return map;
    }
    public Dictionary<string, string> getPropertiesFam(string path)
    {
        string normalizedPath = Path.GetFullPath(path);
        Dictionary<string, string> cachedMap;
        if (famPropertiesCache.TryGetValue(normalizedPath, out cachedMap))
        {
            return cachedMap;
        }

        Dictionary<string, string> map = new Dictionary<string, string>();
        string[] Properties = File.ReadAllLines(path);
        for (int i = 0; i < Properties.Length; i++)
        {
            string[] item = Properties[i].Split('=');
            if (item.Length >= 3)
            {
                map[item[1]] = item[2];
            }
        }
        famPropertiesCache[normalizedPath] = map;
        return map;
    }
    List<string> loadFiles(string filepath)
    {
        List<string> filesPath = new List<string>();

        if (Directory.Exists(filepath))
        {
            DirectoryInfo direction = new DirectoryInfo(filepath);
            FileInfo[] files = direction.GetFiles("*", SearchOption.AllDirectories);
            Console.Write(files.Length);
            for (int i = 0; i < files.Length; i++)
            {
                string FilePath = filepath + "/" + files[i].Name;
                filesPath.Add(FilePath);
            }
        }
        return filesPath;
    }
    void CreateIfc(string path)
    {
        Process process = new Process();
        string info = path;
        ProcessStartInfo startInfo = new ProcessStartInfo(_Ifc2XbimUrl, info);
        process.StartInfo = startInfo;
        //����DOS����  
        process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
        process.Start();
        process.WaitForExit();
        process.Close();
    }

    ProBuilderMesh createPorcelainBushing(float R, float R2, float H)
    {
        proBuilderMeshes3.Clear();
        float J = Mathf.Deg2Rad * (360 / smooth);
        var points1 = new List<Vector3>();
        for (int i = 0; i < smooth; i++)
        {
            Vector3 p = new Vector3();
            p.z = 0;
            p.x = R * Mathf.Sin(J * i);
            p.y = R * Mathf.Cos(J * i);
            points1.Add(p);
        }

        ProBuilderMesh proBuilderMesh = ProBuilderMesh.Create();
        proBuilderMesh.gameObject.transform.SetParent(rootGameObject.transform, false);
        proBuilderMesh.CreateShapeFromPolygon(points1, 0f, false);
        ProBuilderMesh proBuilderMesh23 = ProBuilderMesh.Create();
        proBuilderMesh23.gameObject.transform.SetParent(rootGameObject.transform, false);
        proBuilderMesh23.CreateShapeFromPolygon(points1, 0f, false);
        Vertex[] vertices = proBuilderMesh.GetVertices();
        Matrix4x4 matrix4x4 = Matrix4x4.Scale(new Vector3(R2 / R, R2 / R, 1));
        Matrix4x4 matrix4x41 = Matrix4x4.Translate(new Vector3(0,0,H));

        for (int i = 0; i < smooth; i++)
        {
            vertices[i].position = (matrix4x41.MultiplyPoint3x4(vertices[i].position));
        }
        proBuilderMesh.SetVertices(vertices);
        proBuilderMesh.ToMesh();
        proBuilderMesh.Refresh();

        Vertex[] verticesmin = proBuilderMesh23.GetVertices();
        for (int i = 0; i < verticesmin.Length; i++)
        {
            verticesmin[i].position = (matrix4x4.MultiplyPoint3x4(verticesmin[i].position));
        }
        proBuilderMesh23.SetVertices(verticesmin);
        proBuilderMesh23.ToMesh();
        proBuilderMesh23.Refresh();

        proBuilderMeshes3.Add(proBuilderMesh);
        proBuilderMeshes3.Add(proBuilderMesh23);
        CombineMeshes.Combine(proBuilderMeshes3, proBuilderMesh);
        for (int i = 0; i < smooth; i++)
        {
            Face face = proBuilderMesh.Bridge(proBuilderMesh.faces[1].edges[i], proBuilderMesh.faces[0].edges[i]);
            //face.Reverse();
        }
        proBuilderMesh.faces[1].Reverse();
        proBuilderMesh.ToMesh();
        proBuilderMesh.Refresh();
        Destroy(proBuilderMesh23.gameObject);
        return proBuilderMesh;

    }
    public void DecompressFileToDirectory(string inFileath, string outFilePath)
    {
        if (!Directory.Exists(outFilePath + "CBM"))
        {
            Process process = new Process();
            string info = " x " + inFileath + " -o" + outFilePath + " -r ";
            ProcessStartInfo startInfo = new ProcessStartInfo(_7zExeUrl, info);
            process.StartInfo = startInfo;
            //����DOS����  
            process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            process.Start();
            process.WaitForExit();
            process.Close();
        }

    }

    private void Start()
    {
        /*fileUrl = "C:\\Users\\杨星辰\\Desktop\\byq\\110kv";
        Load();*/
    }
    public void SelectFile()
    {
        //path = EditorUtility.OpenFolderPanel("Select Folder", "", "");
        if (!string.IsNullOrEmpty(path))
        {
            fileUrl = path;
            //Load();
        }
    }

    public IEnumerator LoadModel(string Loadpath)
    {
        phmRecursionStack.Clear();
        devRecursionStack.Clear();
        propertiesCache.Clear();
        devPropertiesCache.Clear();
        famPropertiesCache.Clear();
        ResetFrameBudget();
        fileUrl = Loadpath;
        yield return Load();
        GimUIController.treeItems = TreeGameObjects;


    }

    IEnumerator LoadDev2(GameObject parent, string fileName, Matrix4x4 mat, MyCustomData f4MyCustomData, int depth = 0)
    {
        if (depth > MaxRecursiveDepth)
        {
            UnityEngine.Debug.LogWarning($"Skip DEV due to too much recursion depth: {fileName}");
            yield break;
        }
        var normalizedDevFile = Path.GetFullPath(fileName).Replace("\\", "/");
        if (!devRecursionStack.Add(normalizedDevFile))
        {
            UnityEngine.Debug.LogWarning($"Detected cyclic DEV reference, skip file: {fileName}");
            yield break;
        }

        Dictionary<string, string> f5Dictionary = getPropertiesDev(fileName);
        Dictionary<string, string> f2PropertiesFam = getPropertiesFam(path + "\\DEV\\" + f5Dictionary["BASEFAMILYPOINTER"]);
        var dev = ProBuilderMesh.Create();
        dev.name = f5Dictionary["SYMBOLNAME"];
        MyCustomData myCustomData = new MyCustomData(dev.name, dev.gameObject);
        dev.AddComponent<GimLoadItem>().Items = f2PropertiesFam;

        dev.transform.localPosition = mat.GetT();
        dev.transform.localRotation = mat.GetR();
        dev.transform.localScale = mat.GetS();
        if (parent != null)
        {
            dev.transform.SetParent(parent.transform, false);
        }
        var phmNums = int.Parse(f5Dictionary["SOLIDMODELS.NUM"]);
        if (phmNums > 0)
        {
            var unsupportCombineObjs = new List<GameObject>();
            List<ProBuilderMesh> models = new List<ProBuilderMesh>() { dev };
            for (int i = 0; i < phmNums; i++)
            {
                string[] strings2 = f5Dictionary["SOLIDMODEL.TRANSFORMMATRIX" + i].Split(",");
                var m4 = new Matrix4x4(new Vector4(float.Parse(strings2[0]), float.Parse(strings2[1]), float.Parse(strings2[2]), float.Parse(strings2[3])), new Vector4(float.Parse(strings2[4]), float.Parse(strings2[5]), float.Parse(strings2[6]), float.Parse(strings2[7])), new Vector4(float.Parse(strings2[8]), float.Parse(strings2[9]), float.Parse(strings2[10]), float.Parse(strings2[11])), new Vector4(float.Parse(strings2[12]) * ratio, float.Parse(strings2[13]) * ratio, float.Parse(strings2[14]) * ratio, float.Parse(strings2[15])));
                string phmPath = f5Dictionary["SOLIDMODEL" + i];
                yield return LoadPhm2(parent, mat * m4, path + "//PHM//" + phmPath, unsupportCombineObjs, result =>
                {
                    if (result != null)
                    {
                        models.Add(result);
                    }
                });
                if (ShouldYieldForFrameBudget())
                {
                    yield return null;
                    ResetFrameBudget();
                }
            }
            //combine models
            if (models.Count > 1)
            {
                CombineMeshes.Combine(models, models[0]);
                for (int i = 1; i < models.Count; i++)
                {
                    Destroy(models[i].gameObject);
                }
            }
            Mesh mesh = new Mesh();
            MeshUtility.Compile(models[0], mesh);
            models[0].gameObject.GetComponent<MeshFilter>().mesh = mesh;
            MarkMeshAsStatic(models[0].gameObject);
            Destroy(models[0].gameObject.GetComponent<ProBuilderMesh>());

            foreach (var item in unsupportCombineObjs)
            {
                item.transform.SetParent(parent.transform, true);
            }
        }
        
        var subDevNums = int.Parse(f5Dictionary["SUBDEVICES.NUM"]);
        for (int i = 0; i < subDevNums; i++)
        {
            //自身矩阵
            string[] strings2 = f5Dictionary["SUBDEVICES.TRANSFORMMATRIX" + i].Split(",");
            var m4 = new Matrix4x4(new Vector4(float.Parse(strings2[0]), float.Parse(strings2[1]), float.Parse(strings2[2]), float.Parse(strings2[3])), new Vector4(float.Parse(strings2[4]), float.Parse(strings2[5]), float.Parse(strings2[6]), float.Parse(strings2[7])), new Vector4(float.Parse(strings2[8]), float.Parse(strings2[9]), float.Parse(strings2[10]), float.Parse(strings2[11])), new Vector4(float.Parse(strings2[12]) * ratio, float.Parse(strings2[13]) * ratio, float.Parse(strings2[14]) * ratio, float.Parse(strings2[15])));
            yield return LoadDev2(dev.gameObject, path + "//DEV//" + f5Dictionary["SUBDEVICE" + i], m4, myCustomData, depth + 1);
            if (ShouldYieldForFrameBudget())
            {
                yield return null;
                ResetFrameBudget();
            }
        }
        f4MyCustomData.childs.Add(myCustomData);
        devRecursionStack.Remove(normalizedDevFile);
    }

    private IEnumerator LoadPhm2(GameObject parent, Matrix4x4 mat, string phmPath, List<GameObject> unsupportCombineObjs, UnityAction<ProBuilderMesh> action, int depth = 0)
    {
        if (depth > MaxRecursiveDepth)
        {
            UnityEngine.Debug.LogWarning($"Skip PHM due to too much recursion depth: {phmPath}");
            yield break;
        }
        var normalizedPhmPath = Path.GetFullPath(phmPath).Replace("\\", "/");
        if (!phmRecursionStack.Add(normalizedPhmPath))
        {
            UnityEngine.Debug.LogWarning($"Detected cyclic PHM reference, skip file: {phmPath}");
            yield break;
        }

        Dictionary<string, string> f5Dictionary = getProperties(phmPath);
        List<ProBuilderMesh> models = new List<ProBuilderMesh>() { };
        var modNums = int.Parse(f5Dictionary["SOLIDMODELS.NUM"]);
        for (int i = 0; i < modNums; i++)
        {
            string type = Path.GetExtension(f5Dictionary["SOLIDMODEL" + i]).TrimStart('.').ToLowerInvariant();
            string[] strings2 = f5Dictionary["TRANSFORMMATRIX" + i].Split(",");
            var m4 = new Matrix4x4(new Vector4(float.Parse(strings2[0]), float.Parse(strings2[1]), float.Parse(strings2[2]), float.Parse(strings2[3])), new Vector4(float.Parse(strings2[4]), float.Parse(strings2[5]), float.Parse(strings2[6]), float.Parse(strings2[7])), new Vector4(float.Parse(strings2[8]), float.Parse(strings2[9]), float.Parse(strings2[10]), float.Parse(strings2[11])), new Vector4(float.Parse(strings2[12]) * ratio, float.Parse(strings2[13]) * ratio, float.Parse(strings2[14]) * ratio, float.Parse(strings2[15])));
            if (type.Equals("mod"))
            {
                yield return getModel3(path + "//MOD//" + f5Dictionary["SOLIDMODEL" + i], mat * m4, "mod", result =>
                {
                    if (result != null)
                    {
                        models.Add(result);
                    }
                });
            }
            if (type.Equals("phm"))
            {
                yield return LoadPhm2(parent, mat * m4, path + "//PHM//" + f5Dictionary["SOLIDMODEL" + i], unsupportCombineObjs, result =>
                {
                    if (result != null)
                    {
                        models.Add(result);
                    }
                }, depth + 1);
            }
            if (type.Equals("stl"))
            {
                string[] strings = f5Dictionary["COLOR" + i].Split(",");
                Color color = new Color(float.Parse(strings[0]) / 255, float.Parse(strings[1]) / 255, float.Parse(strings[2]) / 255, 0);
                yield return LoadStl(path + "\\MOD\\" + f5Dictionary["SOLIDMODEL" + i], mat * m4, color, result =>
                {
                    unsupportCombineObjs.Add(result);
                });
            }
            if (ShouldYieldForFrameBudget())
            {
                yield return null;
                ResetFrameBudget();
            }
        }
        Debugger.Log(1, "phm:{}", phmPath);
        if (models.Count > 0)
        {
            if (models.Count > 1)
            {
                CombineMeshes.Combine(models, models[0]);
                for (int i = 1; i < models.Count; i++)
                {
                    Destroy(models[i].gameObject);
                }
            }
            action.Invoke(models[0]);
        }
        phmRecursionStack.Remove(normalizedPhmPath);
    }

    private IEnumerator LoadProject(string path)
    {
        List<GameObject> proBuildersItem = new List<GameObject>();
        Dictionary<string, string> projectDictionary = getProperties(path + "\\CBM\\project.cbm");
        Dictionary<string, string> f1Dictionary = getProperties(path + "\\CBM\\" + projectDictionary["SUBSYSTEM"]);
        Dictionary<string, string> dictionary = getPropertiesFam(path + "\\CBM\\" + f1Dictionary["BASEFAMILY"]);

        
        //ProBuilderMesh rootGameObject = ProBuilderMesh.Create();
        rootGameObject.name = "全站级";

        rootGameObject.AddComponent<GimLoadItem>().Items = dictionary;
        var projectName = "Unknown";
        if (dictionary.ContainsKey("工程名称"))
        {
            projectName = dictionary["工程名称"];

		}
        MyCustomData myCustomData = new MyCustomData(projectName, rootGameObject);
        if (f1Dictionary.ContainsKey("IFC.NUM\""))
        {
			int ifcnum = int.Parse(f1Dictionary["IFC.NUM"]);
			for (int x = 0; x < ifcnum; x++)
			{

				CreateIfc(path + "\\CBM\\" + f1Dictionary["IFC" + x]);
				string[] strings = (path + "\\CBM\\" + f1Dictionary["IFC" + x]).Split(".");
				GameObject gameObject1 = new Parser().Parse(strings[0] + ".Xbim");
				gameObject1.transform.localScale = Vector3.one * ratio;
				gameObject1.name = f1Dictionary["IFC" + x];
				gameObject1.transform.SetParent(rootGameObject.transform, false);
				MyCustomData ifcCustomData = new MyCustomData(gameObject1.name, gameObject1);
				myCustomData.childs.Add(ifcCustomData);
			}
		}
        
        if (f1Dictionary.ContainsKey("SUBSYSTEMS.NUM"))
        {
			int f1num = int.Parse(f1Dictionary["SUBSYSTEMS.NUM"]);
			for (int i = 0; i < f1num; i++)
			{
				Dictionary<string, string> f2Dictionary = getProperties(path + "\\CBM\\" + f1Dictionary["SUBSYSTEM" + i]);

				Dictionary<string, string> f2PropertiesFam = getPropertiesFam(path + "\\CBM\\" + f2Dictionary["BASEFAMILY"]);
				int f2num = int.Parse(f2Dictionary["SUBSYSTEMS.NUM"]);
				GameObject gameObject2 = new GameObject();
				gameObject2.AddComponent<GimLoadItem>().Items = f2PropertiesFam;
				if (!f2Dictionary["SYSCLASSIFYNAME"].Equals("Y"))
				{
					gameObject2.name = f2Dictionary["SYSCLASSIFYNAME"] + "号变压器单元";
				}
				else
				{
					gameObject2.name = "全站公用";
				}
				MyCustomData f2CustomData = new MyCustomData(gameObject2.name, gameObject2);
				myCustomData.childs.Add(f2CustomData);
				gameObject2.transform.SetParent(rootGameObject.transform, false);
				for (int j = 0; j < f2num; j++)
				{
					string temp = path + "\\CBM\\" + f2Dictionary["SUBSYSTEM" + j];
					Dictionary<string, string> f3Dictionary = getProperties(path + "\\CBM\\" + f2Dictionary["SUBSYSTEM" + j]);

					int f3num = int.Parse(f3Dictionary["SUBSYSTEMS.NUM"]);
					Dictionary<string, string> BASEFAMILY1 = getPropertiesFam(path + "\\CBM\\" + f3Dictionary["BASEFAMILY1"]);
					Dictionary<string, string> BASEFAMILY2 = getPropertiesFam(path + "\\CBM\\" + f3Dictionary["BASEFAMILY2"]);
					Dictionary<string, string> BASEFAMILY3 = getPropertiesFam(path + "\\CBM\\" + f3Dictionary["BASEFAMILY3"]);
					string v;
					string[] strings = { };
					try
					{
						v = BASEFAMILY3["系统分类"];
						strings = v.Split("/");
					}
					catch (Exception e)
					{
						string[] list = { "1", "2", "3" };
						strings = list;
					}



					GameObject BASEFAMILY1gameObject1 = new GameObject();

					BASEFAMILY1gameObject1.AddComponent<GimLoadItem>().Items = BASEFAMILY1;
					BASEFAMILY1gameObject1.name = strings[0];
					BASEFAMILY1gameObject1.transform.parent = gameObject2.transform;
					MyCustomData cbm2MyCustomData = new MyCustomData(BASEFAMILY1gameObject1.name, BASEFAMILY1gameObject1);
					f2CustomData.childs.Add(cbm2MyCustomData);

					GameObject BASEFAMILY2gameObject2 = new GameObject();

					BASEFAMILY2gameObject2.AddComponent<GimLoadItem>().Items = BASEFAMILY2;
					BASEFAMILY2gameObject2.name = strings[1];
					BASEFAMILY2gameObject2.transform.parent = BASEFAMILY1gameObject1.transform;
					MyCustomData cbm3MyCustomData = new MyCustomData(BASEFAMILY2gameObject2.name, BASEFAMILY2gameObject2);

					cbm2MyCustomData.childs.Add(cbm3MyCustomData);
					GameObject BASEFAMILY3gameObject3 = new GameObject();

					BASEFAMILY3gameObject3.AddComponent<GimLoadItem>().Items = BASEFAMILY3;
					BASEFAMILY3gameObject3.name = strings[2];
					BASEFAMILY3gameObject3.transform.parent = BASEFAMILY2gameObject2.transform;
					MyCustomData cbm4MyCustomData = new MyCustomData(BASEFAMILY3gameObject3.name, BASEFAMILY3gameObject3);
					cbm3MyCustomData.childs.Add(cbm4MyCustomData);
					for (int k = 0; k < f3num; k++)
					{
						//主变压器
						Dictionary<string, string> f4Dictionary = getProperties(path + "\\CBM\\" + f3Dictionary["SUBSYSTEM" + k]);

						Dictionary<string, string> f5Dictionary = getPropertiesDev(path + "\\DEV\\" + f4Dictionary["OBJECTMODELPOINTER"]);
						/*if (!f4Dictionary["OBJECTMODELPOINTER"].Equals("e9f96378-2486-4e7b-89d3-0a26406d5043.dev")) {
							continue;
						}*/
						/*if (f4Dictionary["OBJECTMODELPOINTER"].Equals("1e8e26e7-86a8-4dea-a129-c39a79b9f758.dev") || f4Dictionary["OBJECTMODELPOINTER"].Equals("b415214a-a1d8-492d-bb45-cdf05993427a.dev"))
						{*/

						Dictionary<string, string> f6Dictionary = getPropertiesFam(path + "\\DEV\\" + f5Dictionary["BASEFAMILYPOINTER"]);
						string[] f1devm4 = f4Dictionary["TRANSFORMMATRIX"].Split(",");
						var m4 = new Matrix4x4(new Vector4(float.Parse(f1devm4[0]), float.Parse(f1devm4[1]), float.Parse(f1devm4[2]), float.Parse(f1devm4[3])), new Vector4(float.Parse(f1devm4[4]), float.Parse(f1devm4[5]), float.Parse(f1devm4[6]), float.Parse(f1devm4[7])), new Vector4(float.Parse(f1devm4[8]), float.Parse(f1devm4[9]), float.Parse(f1devm4[10]), float.Parse(f1devm4[11])), new Vector4(float.Parse(f1devm4[12]) * ratio, float.Parse(f1devm4[13]) * ratio, float.Parse(f1devm4[14]) * ratio, float.Parse(f1devm4[15])));

						GameObject f3gameObject1 = new GameObject();
						f3gameObject1.name = f4Dictionary["SYSCLASSIFYNAME"] + "";
						f3gameObject1.transform.localPosition = m4.GetT();
						f3gameObject1.transform.localRotation = m4.GetR();
						f3gameObject1.transform.localScale = m4.GetS();
						f3gameObject1.transform.SetParent(BASEFAMILY3gameObject3.transform, false);


						MyCustomData f3MyCustomData = new MyCustomData(f3gameObject1.name, f3gameObject1);
						cbm4MyCustomData.childs.Add(f3MyCustomData);


						//ProBuilderMesh f3gameObject2 = ProBuilderMesh.Create();

						//f3gameObject2.name = f5Dictionary["SYMBOLNAME"];

						var gameObject1 = ProBuilderMesh.Create();
						gameObject1.name = f5Dictionary["SYMBOLNAME"];
						gameObject1.transform.SetParent(f3gameObject1.transform, false);
						gameObject1.AddComponent<GimLoadItem>().Items = f6Dictionary;
						var unsupportCombineObjs = new List<GameObject>();
						List<ProBuilderMesh> probuilderMeshList = new List<ProBuilderMesh>() { gameObject1 };
						int subPhmNum = int.Parse(f5Dictionary["SOLIDMODELS.NUM"]);
						if (subPhmNum > 0)
						{
							for (int h = 0; h < subPhmNum; h++)
							{
								string[] strings2 = f5Dictionary["SOLIDMODEL.TRANSFORMMATRIX" + h].Split(",");
								var f2phm = new Matrix4x4(new Vector4(float.Parse(strings2[0]), float.Parse(strings2[1]), float.Parse(strings2[2]), float.Parse(strings2[3])), new Vector4(float.Parse(strings2[4]), float.Parse(strings2[5]), float.Parse(strings2[6]), float.Parse(strings2[7])), new Vector4(float.Parse(strings2[8]), float.Parse(strings2[9]), float.Parse(strings2[10]), float.Parse(strings2[11])), new Vector4(float.Parse(strings2[12]) * ratio, float.Parse(strings2[13]) * ratio, float.Parse(strings2[14]) * ratio, float.Parse(strings2[15])));
								yield return LoadPhm2(f3gameObject1, m4 * f2phm, path + "//PHM//" + f5Dictionary["SOLIDMODEL" + h], unsupportCombineObjs, result =>
								{
									if (result != null)
									{
										probuilderMeshList.Add(result);
									}
								});
							}
						}

						if (probuilderMeshList.Count > 1)
						{
							CombineMeshes.Combine(probuilderMeshList, probuilderMeshList[0]);
							for (int P = 1; P < probuilderMeshList.Count; P++)
							{
								Destroy(probuilderMeshList[P].gameObject);
							}
						}
						Mesh mesh = new Mesh();
						MeshUtility.Compile(probuilderMeshList[0], mesh);
						probuilderMeshList[0].GetComponent<MeshFilter>().mesh = mesh;
						if (generateMeshColliders)
						{
							probuilderMeshList[0].gameObject.AddComponent<MeshCollider>().sharedMesh = mesh;
						}
						MarkMeshAsStatic(probuilderMeshList[0].gameObject);

						foreach (var item in unsupportCombineObjs)
						{
							item.transform.SetParent(f3gameObject1.transform, true);
						}

						int subDevNum = int.Parse(f5Dictionary["SUBDEVICES.NUM"]);
						MyCustomData f4MyCustomData = new MyCustomData(gameObject1.name, gameObject1.gameObject);
						f3MyCustomData.childs.Add(f4MyCustomData);
						for (int x = 0; x < subDevNum; x++)
						{
							string[] strings2 = f5Dictionary["SUBDEVICES.TRANSFORMMATRIX" + x].Split(",");
							var f2dev = new Matrix4x4(new Vector4(float.Parse(strings2[0]), float.Parse(strings2[1]), float.Parse(strings2[2]), float.Parse(strings2[3])), new Vector4(float.Parse(strings2[4]), float.Parse(strings2[5]), float.Parse(strings2[6]), float.Parse(strings2[7])), new Vector4(float.Parse(strings2[8]), float.Parse(strings2[9]), float.Parse(strings2[10]), float.Parse(strings2[11])), new Vector4(float.Parse(strings2[12]) * ratio, float.Parse(strings2[13]) * ratio, float.Parse(strings2[14]) * ratio, float.Parse(strings2[15])));
							yield return LoadDev2(gameObject1.gameObject, path + "//DEV//" + f5Dictionary["SUBDEVICE" + x], m4 * f2dev, f4MyCustomData);
						}
						Destroy(probuilderMeshList[0].GetComponent<ProBuilderMesh>());
						// }
					}
				}
			}
		}
        
        //rootGameObject.transform.rotation = Quaternion.Euler(-90, 0, 0);
        //rootGameObject.transform.localScale = new Vector3(0.001f, 0.001f, 0.001f);
        //rootGameObject.transform.localPosition = new Vector3(246, 93.3f, -130);
        //proBuildersItem.Add(rootGameObject);
        //return proBuildersItem;
        TreeGameObjects.Add(myCustomData);

    }

    public static bool IsPrefab(Transform This)
    {
        if (Application.isEditor && !Application.isPlaying)
        {
            throw new InvalidOperationException("Does not work in edit mode");
        }
        return This.gameObject.scene.buildIndex < 0;
    }
    public IEnumerator Load()
    {
        ResetFrameBudget();
        var filePathWithoutExtension = Path.Combine(Path.GetDirectoryName(fileUrl) ?? string.Empty, Path.GetFileNameWithoutExtension(fileUrl));
        string filePath = filePathWithoutExtension + "\\";
        path = filePath;
        //不存在文件夹
        if (!Directory.Exists(filePathWithoutExtension))
        {//创建
            Directory.CreateDirectory(filePathWithoutExtension);
            string fileName = filePathWithoutExtension + "\\" + System.IO.Path.GetFileNameWithoutExtension(fileUrl) + ".zip";
            File.Copy(fileUrl, fileName, true);
            DecompressFileToDirectory(fileName, filePathWithoutExtension + "\\");
        }
        //判断是否是子模型
        rootGameObject = new GameObject();
        rootGameObject.transform.rotation = Quaternion.Euler(-90, 0, 0);
        if (File.Exists(filePath + "\\CBM\\project.cbm"))
        {
            yield return LoadProject(filePath);
        }
        else
        {
            yield return loadGim(filePath);
        }
    }
}
