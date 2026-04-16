using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Xbim.Common;
using Xbim.Common.Geometry;
using Xbim.Ifc4.Interfaces;
using Xbim.ModelGeometry.Scene;
using Xbim.Presentation;
using Xbim.Common.XbimExtensions;
using System.Linq;
using System.IO;
using System;
using System.Globalization;

public static class MeshGeometry3DExtensions
{
    public static void AddElements(this Mesh m, EntitySelection selection, XbimMatrix3D wcsTransform)
    {
        foreach (var item in selection)
        {
            m.AddElements(item, wcsTransform);
        }
    }

    public static void AddElements(this Mesh m, IPersistEntity item, XbimMatrix3D wcsTransform)
    {
        if (item.Model == null || !(item is IIfcProduct)) return;

        var context = new Xbim3DModelContext(item.Model);

        var productShape = context.ShapeInstancesOf((IIfcProduct)item)
            .Where(s => s.RepresentationType != XbimGeometryRepresentationType.OpeningsAndAdditionsExcluded)
            .ToList();
        if (!productShape.Any() && item is IIfcFeatureElement)
        {
            productShape = context.ShapeInstancesOf((IIfcProduct)item)
                .Where(
                    s => s.RepresentationType == XbimGeometryRepresentationType.OpeningsAndAdditionsExcluded)
                .ToList();
        }

        if (!productShape.Any())
            return;
        foreach (var shapeInstance in productShape)
        {
            IXbimShapeGeometryData shapeGeom =
                context.ShapeGeometry(shapeInstance.ShapeGeometryLabel);
            switch ((XbimGeometryType)shapeGeom.Format)
            {
                case XbimGeometryType.PolyhedronBinary:
                    m.Read(shapeGeom.ShapeData,
                        XbimMatrix3D.Multiply(shapeInstance.Transformation, wcsTransform));
                    break;
                case XbimGeometryType.Polyhedron:
                    m.Read(((XbimShapeGeometry)shapeGeom).ShapeData,
                        XbimMatrix3D.Multiply(shapeInstance.Transformation, wcsTransform));
                    break;
            }
        }
    }

    public static void Add(this Mesh m3D, XbimMeshGeometry3D addedGeometry3D)
    {
        var positions = new List<Vector3>(m3D.vertexCount);
        m3D.GetVertices(positions);
        foreach (var item in addedGeometry3D.Positions)
        {
            positions.Add(new Vector3((float)item.X, (float)item.Y, (float)item.Z));
        }
        m3D.SetVertices(positions);

        var normals = new List<Vector3>(m3D.vertexCount);
        m3D.GetNormals(normals);
        foreach (var item in addedGeometry3D.Normals)
        {
            normals.Add(new Vector3((float)item.X, (float)item.Y, (float)item.Z));
        }
        m3D.SetNormals(normals);

        m3D.SetTriangles(addedGeometry3D.TriangleIndices, m3D.subMeshCount);
    }

