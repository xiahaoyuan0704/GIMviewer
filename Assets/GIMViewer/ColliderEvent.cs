using HighlightPlus;
using System.Collections;
using System.Collections.Generic;
using UniStorm.Example;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using static Microsoft.Isam.Esent.Interop.EnumeratedColumn;

public class ColliderEvent : MonoBehaviour
{

    public GameObject PropertiesItem;
    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hitInfo;
            if (Physics.Raycast(ray, out hitInfo))
            {
/*
                HighlightEffect highlightEffect = hitInfo.transform.AddComponent<HighlightEffect>();

                highlightEffect.highlighted = true;
                HighlightTrigger highlightTrigger = hitInfo.transform.AddComponent<HighlightTrigger>();
                highlightTrigger.triggerMode = TriggerMode.RaycastOnThisObjectAndChildren;

*/


                if (null == hitInfo.transform.GetComponent<GimLoadItem>())
                {
                    return;
                }
                GameObject row;
                GameObject table = GameObject.Find("Canvas/PropertiesItems/Scroll View/Viewport/Content");
                for (int i = 1; i < table.transform.childCount; i++)
                {
                    Transform transform;
                    transform = table.transform.GetChild(i);
                    GameObject.Destroy(transform.gameObject);
                }
                Dictionary<string, string> items1 = hitInfo.transform.GetComponent<GimLoadItem>().Items;
                if (items1 != null || items1.Count != 0)
                {
                    //在Table下创建新的预设实例
                    //table = GameObject.Find("Canvas/GameObject/Scroll View/Viewport/Content");
                    /* for (int i = 0; i < items1.Count; i++)
                     {*/
                    foreach (KeyValuePair<string, string> kvp in items1)
                    {
                        string key = kvp.Key;
                        string value1 = kvp.Value;
                        row = GameObject.Instantiate(PropertiesItem, table.transform.position, table.transform.rotation) as GameObject;
                        row.name = "row";
                        row.transform.SetParent(table.transform);
                        row.transform.localScale = Vector3.one;//设置缩放比例1,1,1，不然默认的比例非常大

                        //设置预设实例中的各个子物体的文本内容
                        row.transform.Find("key").GetComponent<Text>().text = key;
                        row.transform.Find("value").GetComponent<Text>().text = value1;



                    }

                }
            }
        }
    }
}