using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using Xbim.Common;
using Xbim.Common.Geometry;
using Xbim.Common.XbimExtensions;
using Xbim.Ifc4.Interfaces;
using Xbim.ModelGeometry.Scene;

public class UnityMeshGeometry3D : IXbimMeshGeometry3D
{
    public GameObject UnityModel;
    private MeshRenderer _meshRenderer;
    private MeshFilter _meshFilter;
    private Mesh _mesh;
    private List<Vector3> _unfrozenPositions;
    private List<Int32> _unfrozenIndices;
    private List<Vector3> _unfrozenNormals;
    XbimMeshFragmentCollection _meshes = new XbimMeshFragmentCollection();
    private TriangleType _meshType;

    uint _previousToLastIndex;
    uint _lastIndex;
    uint _pointTally;
    uint _fanStartIndex;
    uint _indexOffset;

    #region standard calls

    private void Init()
    {
        //_indexOffset = (uint)Mesh.Positions.Count;
    }

    private void StandardBeginPolygon(TriangleType meshType)
    {
        _meshType = meshType;
        _pointTally = 0;
        _previousToLastIndex = 0;
        _lastIndex = 0;
        _fanStartIndex = 0;
    }
    #endregion

    public void ReportGeometryTo(StringBuilder sb)
    {
        var i = 0;
        using (var pEn = Positions.GetEnumerator())
        using (var nEn = Normals.GetEnumerator())
        {
            while (pEn.MoveNext() && nEn.MoveNext())
            {
                var p = pEn.Current;
                var n = nEn.Current;
                sb.AppendFormat("{0} pos: {1} nrm:{2}\r\n", i++, p, n);
            }

            i = 0;
            sb.AppendLine("Triangles:");
            foreach (var item in TriangleIndices)
            {
                sb.AppendFormat("{0}, ", item);
                i++;
                if (i % 3 == 0)
                {
                    sb.AppendLine();
                }
            }
        }
    }

    /*public static UnityMeshGeometry3D GetGeometry(IPersistEntity entity, XbimMatrix3D modelTransform, Material mat)
    {
        var tgt = new UnityMeshGeometry3D(mat, mat);
        tgt.BeginUpdate();
        using (var geomstore = entity.Model.GeometryStore)
        using (var geomReader = geomstore.BeginRead())
        {
            foreach (var shapeInstance in geomReader.ShapeInstancesOfEntity(entity).Where(x => x.RepresentationType == XbimGeometryRepresentationType.OpeningsAndAdditionsIncluded))
            {
                IXbimShapeGeometryData shapegeom = geomReader.ShapeGeometry(shapeInstance.ShapeGeometryLabel);
                if (shapegeom.Format != (byte)XbimGeometryType.PolyhedronBinary)
                    continue;
                var transform = shapeInstance.Transformation * modelTransform;
                tgt.Add(
                    shapegeom.ShapeData,
                    shapeInstance.IfcTypeId,
                    shapeInstance.IfcProductLabel,
                    shapeInstance.InstanceLabel,
                    transform,
                    (short)entity.Model.UserDefinedId
                    );
            }
        }
        tgt.EndUpdate();
        return tgt;
    }*/

    // attempting to load the shapeGeometry from the database; 
    // 
    /*public static UnityMeshGeometry3D GetGeometry(IIfcShapeRepresentation rep, XbimModelPositioningCollection positions, Material mat, bool wcsAdjust)
    {
        var productContexts = rep.OfProductRepresentation?.OfType<IIfcProductDefinitionShape>().SelectMany(x => x.ShapeOfProduct);
        var representationLabels = rep.Items.Select(x => x.EntityLabel);
        var selModel = rep.Model;
        var modelTransform = positions[selModel].Transform;

        return GetRepresentationGeometry(mat, productContexts, representationLabels, selModel, modelTransform, wcsAdjust);
    }*/