    /// <summary>
    /// Reads a triangulated model from an array of bytes and adds the mesh to the current state
    ///  </summary>
    /// <param name="m3D"></param>
    /// <param name="mesh">byte array of XbimGeometryType.PolyhedronBinary  Data</param>
    /// <param name="transform">Transforms the mesh to the new position if not null</param>
    public static void Read(this Mesh m3D, byte[] mesh, XbimMatrix3D? tr = null)
    {
        int indexBase = m3D.vertexCount;
        //var qrd = new RotateTransform3D();
        //Matrix3D? matrix3D = null;
        Matrix4x4 matrix = Matrix4x4.identity;
        if (tr.HasValue)
        {
            /*var xq = transform.Value.GetRotationQuaternion();
            var quaternion = new Quaternion(xq.X, xq.Y, xq.Z, xq.W);
            if (!quaternion.IsIdentity)
                qrd.Rotation = new QuaternionRotation3D(quaternion);
            else
                qrd = null;
            matrix3D = transform.Value.ToMatrix3D();*/
            matrix = new Matrix4x4(
                    new Vector4((float)tr.Value.M11, (float)tr.Value.M12, (float)tr.Value.M13, (float)tr.Value.M14),
                    new Vector4((float)tr.Value.M21, (float)tr.Value.M22, (float)tr.Value.M23, (float)tr.Value.M24),
                    new Vector4((float)tr.Value.M31, (float)tr.Value.M32, (float)tr.Value.M33, (float)tr.Value.M34),
                    new Vector4((float)tr.Value.OffsetX, (float)tr.Value.OffsetY, (float)tr.Value.OffsetZ, (float)tr.Value.M44));
        }
        using (var ms = new MemoryStream(mesh))
        {
            using (var br = new BinaryReader(ms))
            {
                var version = br.ReadByte(); //stream format version
                var numVertices = br.ReadInt32();
                var numTriangles = br.ReadInt32();

                var uniqueVertices = new List<Vector3>(numVertices);
                var vertices = new List<Vector3>(numVertices * 4); //approx the size
                var triangleIndices = new List<int>(numTriangles * 3);
                var normals = new List<Vector3>(numVertices * 4);
                for (var i = 0; i < numVertices; i++)
                {
                    float x = br.ReadSingle();
                    float y = br.ReadSingle();
                    float z = br.ReadSingle();
                    var p = new Vector3(x, y, z);
                    p = matrix * p;
                    uniqueVertices.Add(p);
                }
                var numFaces = br.ReadInt32();

                for (var i = 0; i < numFaces; i++)
                {
                    var numTrianglesInFace = br.ReadInt32();
                    if (numTrianglesInFace == 0) continue;
                    var isPlanar = numTrianglesInFace > 0;
                    numTrianglesInFace = Mathf.Abs(numTrianglesInFace);
                    if (isPlanar)
                    {
                        var normal = br.ReadPackedNormal().Normal;
                        var wpfNormal = new Vector3((float)normal.X, (float)normal.Y, (float)normal.Z);
                        wpfNormal = matrix.rotation * wpfNormal;
                        var uniqueIndices = new Dictionary<int, int>();
                        for (var j = 0; j < numTrianglesInFace; j++)
                        {
                            for (int k = 0; k < 3; k++)
                            {
                                int idx = ReadIndex(br, numVertices);
                                int writtenIdx;
                                if (!uniqueIndices.TryGetValue(idx, out writtenIdx)) //we haven't got it, so add it
                                {
                                    writtenIdx = vertices.Count;
                                    vertices.Add(uniqueVertices[idx]);
                                    uniqueIndices.Add(idx, writtenIdx);
                                    //add a matching normal
                                    normals.Add(wpfNormal);
                                }
                                triangleIndices.Add(indexBase + writtenIdx);
                            }
                        }
                    }
                    else
                    {
                        var uniqueIndices = new Dictionary<int, int>();
                        for (var j = 0; j < numTrianglesInFace; j++)
                        {
                            for (int k = 0; k < 3; k++)
                            {
                                int idx = ReadIndex(br, numVertices);
                                var normal = br.ReadPackedNormal().Normal;
                                int writtenIdx;
                                var wpfNormal = new Vector3((float)normal.X, (float)normal.Y, (float)normal.Z);
                                wpfNormal = matrix.rotation * wpfNormal;
                                if (!uniqueIndices.TryGetValue(idx, out writtenIdx)) //we haven't got it, so add it
                                {
                                    writtenIdx = vertices.Count;
                                    vertices.Add(uniqueVertices[idx]);
                                    uniqueIndices.Add(idx, writtenIdx);
                                    normals.Add(wpfNormal);
                                }
                                else
                                {
                                    if (normals[writtenIdx] != wpfNormal) //deal with normals that vary at a node
                                    {
                                        writtenIdx = vertices.Count;
                                        vertices.Add(uniqueVertices[idx]);
                                        normals.Add(wpfNormal);
                                    }
                                }

                                triangleIndices.Add(indexBase + writtenIdx);
                            }
                        }
                    }
                }

                var positions = new List<Vector3>(m3D.vertexCount);
                m3D.GetVertices(positions);
                positions.AddRange(vertices);
                m3D.SetVertices(positions);

                var normals1 = new List<Vector3>(m3D.vertexCount);
                m3D.GetNormals(normals1);
                normals1.AddRange(normals);
                m3D.SetNormals(normals);

                m3D.SetTriangles(triangleIndices, m3D.subMeshCount);
            }
            // if(m3D.CanFreeze) m3D.Freeze(); //freeze the mesh to improve performance
        }
    }

