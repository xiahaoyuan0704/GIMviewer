using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Test2 : MonoBehaviour
{

    MeshFilter meshFilter;

    //存放定点的数据
    List<Vector3> verts;
    //定点的序号
    List<int> indices;

    void Start()
    {
        verts = new List<Vector3>();
        indices = new List<int>();
        meshFilter = GetComponent<MeshFilter>();
        Generate();
    }
    void Generate()
    {
        ClearMeshData();
        AddMeshData();

        Mesh mesh = new Mesh();
        mesh.vertices = verts.ToArray();
        mesh.triangles = indices.ToArray();
        //计算法线
        mesh.RecalculateNormals();
        //计算物体的边界
        mesh.RecalculateBounds();
        meshFilter.mesh = mesh;

    }

    void ClearMeshData()
    {
        verts.Clear();
        indices.Clear();
    }

    void AddMeshData()
    {
        verts.Add(new Vector3(0, 0, 0));
        verts.Add(new Vector3(0, 1, 0));
        verts.Add(new Vector3(1, 1, 0));
        verts.Add(new Vector3(1, 0, 0));

        verts.Add(new Vector3(1, 0, 0));
        verts.Add(new Vector3(1, 1, 0));
        verts.Add(new Vector3(1, 1, 1));
        verts.Add(new Vector3(1, 0, 1));


        verts.Add(new Vector3(0, 1, 0));
        verts.Add(new Vector3(0, 1, 1));
        verts.Add(new Vector3(1, 1, 1));
        verts.Add(new Vector3(1, 1, 0));


        verts.Add(new Vector3(0, 0, 0));
        verts.Add(new Vector3(1, 0, 0));
        verts.Add(new Vector3(1, 0, 1));
        verts.Add(new Vector3(0, 0, 1));


        verts.Add(new Vector3(0, 0, 1));
        verts.Add(new Vector3(1, 0, 1));
        verts.Add(new Vector3(1, 1, 1));
        verts.Add(new Vector3(0, 1, 1));


        verts.Add(new Vector3(0, 0, 0));
        verts.Add(new Vector3(0, 0, 1));
        verts.Add(new Vector3(0, 1, 1));
        verts.Add(new Vector3(0, 1, 0));


        for (int i = 0; i <= 20; i += 4)
        {
            indices.Add(0 + i);
            indices.Add(1 + i);
            indices.Add(2 + i);
            indices.Add(0 + i);
            indices.Add(2 + i);
            indices.Add(3 + i);

        }
    }
}