    /*internal static UnityMeshGeometry3D GetRepresentationGeometry2(Material mat, IEnumerable<int> representationLabels, IModel selModel, XbimMatrix3D modelTransform, bool wcsAdjust, XbimShapeGeometry shapegeom, IIfcProduct contextualProduct)
    {
        var placementTree = new XbimPlacementTree(selModel, wcsAdjust);
        var trsf = placementTree[contextualProduct.ObjectPlacement.EntityLabel];
        var tgt = new UnityMeshGeometry3D(mat, mat);
        tgt.BeginUpdate();
        if (shapegeom.Format == XbimGeometryType.PolyhedronBinary)
        {
            // Debug.WriteLine($"adding {shapegeom.ShapeLabel} at {DateTime.Now.ToLongTimeString()}");
            var transform = trsf * modelTransform;
            tgt.Add(
                ((IXbimShapeGeometryData)shapegeom).ShapeData,
                contextualProduct.ExpressType.TypeId, // shapeInstance.IfcTypeId,
                contextualProduct.EntityLabel, // shapeInstance.IfcProductLabel,
                -1, // shapeInstance.InstanceLabel,
                transform,
                (short)contextualProduct.Model.UserDefinedId
            );

        }
        tgt.EndUpdate();
        return tgt;
    }*/

    // attempting to load the shapeGeometry from the database; 
    // 
    /*internal static WpfMeshGeometry3D GetRepresentationGeometry(Material mat, IEnumerable<IIfcProduct> productContexts, IEnumerable<int> representationLabels, IModel selModel, XbimMatrix3D modelTransform, bool wcsAdjust)
    {
        var placementTree = new XbimPlacementTree(selModel, wcsAdjust);
        var tgt = new WpfMeshGeometry3D(mat, mat);
        tgt.BeginUpdate();
        using (var geomstore = selModel.GeometryStore)
        using (var geomReader = geomstore.BeginRead())
        {
            var matchingGeometries = geomReader.ShapeGeometries.Where(x => representationLabels.Contains(x.IfcShapeLabel));
            foreach (var contextualProduct in productContexts)
            {
                var trsf = placementTree[contextualProduct.ObjectPlacement.EntityLabel];
                foreach (IXbimShapeGeometryData shapegeom in matchingGeometries)
                {
                    if (shapegeom.Format != (byte)XbimGeometryType.PolyhedronBinary)
                        continue;
                    // Debug.WriteLine($"adding {shapegeom.ShapeLabel} at {DateTime.Now.ToLongTimeString()}");
                    var transform = trsf * modelTransform;
                    tgt.Add(
                        shapegeom.ShapeData,
                        contextualProduct.ExpressType.TypeId, // shapeInstance.IfcTypeId,
                        contextualProduct.EntityLabel, // shapeInstance.IfcProductLabel,
                        -1, // shapeInstance.InstanceLabel,
                        transform,
                        (short)contextualProduct.Model.UserDefinedId
                    );
                }
            }
        }
        tgt.EndUpdate();
        return tgt;
    }*/

    /*public static WpfMeshGeometry3D GetGeometry(EntitySelection selection, XbimModelPositioningCollection positions, Material mat)
    {
        var tgt = new WpfMeshGeometry3D(mat, mat);
        tgt.BeginUpdate();
        foreach (var modelgroup in selection.GroupBy(i => i.Model))
        {
            var model = modelgroup.Key;
            var modelTransform = positions[model]?.Transform;
            if (modelTransform != null)
            {
                using (var geomstore = model.GeometryStore)
                using (var geomReader = geomstore.BeginRead())
                {
                    foreach (var item in modelgroup)
                    {
                        foreach (var shapeInstance in geomReader.ShapeInstancesOfEntity(item).Where(x => x.RepresentationType != XbimGeometryRepresentationType.OpeningsAndAdditionsExcluded))
                        {
                            IXbimShapeGeometryData shapegeom = geomReader.ShapeGeometry(shapeInstance.ShapeGeometryLabel);
                            if (shapegeom.Format != (byte)XbimGeometryType.PolyhedronBinary)
                                continue;
                            var transform = shapeInstance.Transformation * modelTransform;
                            tgt.Add(
                                shapegeom.ShapeData,
                                shapeInstance.IfcTypeId,
                                shapeInstance.IfcProductLabel,
                                shapeInstance.InstanceLabel,
                                transform,
                                (short)model.UserDefinedId
                                );
                        }
                    }
                }
            }
        }
        tgt.EndUpdate();
        return tgt;
    }*/

