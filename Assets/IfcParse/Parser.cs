using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Xbim.Common;
using Xbim.Ifc;
using Xbim.Ifc4;
using Xbim.Ifc4.Interfaces;
using Xbim.Ifc4.Kernel;
using Xbim.Ifc4.SharedBldgElements;
using Xbim.Ifc4.GeometricModelResource;
using System;
using Xbim.IO;
using Xbim.ModelGeometry.Scene;
using Xbim.Common.Geometry;
using Xbim.Common.XbimExtensions;
using System.Reflection;
using System.Text;
using Xbim.Common.Metadata;


public class Parser : MonoBehaviour
{
    public class MaterialData
    {
        public bool IsTransparent { get; set; }
        public List<Material> Materials { get; set; }

        public string Description { get; set; }
    }

    private static readonly IEntityFactory ef4 = new EntityFactoryIfc4();

    public static List<Type> DefaultExcludedTypes = new List<Type>()
        {
            typeof(Xbim.Ifc2x3.ProductExtension.IfcSpace),
            typeof(Xbim.Ifc4.ProductExtension.IfcSpace),
            typeof(Xbim.Ifc2x3.ProductExtension.IfcFeatureElement),
            typeof(Xbim.Ifc4.ProductExtension.IfcFeatureElement)
        };

    public List<Type> ExcludedTypes = new List<Type>(DefaultExcludedTypes);

    readonly XbimColourMap _colourMap = new XbimColourMap();

    public bool UseMaps = false;

    public XbimModelPositioningCollection ModelPositions = new XbimModelPositioningCollection();

    // Start is called before the first frame update

    public GameObject Parse(string file)
    {
        using (var model = IfcStore.Open(file))
        {
            //ParseXBim(model.ReferencingModel, ModelPositions[model.ReferencingModel].Transform, null, null, DefaultExcludedTypes);
            var obj = ParseXBim(model.ReferencingModel, XbimMatrix3D.Identity, null, null, DefaultExcludedTypes);
            obj.name = Path.GetFileNameWithoutExtension(file);
            return obj;
        }
    }

    protected MaterialData GetWpfMaterialByType(IModel model, short typeid)
    {
        var prodType = model.Metadata.ExpressType(typeid);
        var v = _colourMap[prodType.Name];
        var texture = XbimTexture.Create(v);

        var transparent = true;
        var materials = new List<Material>();
        var description = "";
        var descBuilder = new StringBuilder();
        descBuilder.Append("Texture");

        foreach (var colour in texture.ColourMap)
        {
            if (!colour.IsTransparent)
                transparent = false; //only transparent if everything is transparent
            descBuilder.AppendFormat(" {0}", colour);
            materials.Add(MaterialFromColour(colour));
        }
        description = descBuilder.ToString();
        return new MaterialData() { IsTransparent = transparent, Materials = materials, Description = description };
    }

    protected MaterialData GetMaterial(IModel model, int styleId)
    {
        var sStyle = model.Instances[styleId] as IIfcSurfaceStyle;
        var texture = XbimTexture.Create(sStyle);
        if (texture.ColourMap.Count > 0)
        {
            if (texture.ColourMap[0].Alpha <= 0)
            {
                texture.ColourMap[0].Alpha = 0.5f;
                Debug.LogWarningFormat("Fully transparent style {0} forced to 50% opacity.", styleId);
            }
        }

        texture.DefinedObjectId = styleId;
        var transparent = true;
        var materials = new List<Material>();
        var description = "";
        var descBuilder = new StringBuilder();
        descBuilder.Append("Texture");

        foreach (var colour in texture.ColourMap)
        {
            if (!colour.IsTransparent)
                transparent = false; //only transparent if everything is transparent
            descBuilder.AppendFormat(" {0}", colour);
            materials.Add(MaterialFromColour(colour));
        }
        description = descBuilder.ToString();
        return new MaterialData() { IsTransparent = transparent, Materials = materials, Description = description };
    }

    private Material MaterialFromColour(XbimColour colour)
    {
        // build material
        Material mat = new Material(Shader.Find("Custom/TwoSided"));
        mat.SetColor("_Color", new Color(colour.Red, colour.Green, colour.Blue, colour.Alpha));
        if (colour.SpecularFactor > 0)
            mat.SetFloat("_Glossiness", colour.SpecularFactor);
        else if (colour.ReflectionFactor > 0)
            mat.SetColor("_EmissionColor", new Color(colour.Red, colour.Green, colour.Blue, colour.Alpha));
        return mat;
    }

    protected static UnityMeshGeometry3D GetNewStyleMesh(string name, MaterialData material, Transform parent = null)
    {
        /*var mg = new WpfMeshGeometry3D(wpfMaterial, wpfMaterial);
        // set the tag of the child of mg to mg, so that it can be identified on click
        mg.WpfModel.SetValue(FrameworkElement.TagProperty, mg);
        mg.BeginUpdate();
        if (wpfMaterial.IsTransparent)
            tmpTransparentsGroup.Children.Add(mg);
        else
            tmpOpaquesGroup.Children.Add(mg);
        return mg;*/
        var mg = new UnityMeshGeometry3D(name, material.Materials);
        mg.BeginUpdate();
        if (parent != null)
        {
            mg.UnityModel.transform.parent = parent;
        }
        /*if (wpfMaterial.IsTransparent)
            tmpTransparentsGroup.Children.Add(mg);
        else
            tmpOpaquesGroup.Children.Add(mg);*/
        return mg;
    }