    /// <summary>
    /// Reads a packed Xbim Triangle index from a stream
    /// </summary>
    /// <param name="br"></param>
    /// <param name="maxVertexCount">The size of the maximum number of vertices in the stream, i.e. the biggest index value</param>
    /// <returns></returns>
    private static int ReadIndex(BinaryReader br, int maxVertexCount)
    {
        if (maxVertexCount <= 0xFF)
            return br.ReadByte();
        if (maxVertexCount <= 0xFFFF)
            return br.ReadUInt16();
        return (int)br.ReadUInt32(); //this should never go over int32
    }
    public static void Read(this Mesh m3D, string shapeData, XbimMatrix3D? tr = null)
    {
        //var qrd = new RotateTransform3D();
        //Matrix3D? matrix3D = null;
        Matrix4x4 matrix = Matrix4x4.identity;
        if (tr.HasValue)
        {
            /*var xq = transform.Value.GetRotationQuaternion();
            var quaternion = new Quaternion(xq.X, xq.Y, xq.Z, xq.W);
            if (!quaternion.IsIdentity)
                qrd.Rotation = new QuaternionRotation3D(quaternion);
            else
                qrd = null;
            matrix3D = transform.Value.ToMatrix3D();*/
            matrix = new Matrix4x4(
                    new Vector4((float)tr.Value.M11, (float)tr.Value.M12, (float)tr.Value.M13, (float)tr.Value.M14),
                    new Vector4((float)tr.Value.M21, (float)tr.Value.M22, (float)tr.Value.M23, (float)tr.Value.M24),
                    new Vector4((float)tr.Value.M31, (float)tr.Value.M32, (float)tr.Value.M33, (float)tr.Value.M34),
                    new Vector4((float)tr.Value.OffsetX, (float)tr.Value.OffsetY, (float)tr.Value.OffsetZ, (float)tr.Value.M44));
        }

        using (StringReader sr = new StringReader(shapeData))
        {
            int version = 1;
            List<Vector3> vertexList = new List<Vector3>(512); //holds the actual unique positions of the vertices in this data set in the mesh
            List<Vector3> normalList = new List<Vector3>(512); //holds the actual unique normals of the vertices in this data set in the mesh

            List<Vector3> positions = new List<Vector3>(1024); //holds the actual positions of the vertices in this data set in the mesh
            List<Vector3> normals = new List<Vector3>(1024); //holds the actual normals of the vertices in this data set in the mesh
            List<int> triangleIndices = new List<int>(2048);
            String line;
            // Read and display lines from the data until the end of
            // the data is reached.

            while ((line = sr.ReadLine()) != null)
            {

                string[] tokens = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (tokens.Length > 0) //we need a command
                {
                    string command = tokens[0].Trim().ToUpper();
                    switch (command)
                    {
                        case "P":
                            vertexList = new List<Vector3>(512);
                            normalList = new List<Vector3>(512);
                            if (tokens.Length > 0)
                                version = Int32.Parse(tokens[1]);
                            break;
                        case "V": //process vertices
                            for (int i = 1; i < tokens.Length; i++)
                            {
                                string[] xyz = tokens[i].Split(',');
                                var p = new Vector3(Convert.ToSingle(xyz[0], CultureInfo.InvariantCulture),
                                                                  Convert.ToSingle(xyz[1], CultureInfo.InvariantCulture),
                                                                  Convert.ToSingle(xyz[2], CultureInfo.InvariantCulture));
                                p = matrix * p;
                                vertexList.Add(p);
                            }
                            break;
                        case "N": //processes normals
                            for (int i = 1; i < tokens.Length; i++)
                            {
                                string[] xyz = tokens[i].Split(',');
                                var v = new Vector3(Convert.ToSingle(xyz[0], CultureInfo.InvariantCulture),
                                                                   Convert.ToSingle(xyz[1], CultureInfo.InvariantCulture),
                                                                   Convert.ToSingle(xyz[2], CultureInfo.InvariantCulture));
                                normalList.Add(v);
                            }
                            break;
                        case "T": //process triangulated meshes
                            Vector3 currentNormal = new Vector3(0, 0, 0);
                            //each time we start a new mesh face we have to duplicate the vertices to ensure that we get correct shading of planar and non planar faces
                            var writtenVertices = new Dictionary<int, int>();

                            for (int i = 1; i < tokens.Length; i++)
                            {
                                string[] indices = tokens[i].Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                                if (indices.Length != 3) throw new Exception("Invalid triangle definition");
                                for (int t = 0; t < 3; t++)
                                {
                                    string[] indexNormalPair = indices[t].Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);

                                    if (indexNormalPair.Length > 1) //we have a normal defined
                                    {
                                        if (version == 1)
                                        {
                                            string normalStr = indexNormalPair[1].Trim();
                                            switch (normalStr)
                                            {
                                                case "F": //Front
                                                    currentNormal = new Vector3(0, -1, 0);
                                                    break;
                                                case "B": //Back
                                                    currentNormal = new Vector3(0, 1, 0);
                                                    break;
                                                case "L": //Left
                                                    currentNormal = new Vector3(-1, 0, 0);
                                                    break;
                                                case "R": //Right
                                                    currentNormal = new Vector3(1, 0, 0);
                                                    break;
                                                case "U": //Up
                                                    currentNormal = new Vector3(0, 0, 1);
                                                    break;
                                                case "D": //Down
                                                    currentNormal = new Vector3(0, 0, -1);
                                                    break;
                                                default: //it is an index number
                                                    int normalIndex = int.Parse(indexNormalPair[1]);
                                                    currentNormal = normalList[normalIndex];
                                                    break;
                                            }
                                        }
                                        else //we have support for packed normals
                                        {
                                            var packedNormal = new XbimPackedNormal(ushort.Parse(indexNormalPair[1]));
                                            var n = packedNormal.Normal;
                                            currentNormal = new Vector3((float)n.X, (float)n.Y, (float)n.Z);
                                        }
                                        currentNormal = matrix.rotation * currentNormal;
                                    }

                                    //now add the index
                                    int index = int.Parse(indexNormalPair[0]);

                                    int alreadyWrittenAt; //in case it is the first mesh
                                    if (!writtenVertices.TryGetValue(index, out alreadyWrittenAt)) //if we haven't  written it in this mesh pass, add it again unless it is the first one which we know has been written
                                    {
                                        //all vertices will be unique and have only one normal
                                        writtenVertices.Add(index, positions.Count);
                                        triangleIndices.Add(positions.Count);
                                        positions.Add(vertexList[index]);
                                        normals.Add(currentNormal);
                                    }
                                    else //just add the index reference
                                    {
                                        if (normals[alreadyWrittenAt] == currentNormal)
                                            triangleIndices.Add(alreadyWrittenAt);
                                        else //we need another
                                        {
                                            triangleIndices.Add(positions.Count);
                                            positions.Add(vertexList[index]);
                                            normals.Add(currentNormal);
                                        }
                                    }
                                }
                            }
                            break;
                        case "F": //skip faces for now, can be used to draw edges
                            break;
                        default:
                            throw new Exception("Invalid Geometry Command");

                    }
                }
            }
            
            var positions1 = new List<Vector3>(m3D.vertexCount);
            m3D.GetVertices(positions1);
            positions1.AddRange(positions);
            m3D.SetVertices(positions1);

            var normals1 = new List<Vector3>(m3D.vertexCount);
            m3D.GetNormals(normals1);
            normals1.AddRange(normals);
            m3D.SetNormals(normals);

            m3D.SetTriangles(triangleIndices, m3D.subMeshCount);
        }
    }

    //public static void Read(this MeshGeometry3D m3D, byte[] shapeData, XbimMatrix3D? transform = null)
    //{

    //    var qrd = new RotateTransform3D();
    //    Matrix3D? matrix3D = null;
    //    if (transform.HasValue)
    //    {
    //        XbimQuaternion xq = transform.Value.GetRotationQuaternion();
    //        qrd.Rotation = new QuaternionRotation3D(new Quaternion(xq.X, xq.Y, xq.Z, xq.W));
    //        matrix3D = transform.Value.ToMatrix3D();
    //    }

    //    using (var br = new BinaryReader(new MemoryStream(shapeData) ))
    //    {



    //        List<Point3D> vertexList = new List<Point3D>(512); //holds the actual unique positions of the vertices in this data set in the mesh
    //        List<Vector3D> normalList = new List<Vector3D>(512); //holds the actual unique normals of the vertices in this data set in the mesh

    //        List<Point3D> positions = new List<Point3D>(1024); //holds the actual positions of the vertices in this data set in the mesh
    //        List<Vector3D> normals = new List<Vector3D>(1024); //holds the actual normals of the vertices in this data set in the mesh
    //        List<int> triangleIndices = new List<int>(2048);
    //        String line;
    //        // Read and display lines from the data until the end of
    //        // the data is reached.

    //        while ((line = sr.ReadLine()) != null)
    //        {

    //            string[] tokens = line.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
    //            if (tokens.Length > 0) //we need a command
    //            {
    //                string command = tokens[0].Trim().ToUpper();
    //                switch (command)
    //                {
    //                    case "P":
    //                        vertexList = new List<Point3D>(512);
    //                        normalList = new List<Vector3D>(512);
    //                        break;
    //                    case "V": //process vertices
    //                        for (int i = 1; i < tokens.Length; i++)
    //                        {
    //                            string[] xyz = tokens[i].Split(',');
    //                            Point3D p = new Point3D(Convert.ToDouble(xyz[0], CultureInfo.InvariantCulture),
    //                                                              Convert.ToDouble(xyz[1], CultureInfo.InvariantCulture),
    //                                                              Convert.ToDouble(xyz[2], CultureInfo.InvariantCulture));
    //                            if (matrix3D.HasValue)
    //                                p = matrix3D.Value.Transform(p);
    //                            vertexList.Add(p);
    //                        }
    //                        break;
    //                    case "N": //processes normals
    //                        for (int i = 1; i < tokens.Length; i++)
    //                        {
    //                            string[] xyz = tokens[i].Split(',');
    //                            Vector3D v = new Vector3D(Convert.ToDouble(xyz[0], CultureInfo.InvariantCulture),
    //                                                               Convert.ToDouble(xyz[1], CultureInfo.InvariantCulture),
    //                                                               Convert.ToDouble(xyz[2], CultureInfo.InvariantCulture));
    //                            normalList.Add(v);
    //                        }
    //                        break;
    //                    case "T": //process triangulated meshes
    //                        Vector3D currentNormal = new Vector3D(0, 0, 0);
    //                        //each time we start a new mesh face we have to duplicate the vertices to ensure that we get correct shading of planar and non planar faces
    //                        Dictionary<int, int> writtenVertices = new Dictionary<int, int>();

    //                        for (int i = 1; i < tokens.Length; i++)
    //                        {
    //                            string[] indices = tokens[i].Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
    //                            if (indices.Length != 3) throw new Exception("Invalid triangle definition");
    //                            for (int t = 0; t < 3; t++)
    //                            {
    //                                string[] indexNormalPair = indices[t].Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);

    //                                if (indexNormalPair.Length > 1) //we have a normal defined
    //                                {
    //                                    string normalStr = indexNormalPair[1].Trim();
    //                                    switch (normalStr)
    //                                    {
    //                                        case "F": //Front
    //                                            currentNormal = new Vector3D(0, -1, 0);
    //                                            break;
    //                                        case "B": //Back
    //                                            currentNormal = new Vector3D(0, 1, 0);
    //                                            break;
    //                                        case "L": //Left
    //                                            currentNormal = new Vector3D(-1, 0, 0);
    //                                            break;
    //                                        case "R": //Right
    //                                            currentNormal = new Vector3D(1, 0, 0);
    //                                            break;
    //                                        case "U": //Up
    //                                            currentNormal = new Vector3D(0, 0, 1);
    //                                            break;
    //                                        case "D": //Down
    //                                            currentNormal = new Vector3D(0, 0, -1);
    //                                            break;
    //                                        default: //it is an index number
    //                                            int normalIndex = int.Parse(indexNormalPair[1]);
    //                                            currentNormal = normalList[normalIndex];
    //                                            break;
    //                                    }
    //                                    if (matrix3D.HasValue)
    //                                    {
    //                                        currentNormal = qrd.Transform(currentNormal);
    //                                    }
    //                                }

    //                                //now add the index
    //                                int index = int.Parse(indexNormalPair[0]);

    //                                int alreadyWrittenAt = index; //in case it is the first mesh
    //                                if (!writtenVertices.TryGetValue(index, out alreadyWrittenAt)) //if we haven't  written it in this mesh pass, add it again unless it is the first one which we know has been written
    //                                {
    //                                    //all vertices will be unique and have only one normal
    //                                    writtenVertices.Add(index, positions.Count);
    //                                    triangleIndices.Add(positions.Count + m3D.TriangleIndices.Count);
    //                                    positions.Add(vertexList[index]);
    //                                    normals.Add(currentNormal);
    //                                }
    //                                else //just add the index reference
    //                                {
    //                                    triangleIndices.Add(alreadyWrittenAt);
    //                                }
    //                            }
    //                        }

    //                        break;
    //                    case "F": //skip faces for now, can be used to draw edges
    //                        break;
    //                    default:
    //                        throw new Exception("Invalid Geometry Command");

    //                }
    //            }

    //        }

    //        m3D.Positions = new Point3DCollection(m3D.Positions.Concat(positions)); //we do this for wpf performance issues
    //        m3D.Normals = new Vector3DCollection(m3D.Normals.Concat(normals)); //we do this for wpf performance issues
    //        m3D.TriangleIndices = new Int32Collection(m3D.TriangleIndices.Concat(triangleIndices)); //we do this for wpf performance issues
    //    }

    //}
}