    public UnityMeshGeometry3D(string name)
    {
        UnityModel = new GameObject();
        UnityModel.name = name;
        _meshRenderer = UnityModel.AddComponent<MeshRenderer>();
        _meshFilter = UnityModel.AddComponent<MeshFilter>();
        _mesh = new Mesh();
        _mesh.name = name;
        _meshFilter.mesh = _mesh;
    }

    public UnityMeshGeometry3D(string name, IXbimMeshGeometry3D mesh)
    {
        UnityModel = new GameObject();
        UnityModel.name = name;
        _meshRenderer = UnityModel.AddComponent<MeshRenderer>();
        _meshFilter = UnityModel.AddComponent<MeshFilter>();
        _mesh = new Mesh();
        _mesh.name = name;

        var vertices = new List<Vector3>(mesh.PositionCount);
        foreach (var item in mesh.Positions)
        {
            vertices.Add(new Vector3((float)item.X, (float)item.Y, (float)item.Z));
        }
        _mesh.SetVertices(vertices);

        _mesh.SetIndices(mesh.TriangleIndices.ToArray(), MeshTopology.Triangles, 0);

        var normals = new List<Vector3>(mesh.PositionCount);
        foreach (var item in mesh.Normals)
        {
            normals.Add(new Vector3((float)item.X, (float)item.Y, (float)item.Z));
        }
        _mesh.SetNormals(normals);
        _mesh.RecalculateBounds();
        _meshFilter.mesh = _mesh;
        _meshes = new XbimMeshFragmentCollection(mesh.Meshes);
    }

    public UnityMeshGeometry3D(string name, List<Material> materials)
    {
        UnityModel = new GameObject();
        UnityModel.name = name;
        _meshRenderer = UnityModel.AddComponent<MeshRenderer>();
        _meshRenderer.materials = materials.ToArray();
        _meshFilter = UnityModel.AddComponent<MeshFilter>();
        _mesh = new Mesh();
        _mesh.name = name;
        _meshFilter.mesh = _mesh;
    }

    public static implicit operator GameObject(UnityMeshGeometry3D mesh)
    {
        return mesh.UnityModel ??
            (mesh.UnityModel = new GameObject());
    }

    public Mesh Mesh
    {
        get
        {
            return _mesh;
        }
    }

    public XbimMeshFragmentCollection Meshes
    {
        get { return _meshes; }
        set
        {
            _meshes = new XbimMeshFragmentCollection(value);
        }
    }

    /// <summary>
    /// Do not use this rather create a XbimMeshGeometry3D first and construct this from it, appending WPF collections is slow
    /// </summary>
    /// <param name="geometryMeshData"></param>
    /// <param name="modelId"></param>
    public bool Add(XbimGeometryData geometryMeshData, short modelId)
    {
        throw new NotImplementedException();
    }

    public IEnumerable<XbimPoint3D> Positions
    {
        get 
        {
            var vertices = new List<Vector3>(_mesh.vertexCount);
            _mesh.GetVertices(vertices);
            var points = new List<XbimPoint3D>(_mesh.vertexCount);
            foreach (var item in vertices)
            {
                points.Add(new XbimPoint3D(item.x, item.y, item.z));
            }
            return points;
        }
        set
        {
            if (_mesh == null)
            {
                _unfrozenPositions = new List<Vector3>(value.Count());
                foreach (var xbimPoint3D in value)
                {
                    _unfrozenPositions.Add(new Vector3((float)xbimPoint3D.X, (float)xbimPoint3D.Y, (float)xbimPoint3D.Z));
                }
            }
            else
            {
                var vertices = new List<Vector3>(value.Count());
                foreach (var item in value)
                {
                    vertices.Add(new Vector3((float)item.X, (float)item.Y, (float)item.Z));
                }
                _mesh.SetVertices(vertices);
            }
        }
    }

