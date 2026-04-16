using UnityEngine;
using UnityEditor.AssetImporters;
using System.IO;

[ScriptedImporter(1, "SampleScene")]
public class CubeImporter : ScriptedImporter
{
    public float m_Scale = 1;

    public override void OnImportAsset(AssetImportContext ctx)
    {
        var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        var position = JsonUtility.FromJson<Vector3>(File.ReadAllText(ctx.assetPath));

        cube.transform.position = position;
        cube.transform.localScale = new Vector3(m_Scale, m_Scale, m_Scale);

        /*// 'cube' 是一个游戏对象，会自动转换为预制件
        //（只有 'Main Asset' 才能成为预制件。）
        ctx.AddObjectToAsset("main obj", cube);
        ctx.SetMainObject(cube);

        var material = new Material(Shader.Find("Standard"));
        material.color = Color.red;

        // 资源必须为资源分配一个唯一标识符字符串，且在所有导入之间保持一致
        ctx.AddObjectToAsset("my Material", material);

        // 必须销毁未作为导入输出传入上下文的资源
        var tempMesh = new Mesh();
        DestroyImmediate(tempMesh);*/
    }
}