using System.Collections.Generic;
using UnityEngine;

public class GLSquare : MonoBehaviour
{
	//网格材质
	public Material LineMat;//随意指定一个材质就行
							//网格颜色
	public Color MeshColor;
	//网格线坐标存储
	List<Vector3[]> m_linePoints = new List<Vector3[]>();

	void Start()
	{
		initPoints();
		//修改网格材质颜色
		LineMat.SetColor("_Color", MeshColor);
	}

	//一个10*10的，单位大小为1网格
	void initPoints()
	{
		for (int i = -5; i <= 5; i++)
		{
			Vector3[] rows = new Vector3[2];
			rows[0] = new Vector3(-5, 0, i);
			rows[1] = new Vector3(5, 0, i);
			m_linePoints.Add(rows);
			Vector3[] clos = new Vector3[2];
			clos[0] = new Vector3(i, 0, 5);
			clos[1] = new Vector3(i, 0, -5);
			m_linePoints.Add(clos);
		}
	}

	/// <summary>
	/// 照相机完成场景渲染后调用
	/// </summary>
	void OnPostRender()
	{
		//线条材质
		LineMat.SetPass(0);
		GL.PushMatrix();
		//线条颜色,当前材质下，该方式修改颜色无效，详情可以看官方文档
		//GL.Color(MeshColor);
		//绘制线条
		GL.Begin(GL.LINES);

		//所有线条 (两点一条线)
		for (int i = 0; i < m_linePoints.Count; i++)
		{
			GL.Vertex(m_linePoints[i][0]);
			GL.Vertex(m_linePoints[i][1]);
		}
		GL.End();
		GL.PopMatrix();
	}
}