    public IEnumerable<XbimVector3D> Normals
    {
        get 
        {
            var normals = new List<Vector3>(_mesh.vertexCount);
            _mesh.GetNormals(normals);
            var vectors = new List<XbimVector3D>(_mesh.vertexCount);
            foreach (var item in normals)
            {
                vectors.Add(new XbimVector3D(item.x, item.y, item.z));
            }
            return vectors;
        }
        set
        {
            if (_mesh == null)
            {
                _unfrozenNormals = new List<Vector3>(value.Count());
                foreach (var xbimV3D in value)
                {
                    _unfrozenNormals.Add(new Vector3((float)xbimV3D.X, (float)xbimV3D.Y, (float)xbimV3D.Z));
                }
            }
            else
            {
                var normals = new List<Vector3>(value.Count());
                foreach (var item in value)
                {
                    normals.Add(new Vector3((float)item.X, (float)item.Y, (float)item.Z));
                }
                _mesh.SetNormals(normals);
            }
        }
    }

    public IList<int> TriangleIndices
    {
        get 
        { 
            return _mesh.GetIndices(0);
        }
        set
        {
            if (_mesh == null)
            {
                _unfrozenIndices = value.ToList();
            }
            else
                _mesh.SetIndices(value.ToArray(), MeshTopology.Triangles, 0);
        }
    }

    public void MoveTo(IXbimMeshGeometry3D toMesh)
    {
        if (_meshes.Any()) //if no meshes nothing to move
        {
            toMesh.Positions = new List<XbimPoint3D>(Positions);
            toMesh.Normals = new List<XbimVector3D>(Normals);
            toMesh.TriangleIndices = new List<int>(TriangleIndices);

            toMesh.Meshes = new XbimMeshFragmentCollection(Meshes);

            _meshes.Clear();
            _mesh = new Mesh();
            _meshFilter.mesh = _mesh;
        }
    }

    public GameObject ToGeometryModel3D()
    {
        return UnityModel;
    }

    /*public Mesh GetWpfMeshGeometry3D(XbimMeshFragment frag)
    {
        var m3D = new MeshGeometry3D();
        var m = Mesh;
        if (m != null)
        {
            for (int i = frag.StartPosition; i <= frag.EndPosition; i++)
            {
                Point3D p = m.Positions[i];
                m3D.Positions.Add(p);
                if (m.Normals != null)
                {
                    Vector3D v = m.Normals[i];
                    m3D.Normals.Add(v);
                }
            }
            for (int i = frag.StartTriangleIndex; i <= frag.EndTriangleIndex; i++)
            {
                m3D.TriangleIndices.Add(m.TriangleIndices[i] - frag.StartPosition);
            }
        }
        return m3D;
    }*/

    public IXbimMeshGeometry3D GetMeshGeometry3D(XbimMeshFragment frag)
    {
        var m3D = new XbimMeshGeometry3D();
        var m = Mesh;
        if (m != null)
        {
            var vertices = new List<Vector3>();
            m.GetVertices(vertices);
            var normals = new List<Vector3>();
            m.GetNormals(normals);
            var indices = m.GetIndices(0);
            for (int i = frag.StartPosition; i <= frag.EndPosition; i++)
            {
                var p = vertices[i];
                var v = normals[i];
                m3D.Positions.Add(new XbimPoint3D(p.x, p.y, p.z));
                m3D.Normals.Add(new XbimVector3D(v.x, v.y, v.z));
            }
            for (int i = frag.StartTriangleIndex; i <= frag.EndTriangleIndex; i++)
            {
                m3D.TriangleIndices.Add(indices[i] - frag.StartPosition);
            }
            m3D.Meshes.Add(new XbimMeshFragment(0, 0, 0)
            {
                EndPosition = m3D.PositionCount - 1,
                StartTriangleIndex = frag.StartTriangleIndex - m3D.PositionCount - 1,
                EndTriangleIndex = frag.EndTriangleIndex - m3D.PositionCount - 1
            });
        }
        return m3D;
    }

    public XbimRect3D GetBounds()
    {
        bool first = true;
        XbimRect3D boundingBox = XbimRect3D.Empty;
        foreach (var pos in Positions)
        {
            if (first)
            {
                boundingBox = new XbimRect3D(pos);
                first = false;
            }
            else
                boundingBox.Union(pos);
        }
        return boundingBox;
    }