    protected IEnumerable<XbimShapeInstance> GetShapeInstancesToRender(IGeometryStoreReader geomReader, HashSet<short> excludedTypes)
    {
        var shapeInstances = geomReader.ShapeInstances
            .Where(s => s.RepresentationType == XbimGeometryRepresentationType.OpeningsAndAdditionsIncluded
                        &&
                        !excludedTypes.Contains(s.IfcTypeId));
        return shapeInstances;
    }

    private GameObject ParseXBim(IModel model, XbimMatrix3D modelTransform, List<IPersistEntity> isolateInstances = null, List<IPersistEntity> hideInstances = null, List<Type> excludeTypes = null)
    {
        var excludedTypes = model.DefaultExclusions(excludeTypes);
        var onlyInstances = isolateInstances?.Where(i => i.Model == model).ToDictionary(i => i.EntityLabel);
        var hiddenInstances = hideInstances?.Where(i => i.Model == model).ToDictionary(i => i.EntityLabel);

        //var scene = new XbimScene<WpfMeshGeometry3D, WpfMaterial>(model);
        //var timer = new Stopwatch();
        //timer.Start();
        var parent = new GameObject();
        using (var geomStore = model.GeometryStore)
        {
            using (var geomReader = geomStore.BeginRead())
            {
                var materialsByStyleId = new Dictionary<int, MaterialData>();
                var repeatedShapeGeometries = new Dictionary<int, Mesh>();
                var meshesByStyleId = new Dictionary<int, UnityMeshGeometry3D>();
                //var tmpOpaquesGroup = new Model3DGroup();
                //var tmpTransparentsGroup = new Model3DGroup();

                //get a list of all the unique style ids then build their style and mesh
                var sstyleIds = geomReader.StyleIds;
                foreach (var styleId in sstyleIds)
                {
                    var materialData = GetMaterial(model, styleId);
                    materialsByStyleId.Add(styleId, materialData);

                    var mg = GetNewStyleMesh(styleId.ToString(), materialData, parent.transform);
                    meshesByStyleId.Add(styleId, mg);
                }

                var shapeInstances = GetShapeInstancesToRender(geomReader, excludedTypes);
                Debug.LogFormat("shapeInstances, {0}", shapeInstances.Count());
                //var tot = 1;
                //if (ProgressChanged != null)
                //{
                //    // only enumerate if there's a need for progress update
                //    tot = shapeInstances.Count();
                //}
                //var prog = 0;
                //var lastProgress = 0;

                // !typeof (IfcFeatureElement).IsAssignableFrom(IfcMetaData.GetType(s.IfcTypeId)) /*&&
                // !typeof(IfcSpace).IsAssignableFrom(IfcMetaData.GetType(s.IfcTypeId))*/);
                foreach (var shapeInstance in shapeInstances
                    .Where(s => null == onlyInstances || onlyInstances.Count == 0 || onlyInstances.Keys.Contains(s.IfcProductLabel))
                    .Where(s => null == hiddenInstances || hiddenInstances.Count == 0 || !hiddenInstances.Keys.Contains(s.IfcProductLabel)))
                {
                    var styleId = shapeInstance.StyleLabel > 0
                        ? shapeInstance.StyleLabel
                        : shapeInstance.IfcTypeId * -1;

                    if (!materialsByStyleId.ContainsKey(styleId))
                    {
                        // if the style is not available we build one by ExpressType
                        var material2 = GetWpfMaterialByType(model, shapeInstance.IfcTypeId);
                        materialsByStyleId.Add(styleId, material2);

                        var mg = GetNewStyleMesh(styleId.ToString(), material2, parent.transform);
                        meshesByStyleId.Add(styleId, mg);
                    }

                    //GET THE ACTUAL GEOMETRY 
                    Mesh wpfMesh;
                    //see if we have already read it
                    if (UseMaps && repeatedShapeGeometries.TryGetValue(shapeInstance.ShapeGeometryLabel, out wpfMesh))
                    {
                        var go = new GameObject();
                        var renderer = go.AddComponent<MeshRenderer>();
                        renderer.materials = materialsByStyleId[styleId].Materials.ToArray();
                        var mesh = new Mesh();
                        var filter = go.AddComponent<MeshFilter>();
                        filter.mesh = mesh;
                        var mat1 = XbimMatrix3D.Multiply(shapeInstance.Transformation, modelTransform);
                        var mat = new Matrix4x4(
                            new Vector4((float)mat1.M11, (float)mat1.M12, (float)mat1.M13, (float)mat1.M14),
                            new Vector4((float)mat1.M21, (float)mat1.M22, (float)mat1.M23, (float)mat1.M24),
                            new Vector4((float)mat1.M31, (float)mat1.M32, (float)mat1.M33, (float)mat1.M34),
                            new Vector4((float)mat1.OffsetX, (float)mat1.OffsetY, (float)mat1.OffsetZ, (float)mat1.M44));
                        go.transform.position = mat.GetPosition();
                        go.transform.rotation = mat.rotation;
                        go.transform.localScale = mat.lossyScale;
                    }
                    else //we need to get the shape geometry
                    {
                        IXbimShapeGeometryData shapeGeom = geomReader.ShapeGeometry(shapeInstance.ShapeGeometryLabel);

                        if (UseMaps && shapeGeom.ReferenceCount > 1) //only store if we are going to use again
                        {
                            wpfMesh = new Mesh();
                            switch ((XbimGeometryType)shapeGeom.Format)
                            {
                                case XbimGeometryType.PolyhedronBinary:
                                    wpfMesh.Read(shapeGeom.ShapeData);
                                    break;
                                case XbimGeometryType.Polyhedron:
                                    wpfMesh.Read(((XbimShapeGeometry)shapeGeom).ShapeData);
                                    break;
                            }
                            repeatedShapeGeometries.Add(shapeInstance.ShapeGeometryLabel, wpfMesh);

                            var go = new GameObject();
                            var renderer = go.AddComponent<MeshRenderer>();
                            renderer.materials = materialsByStyleId[styleId].Materials.ToArray();
                            var filter = go.AddComponent<MeshFilter>();
                            filter.mesh = wpfMesh;
                            var mat1 = XbimMatrix3D.Multiply(shapeInstance.Transformation, modelTransform);
                            var mat = new Matrix4x4(
                                new Vector4((float)mat1.M11, (float)mat1.M12, (float)mat1.M13, (float)mat1.M14),
                                new Vector4((float)mat1.M21, (float)mat1.M22, (float)mat1.M23, (float)mat1.M24),
                                new Vector4((float)mat1.M31, (float)mat1.M32, (float)mat1.M33, (float)mat1.M34),
                                new Vector4((float)mat1.OffsetX, (float)mat1.OffsetY, (float)mat1.OffsetZ, (float)mat1.M44));
                            go.transform.position = mat.GetPosition();
                            go.transform.rotation = mat.rotation;
                            go.transform.localScale = mat.lossyScale;
                        }
                        else //it is a one off, merge it with shapes of same style
                        {
                            var targetMergeMeshByStyle = meshesByStyleId[styleId];

                            // replace target mesh beyond suggested size
                            // https://docs.microsoft.com/en-us/dotnet/framework/wpf/graphics-multimedia/maximize-wpf-3d-performance
                            // 
                            if (targetMergeMeshByStyle.PositionCount > 20000
                                ||
                                targetMergeMeshByStyle.TriangleIndexCount > 60000
                            )
                            {
                                targetMergeMeshByStyle.EndUpdate();
                                var replace = GetNewStyleMesh(styleId.ToString(), materialsByStyleId[styleId], parent.transform);
                                meshesByStyleId[styleId] = replace;
                                targetMergeMeshByStyle = replace;
                            }
                            // end replace

                            if (shapeGeom.Format != (byte)XbimGeometryType.PolyhedronBinary)
                                continue;
                            var transform = XbimMatrix3D.Multiply(shapeInstance.Transformation, modelTransform);
                            targetMergeMeshByStyle.Add(
                                shapeGeom.ShapeData,
                                shapeInstance.IfcTypeId,
                                shapeInstance.IfcProductLabel,
                                shapeInstance.InstanceLabel, transform,
                                (short)model.UserDefinedId);
                        }
                    }
                }

                foreach (var wpfMeshGeometry3D in meshesByStyleId.Values)
                {
                    wpfMeshGeometry3D.EndUpdate();
                }
            }
        }
        return parent;
    }

