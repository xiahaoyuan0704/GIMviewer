using System;
using System.Runtime.InteropServices;
using UnityEngine;
using Xbim.Common.Geometry;
using Xbim.Geometry.Engine.Interop;
using Xbim.Ifc4.Interfaces;
using Xbim.IO.Memory;


public class XbimDemo : MonoBehaviour
{
    [DllImport("Xbim.Geometry.Engine", CharSet = CharSet.Unicode)]
    public static extern IXbimSolid CreateSolid(IIfcSphere ifcSolid, Microsoft.Extensions.Logging.ILogger logger = null);

    [DllImport("Xbim.Geometry.Engine", CharSet = CharSet.Unicode)]
    public static extern IXbimSolid CreateSolid(IIfcRightCircularCylinder ifcSolid, Microsoft.Extensions.Logging.ILogger logger = null);

    [DllImport("Xbim.Geometry.Engine", EntryPoint = "?Load@@YANXZ")]
    public static extern void Load();

    static private IXbimGeometryEngine geomEngine;

    private void Awake()
    {
        //geomEngine = new XbimGeometryEngine();
    }

    // Start is called before the first frame update
    void Start()
    {
        /*const string fileName = "C:\\Users\\dingbin\\Downloads\\ÍÁ½¨×ÜÍ¼.xBIM";

        new Parser().Parse(fileName);*/
        //BooleanIntersectSolidTest();
        //Load();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void BooleanIntersectSolidTest()
    {
        using (var m = new MemoryModel(new Xbim.Ifc4.EntityFactoryIfc4()))
        {
            using (var txn = m.BeginTransaction(""))
            {
                var cylinder = IfcModelBuilder.MakeRightCircularCylinder(m, 10, 20);
                var sphere = IfcModelBuilder.MakeSphere(m, 15);
                var a = CreateSolid(sphere, null);
                var b = CreateSolid(cylinder, null);
                var solidSet = a.Intersection(b, m.ModelFactors.PrecisionBoolean);
            }
        }
    }
}