    public int PositionCount
    {
        get
        {
            if (Mesh == null && _unfrozenPositions == null)
            {
                return 0;
            }
            if (Mesh != null)
            {
                return Mesh.vertexCount;
            }
            return _unfrozenPositions.Count;
        }
    }

    public int TriangleIndexCount
    {
        get
        {
            if (Mesh == null && _unfrozenIndices == null)
            {
                return 0;
            }
            if (Mesh != null)
            {
                return _mesh.GetIndices(0).Length;
            }
            return _unfrozenIndices.Count;
            
        }
    }

    XbimMeshFragment IXbimMeshGeometry3D.Add(IXbimGeometryModel geometryModel, IIfcProduct product, XbimMatrix3D transform, double? deflection, short modelId)
    {
        return geometryModel.MeshTo(this, product, transform, deflection ?? product.Model.ModelFactors.DeflectionTolerance, modelId);
    }

    public void BeginBuild()
    {
        Init();
    }

    public void BeginPositions(uint numPoints)
    {
        //Mesh.Positions = new WpfPoint3DCollection((int)numPoints);
    }

    public void AddPosition(XbimPoint3D pt)
    {
        //Mesh.Positions.Add(new Point3D(pt.X, pt.Y, pt.Z));
    }

    public void EndPositions()
    {

    }

    public void BeginPolygons(uint totalNumberTriangles, uint numPolygons)
    {

    }

    public void BeginPolygon(TriangleType meshType, uint indicesCount)
    {
        StandardBeginPolygon(meshType);
    }

    private int Offset(uint index)
    {
        return (int)(index + _indexOffset);
    }

    public void AddTriangleIndex(uint index)
    {
        if (_pointTally == 0)
            _fanStartIndex = index;
        if (_pointTally < 3) //first time
        {
            TriangleIndices.Add(Offset(index));
            // _meshGeometry.Positions.Add(_points[(int)index]);
        }
        else
        {
            switch (_meshType)
            {
                case TriangleType.GL_Triangles://      0x0004
                    TriangleIndices.Add(Offset(index));
                    break;
                case TriangleType.GL_Triangles_Strip:// 0x0005
                    if (_pointTally % 2 == 0)
                    {
                        TriangleIndices.Add(Offset(_previousToLastIndex));
                        TriangleIndices.Add(Offset(_lastIndex));
                    }
                    else
                    {
                        TriangleIndices.Add(Offset(_lastIndex));
                        TriangleIndices.Add(Offset(_previousToLastIndex));
                    }
                    TriangleIndices.Add(Offset(index));
                    break;
                case TriangleType.GL_Triangles_Fan://   0x0006
                    TriangleIndices.Add(Offset(_fanStartIndex));
                    TriangleIndices.Add(Offset(_lastIndex));
                    TriangleIndices.Add(Offset(index));
                    break;
            }
        }
        _previousToLastIndex = _lastIndex;
        _lastIndex = index;
        _pointTally++;
    }

    public void EndPolygon()
    {
    }

    public void EndPolygons()
    {

    }

    public void EndBuild()
    {

    }


    public void BeginPoints(uint numPoints)
    {

    }

    public void AddNormal(XbimVector3D normal)
    {
        // throw new NotImplementedException();
    }

    public void EndPoints()
    {

    }

    public void Add(string mesh, short productTypeId, int productLabel, int geometryLabel, XbimMatrix3D? transform, short modelId)
    {
        XbimMeshFragment frag = new XbimMeshFragment(PositionCount, TriangleIndexCount, productTypeId, productLabel, geometryLabel, modelId);
        Read(mesh, transform);
        frag.EndPosition = PositionCount - 1;
        frag.EndTriangleIndex = TriangleIndexCount - 1;
        _meshes.Add(frag);
    }

    public XbimMeshFragment Add(IXbimGeometryModel geometryModel, IIfcProduct product, XbimMatrix3D transform, double? deflection, short modelId = 0)
    {
        throw new NotImplementedException();
    }

