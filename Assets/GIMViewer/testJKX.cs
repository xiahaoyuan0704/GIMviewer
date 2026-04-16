using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using static UniStorm.Example.GimUIController;

public class testJKX : MonoBehaviour
{
    //private string HNum;
    private Dictionary<string, List<string>> HNum = new Dictionary<string, List<string>>();
    private Dictionary<string, List<string>> HBody1 = new Dictionary<string, List<string>>();
    private Dictionary<string, List<string>> HLeg1 = new Dictionary<string, List<string>>();
    private Dictionary<string, List<string>> HSubLeg1 = new Dictionary<string, List<string>>();



    void Start()
    {
        /*        GameObject gameObject1 = new GameObject();
                // 两个坐标点
                Vector3 startPoint = new Vector3(52, -19, 400);
                Vector3 endPoint = new Vector3(19, -19, 400);

                // 计算钢管的长度和方向
                Vector3 direction = endPoint - startPoint;
                float length = direction.magnitude;

                // 创建钢管对象
                GameObject steelPipe = Instantiate(gameObject1);

                // 设置钢管的位置
                steelPipe.transform.position = startPoint;

                // 设置钢管的旋转
                steelPipe.transform.rotation = Quaternion.LookRotation(direction);

                // 设置钢管的缩放
                steelPipe.transform.localScale = new Vector3(1, length, 1);

                // 添加材质
                Renderer renderer = steelPipe.GetComponent<Renderer>();
                renderer.material.color = Color.red;*/




        /*ReadFile("C:\\Users\\杨星辰\\Desktop\\Game\\锚塔\\MOD\\锚塔.mod");
        GameObject gameObject1 = new GameObject();
        List<string> list = HBody1["P"];
        foreach (string s in list)
        {
            string[] strings = s.Split(',');
            GameObject gameObject2 = CreateConeSteelPipe(new Vector3(float.Parse(strings[2]) / 100, float.Parse(strings[3]) / 100, float.Parse(strings[4]) / 100), 102.0f, 5.0f, Color.gray, strings[1]);
            gameObject2.transform.parent = gameObject1.transform;
            gameObject1.transform.rotation = Quaternion.Euler(0, -90, 0);
        }*/









       // List<string> HNumList1 = new List<string>();
        /*CreateConeSteelPipe(new Vector3(52.000000f, -19.000000f, 400.000000f), 102.0f, 5.0f, Color.gray);
        CreateConeSteelPipe(new Vector3(19.000000f, -19.000000f, 400.000000f), 102.0f, 5.0f, Color.gray);*/
    }

    public int forList(List<string> input, Dictionary<string, List<string>> output,int index)
    {

        List<string> HBodyPList = new List<string>();
        List<string> HBodyRList = new List<string>();

        for (int i = index; i < input.Count; i++)
        {
            string[] split = input[i].Split(",");
            if (split[0].Equals("P"))
            {
                HBodyPList.Add(input[i]);
            }else if (split[0].Equals("R"))
            {
                HBodyRList.Add(input[i]);
            }
            else
            {
                output.Add("P", HBodyPList);
                output.Add("R", HBodyRList);
                return index;
            }
        }
        output.Add("P", HBodyPList);
        output.Add("R", HBodyRList);
        return index;
    }

    private GameObject CreateConeSteelPipe(Vector3 v3, float diameter, float thickness, Color color,string modelName)
    {
        // 创建一个空游戏对象作为锥钢管的父级节点
        GameObject coneSteelPipe = new GameObject(modelName);

        // 创建锥钢管模型
        GameObject cylinder = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        cylinder.transform.SetParent(coneSteelPipe.transform);

        // 设置锥钢管的位置和方向
        coneSteelPipe.transform.position = v3;
        //coneSteelPipe.transform.rotation = Quaternion.LookRotation(v3, Vector3.up);

        // 缩放锥钢管的尺寸
        cylinder.transform.localScale = new Vector3(diameter / 10.0f, thickness / 10.0f, diameter / 10.0f);

        // 设置锥钢管的材质
        Renderer rend = cylinder.GetComponent<Renderer>();
        rend.material.color = color;
        return coneSteelPipe;
    }
    // Update is called once per frame
    void Update()
    {
      //  Debug.DrawLine(Vector3.zero, Vector3.up, Color.red, 50);
    }
    public void ReadFile(string filePath)
    {
        List<string> list = new List<string>(); 
        // 创建StreamReader对象来读取文件
        using (StreamReader reader = new StreamReader(filePath))
        {
            string line;

            // 逐行读取文件，直到结束
            while ((line = reader.ReadLine()) != null)
            {
                // 处理每一行的内容
                list.Add(line);
            }
        }
        List<string> HNumList = new List<string>();

        for (int i = 1; i < list.Count; i++)
        {
            string[] split = list[i].Split(",");
            if (split[0].Equals("H"))
            {
                HNumList.Add(list[i]);
            }
            if (split[0].Equals("HBody1"))
            {
                HBody1.Add(list[i], null);
                int v = forList(list, HBody1, i + 1);
                i = v;
                continue;
            }
            if (split[0].Equals("HLeg1"))
            {
                HLeg1.Add(list[i], null);
                int v = forList(list, HLeg1, i + 1);
                i = v;
                continue;
            }
            if (split[0].Equals("HSubLeg1"))
            {
                HSubLeg1.Add(list[i], null);
                int v = forList(list, HSubLeg1, i + 1);
                i = v;
                continue;
            }
            
        }
        HNum.Add(list[0], HNumList);
    }
}
