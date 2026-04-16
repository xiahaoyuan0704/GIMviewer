using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;

using UnityEngine.ProBuilder;
using UnityEngine.ProBuilder.MeshOperations;

public class ParseTowerDemo : MonoBehaviour
{ struct Node
    {
        public string no;
        public Vector3 pos;
    }

    struct DebugData
    {
        public Vector3[] points;
        public Vector3 xDir;
        public Vector3 yDir;
        public Vector3 axis;
        public Vector3 dir;
        public Vector3 start;
        public Vector3 end;
        public Vertex[] vertices;
    }

    public float ratio = 0.001f;
    private Dictionary<Color, Material> matDic = new Dictionary<Color, Material>();
    private Dictionary<string, Node> nodes = new Dictionary<string, Node>();
    private List<string> listSteelAngles = new List<string>();
    private List<string> listSteelTube = new List<string>();
    private Color color = Color.gray;

    private Vector3[] points = new Vector3[6];
    private Vector3 p3_;
    private Vector3 p5_;

    private List<DebugData> debugDatas = new List<DebugData>();

    private GameObject root;

    void Start()
    {
		//var fileName = "1100锚塔.mod";
		var fileName = "锚塔.mod";
		var path = Path.Combine(Application.streamingAssetsPath, "TestData", fileName);
        StreamReader sr = new StreamReader(path);
        string line;
        while ((line = sr.ReadLine()) != null)
        {
            line = line.Trim();
            if (line.StartsWith("P"))
            {
                var segs = line.Split(",");
                nodes[segs[1]] = new Node() { no = segs[1], pos = new Vector3((float.Parse(segs[2])) * ratio, (float.Parse(segs[3])) * ratio, (float.Parse(segs[4])) * ratio) };
            }
            if (line.StartsWith("R"))
            {
                var segs = line.Split(",");
                if (segs.Length == 11)
                {
                    listSteelAngles.Add(line);
                }
                if (segs.Length == 5)
                {
                    listSteelTube.Add(line);
                }
                //listR.Add(segs[1] + "," + segs[2]);
            }
        }
        root = new GameObject(fileName);
        for (int i = 0; i < listSteelTube.Count; i++)
        {
            string[] item = listSteelTube[i].Split(",");
            Vector3 node1 = nodes[item[1]].pos;
            Vector3 node2 = nodes[item[2]].pos;
            steelTube(node1, node2);
        }


        for (int i = 0; i < listSteelAngles.Count; i++)
        {
            string[] item = listSteelAngles[i].Split(",");
            Vector3 node1 = nodes[item[1]].pos;
            Vector3 node2 = nodes[item[2]].pos;
            Vector3 xDirection = new Vector3(float.Parse(item[5]), float.Parse(item[6]), float.Parse(item[7])).normalized;
            Vector3 yDirection = new Vector3(float.Parse(item[8]), float.Parse(item[9]), float.Parse(item[10])).normalized;
            MatchCollection matches = Regex.Matches(item[3], @"\d+");
            float angleSteelWidth = float.Parse(matches[0].Value);
            float angleSteelThickness = float.Parse(matches[1].Value);
            steelAngles(node1, node2, xDirection, yDirection, angleSteelWidth * ratio, angleSteelThickness * ratio, item[1]+","+ item[2]);
        }
        root.transform.rotation = Quaternion.AngleAxis(-90, Vector3.right);
    }