    // this is legacy code from previous versions.
    // 
    public bool Read(string data, XbimMatrix3D? tr = null)
    {
        int version = 1;
        using (var sr = new StringReader(data))
        {
            Matrix4x4 matrix = Matrix4x4.identity;
            //var r = new RotateTransform3D();
            if (tr.HasValue) //set up the windows media transforms
            {
                /*m3D = new Matrix3D(tr.Value.M11, tr.Value.M12, tr.Value.M13, tr.Value.M14,
                                              tr.Value.M21, tr.Value.M22, tr.Value.M23, tr.Value.M24,
                                              tr.Value.M31, tr.Value.M32, tr.Value.M33, tr.Value.M34,
                                              tr.Value.OffsetX, tr.Value.OffsetY, tr.Value.OffsetZ, tr.Value.M44);
                r = tr.Value.GetRotateTransform3D();*/
                matrix = new Matrix4x4(
                    new Vector4((float)tr.Value.M11, (float)tr.Value.M12, (float)tr.Value.M13, (float)tr.Value.M14),
                    new Vector4((float)tr.Value.M21, (float)tr.Value.M22, (float)tr.Value.M23, (float)tr.Value.M24),
                    new Vector4((float)tr.Value.M31, (float)tr.Value.M32, (float)tr.Value.M33, (float)tr.Value.M34),
                    new Vector4((float)tr.Value.OffsetX, (float)tr.Value.OffsetY, (float)tr.Value.OffsetZ, (float)tr.Value.M44));
            }
            var vertexList = new List<Vector3>(); //holds the actual positions of the vertices in this data set in the mesh
            var normalList = new List<Vector3>(); //holds the actual normals of the vertices in this data set in the mesh
            string line;
            // Read and display lines from the data until the end of
            // the data is reached.
            while ((line = sr.ReadLine()) != null)
            {
                var tokens = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (tokens.Length <= 1)
                    continue;
                var command = tokens[0].Trim().ToUpper();
                switch (command)
                {
                    case "P":
                        var pointCount = 512;

                        var faceCount = 128;

                        var triangleCount = 256;
                        var normalCount = 512;
                        if (tokens.Length > 0)
                            version = Int32.Parse(tokens[1]);
                        if (tokens.Length > 1)
                            pointCount = Int32.Parse(tokens[2]);
                        if (tokens.Length > 2)
                            faceCount = Int32.Parse(tokens[3]);

                        if (tokens.Length > 3)
                            triangleCount = Int32.Parse(tokens[4]);
                        if (tokens.Length > 4)
                            normalCount = Math.Max(Int32.Parse(tokens[5]), pointCount); //can't really have less normals than points
                        vertexList = new List<Vector3>(pointCount);
                        normalList = new List<Vector3>(normalCount);
                        //for efficienciency avoid continual regrowing
                        //this.Mesh.Positions = this.Mesh.Positions.GrowBy(pointCount);
                        //this.Mesh.Normals = this.Mesh.Normals.GrowBy(normalCount);
                        //this.Mesh.TriangleIndices = this.Mesh.TriangleIndices.GrowBy(triangleCount*3);
                        break;
                    case "F":
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
                        var currentNormal = new Vector3();
                        //each time we start a new mesh face we have to duplicate the vertices to ensure that we get correct shading of planar and non planar faces
                        var writtenVertices = new Dictionary<int, int>();

                        for (var i = 1; i < tokens.Length; i++)
                        {
                            var triangleIndices = tokens[i].Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                            if (triangleIndices.Length != 3) throw new Exception("Invalid triangle definition");
                            for (var t = 0; t < 3; t++)
                            {
                                var indexNormalPair = triangleIndices[t].Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);

                                if (indexNormalPair.Length > 1) //we have a normal defined
                                {
                                    if (version == 1)
                                    {
                                        var normalStr = indexNormalPair[1].Trim();
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
                                    if (tr.HasValue)
                                    {
                                        //currentNormal = r.Transform(currentNormal);
                                        currentNormal = matrix.rotation * currentNormal;
                                    }
                                }
                                //now add the index
                                var index = int.Parse(indexNormalPair[0]);

                                int alreadyWrittenAt; //in case it is the first mesh
                                if (!writtenVertices.TryGetValue(index, out alreadyWrittenAt)) //if we haven't  written it in this mesh pass, add it again unless it is the first one which we know has been written
                                {
                                    //all vertices will be unique and have only one normal
                                    writtenVertices.Add(index, PositionCount);

                                    _unfrozenIndices.Add(PositionCount);
                                    _unfrozenPositions.Add(vertexList[index]);
                                    _unfrozenNormals.Add(currentNormal);

                                }
                                else //just add the index reference
                                {
                                    if (_unfrozenNormals[alreadyWrittenAt] == currentNormal)
                                        _unfrozenIndices.Add(alreadyWrittenAt);
                                    else //we need another
                                    {
                                        _unfrozenIndices.Add(PositionCount);
                                        _unfrozenPositions.Add(vertexList[index]);
                                        _unfrozenNormals.Add(currentNormal);
                                    }
                                }

                            }
                        }

                        break;
                    default:
                        throw new Exception("Invalid Geometry Command");
                }
            }
        }
        return true;
    }