    public static void Show()
    {
        const string file = "C:\\Users\\dingbin\\Downloads\\XbimSamples-master\\HelloWall\\bin\\Debug\\net47\\HelloWallIfc4.ifc";

        using (var model = IfcStore.Open(file))
        {
            var project = model.Instances.FirstOrDefault<IIfcProject>();
            PrintHierarchy(project, 0);
        }
    }

    private static void PrintHierarchy(IIfcObjectDefinition o, int level)
    {
        Debug.Log(string.Format("{0}{1} [{2}]", GetIndent(level), o.Name, o.GetType().Name));

        //only spatial elements can contain building elements
        var spatialElement = o as IIfcSpatialStructureElement;
        if (spatialElement != null)
        {
            //using IfcRelContainedInSpatialElement to get contained elements
            var containedElements = spatialElement.ContainsElements.SelectMany(rel => rel.RelatedElements);
            foreach (var element in containedElements)
                Debug.Log(string.Format("{0}    ->{1} [{2}]", GetIndent(level), element.Name, element.GetType().Name));
        }

        //using IfcRelAggregares to get spatial decomposition of spatial structure elements
        foreach (var item in o.IsDecomposedBy.SelectMany(r => r.RelatedObjects))
            PrintHierarchy(item, level + 1);
    }

    private static string GetIndent(int level)
    {
        var indent = "";
        for (int i = 0; i < level; i++)
            indent += "  ";
        return indent;
    }
}