    private void OnDrawGizmos()
    {
        //var mat = GetMaterialByColor(color);
        /*   foreach (var p in vertices)
           {
               Gizmos.DrawSphere(p, 0.001f);
           }*/
        /*for (int i = 0; i < listSteelAngles.Count; i++)
        {
            string[] item =  listSteelAngles[i].Split(",");
            Vector3 node1 = nodes[item[1]].pos;
            Vector3 node2 = nodes[item[2]].pos;
            Vector3 xDirection = new Vector3(float.Parse(item[5]), float.Parse(item[6]), float.Parse(item[7]));
            Vector3 yDirection = new Vector3(float.Parse(item[8]), float.Parse(item[9]), float.Parse(item[10]));
            MatchCollection matches = Regex.Matches(item[3], @"\d+");
            float angleSteelWidth = float.Parse(matches[0].Value);
            float angleSteelThickness = float.Parse(matches[1].Value);
            ProBuilderMesh proBuilderMesh = steelAngles(node1,node2, xDirection, yDirection, angleSteelWidth, angleSteelThickness);
            proBuilderMesh.name = "1";
        }*/
        foreach (var item in debugDatas)
        {
            Gizmos.color = Color.blue;
            /*foreach (var v in item.vertices)
            {
                Gizmos.DrawRay(v.position, v.normal);
            }
            Gizmos.color = Color.green;
            Gizmos.DrawRay(Vector3.zero, item.axis);*/
            Gizmos.DrawRay(item.start, item.xDir);
        }
    }
    // Update is called once per frame
    void Update()
    {

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
    private ProBuilderMesh steelAngles(Vector3 node1,Vector3 node2,Vector3 xDirection, Vector3 yDirection, float angleSteelWidth, float angleSteelThickness,string name) {
        //�Ǹֵ����߷�����ߵ�������췽��
        var axis = (node2 - node1).normalized;
        //x֫��y֫�ļн�
        float angle = Vector3.Angle(xDirection, yDirection);

        var xDir_ = Vector3.right;
        var yDir_ = Quaternion.AngleAxis(angle, Vector3.forward) * xDir_;
        //夹角一半处的方向向量
        var dir_ = Quaternion.AngleAxis(angle * 0.5f, Vector3.forward) * xDir_;

        Vector3 p1 = new Vector3(0, 0, 0);
        Vector3 p2 = p1 + xDir_ * angleSteelWidth;
        Vector3 p6 = p1 + yDir_ * angleSteelWidth;

        float a = angleSteelThickness / Mathf.Tan(angle * Mathf.Deg2Rad * 0.5f);
        float c = angleSteelThickness / Mathf.Sin(angle * Mathf.Deg2Rad * 0.5f);

        p3_ = p1 + xDir_ * (angleSteelWidth - a);
        p5_ = p1 + yDir_ * (angleSteelWidth - a);

        var trans = Matrix4x4.Translate(dir_ * c);

        Vector3 p3 = trans.MultiplyPoint(p3_);
        Vector3 p4 = trans.MultiplyPoint(p1);
        Vector3 p5 = trans.MultiplyPoint(p5_);

        points = new Vector3[6]
        {
            p1, p2, p3, p4, p5, p6
        };

        var angleSteel = ProBuilderMesh.Create();
        angleSteel.CreateShapeFromPolygon(points, (node1 - node2).magnitude, false);
        angleSteel.ToMesh();

        angleSteel.GetComponent<MeshRenderer>().material = GetMaterialByColor(color);
        angleSteel.transform.localPosition = node1;
        angleSteel.transform.localRotation = Quaternion.LookRotation(axis);
        angle = Vector3.SignedAngle(angleSteel.transform.right, xDirection, angleSteel.transform.forward);
        angleSteel.transform.Rotate(new Vector3(0, 0, angle), Space.Self);
        angleSteel.transform.SetParent(root.transform);
        angleSteel.name = name;

        /*debugDatas.Add(new DebugData()
        {
            points = points,
            xDir = xDirection,
            yDir = yDirection,
            axis = Vector3.forward,
            dir = dir_,
            start = node1,
            end = node2,
            vertices = angleSteel.GetVertices()
        });*/

        return angleSteel;
    }
    private ProBuilderMesh steelTube(Vector3 vector3, Vector3 vector31)
    {
        //var m = new Matrix4x4(new Vector4(1, 0, 0, 0), new Vector4(0, 1, 0, 0), new Vector4(0, 0, 1, 0), new Vector4(0, 0, 0, 1));
        var length = (vector3 - vector31).magnitude;
        var dir = (vector31 - vector3).normalized;
        var center = vector3 + dir * (length / 2);
        Quaternion q = Quaternion.LookRotation(dir) * Quaternion.Euler(90, 0, 0);
        //m = m * Matrix4x4.Translate(center);
        //m = m * Matrix4x4.Rotate(q);

        ProBuilderMesh proBuilderMesh = ShapeGenerator.GenerateCylinder(PivotLocation.Center, 10, 0.05f, length, 1);
        //proBuilderMesh.gameObject.transform.SetParent(rootGameObject.transform, false);
        /*var vertices = proBuilderMesh.GetVertices();
        foreach (var vertex in vertices)
        {
            vertex.position = Quaternion.Euler(90, 0, 0) * vertex.position;
            vertex.normal = Quaternion.Euler(90, 0, 0) * vertex.normal;
        }
        proBuilderMesh.SetVertices(vertices);*/
        proBuilderMesh.ToMesh();
        proBuilderMesh.Refresh();
        
        proBuilderMesh.transform.localPosition = center;
        proBuilderMesh.transform.localRotation = q;
        proBuilderMesh.transform.localScale = Vector3.one;
        
        proBuilderMesh.name = "steelTube";
        Renderer rend = proBuilderMesh.gameObject.GetComponent<Renderer>();
        rend.material = GetMaterialByColor(color);
        proBuilderMesh.transform.SetParent(root.transform);
        //listR[i].ToString();
        return proBuilderMesh;
    }
}