    public void Add(byte[] mesh, short productTypeId, int productLabel, int geometryLabel, XbimMatrix3D? transform = null, short modelId = 0)
    {
        var frag = new XbimMeshFragment(PositionCount, TriangleIndexCount, productTypeId, productLabel, geometryLabel, modelId);
        Read(mesh, transform);
        frag.EndPosition = PositionCount - 1;
        frag.EndTriangleIndex = TriangleIndexCount - 1;
        _meshes.Add(frag);
    }

    /// <summary>
    /// Reads a triangulated mesh from a byte array 
    /// </summary>
    /// <param name="mesh">the binary data of the mesh</param>
    /// <param name="transform">transforms the mesh if the matrix is not null</param>
    public void Read(byte[] mesh, XbimMatrix3D? tr = null)
    {
        int indexBase = _unfrozenPositions.Count;
        //var qrd = new RotateTransform3D();
        Matrix4x4 matrix = Matrix4x4.identity;
        if (tr.HasValue)
        {
            /*var xq = transform.Value.GetRotationQuaternion();
            var quaternion = new Quaternion((float)xq.X, (float)xq.Y, (float)xq.Z, (float)xq.W);
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
                var t = br.ReadShapeTriangulation();
                List<float[]> pts;
                List<int> idx;
                t.ToPointsWithNormalsAndIndices(out pts, out idx);


                // add to unfrozen list
                //
                _unfrozenPositions.Capacity += pts.Count;
                _unfrozenNormals.Capacity += pts.Count;
                _unfrozenIndices.Capacity += idx.Count;
                foreach (var floatsArray in pts)
                {
                    var wpfPosition = new Vector3(floatsArray[0], floatsArray[1], floatsArray[2]);
                    wpfPosition = matrix.MultiplyPoint3x4(wpfPosition);
                    _unfrozenPositions.Add(wpfPosition);

                    var wpfNormal = new Vector3(floatsArray[3], floatsArray[4], floatsArray[5]);
                    wpfNormal = matrix.MultiplyVector(wpfNormal);
                    _unfrozenNormals.Add(wpfNormal);
                }
                foreach (var index in idx)
                {
                    _unfrozenIndices.Add(index + indexBase);
                }
            }
        }
    }

    /// <summary>
    /// Ends an update and freezes the geometry
    /// </summary>
    public void EndUpdate()
    {
        _mesh = new Mesh();
        _mesh.name = UnityModel.name;
        _mesh.SetVertices(_unfrozenPositions);
        _mesh.SetNormals(_unfrozenNormals);
        _mesh.SetIndices(_unfrozenIndices, MeshTopology.Triangles, 0);
        _mesh.RecalculateBounds();

        _meshFilter.mesh = _mesh;


        var _meshCollider = UnityModel.AddComponent<MeshCollider>();
        _meshCollider.sharedMesh = _mesh;

        _unfrozenPositions = null;
        _unfrozenIndices = null;
        _unfrozenNormals = null;
    }
    public void BeginUpdate()
    {
        _unfrozenPositions = new List<Vector3>();
        _unfrozenIndices = new List<int>();
        _unfrozenNormals = new List<Vector3>();
        _meshFilter.mesh = null;
        _mesh = null;
    }

    public void Add(string mesh, Type productType, int productLabel, int geometryLabel, XbimMatrix3D? transform = null, short modelId = 0)
    {
        throw new NotImplementedException();
    }
}
