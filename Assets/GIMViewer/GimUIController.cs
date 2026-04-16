using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniStorm.Utility;
using UnityEngine.UI;
using System;
using UniStorm.CharacterController;
using UFB;
using Battlehub.UIControls;
using System.Linq;
using Unity.VisualScripting;

namespace UniStorm.Example
{
    public class GimUIController : MonoBehaviour
    {
        public Slider sliderTime;
        public Dropdown dropdownWeather;
        public Text textTime;
        public Text textTemplture;
        public GameObject loadingObject;
        public GameObject stormUI;
        public FreeCameraController freeCameraController;
        public UniStormMouseLook mouseLook;
        public UniStormCharacterController characterController;
        public CreateModel createModel;
        public Text textGameMode;
        private string path;
        public GameObject Terrain;
        public TreeView TreeView;
        public static List<MyCustomData> treeItems = new List<MyCustomData>();
        private static Dictionary<string, string> weatherDic = new Dictionary<string, string>()
        {
            {"Clear", "晴天" },
            {"Mostly Clear", "大部分地区晴天" },
            {"Mostly Cloudy", "大部分地区多余" },
            {"Partly Cloudy", "局部多云" },
            {"Cloudy", "多云" },
            {"Blowing Leaves", "飘落叶" },
            {"Blowing Pine Needles", "吹松针" },
            {"Blowing Snow", "吹雪" },
            {"Blowing Pollen", "吹粉尘" },
            {"Foggy", "有雾的" },
            {"Overcast", "阴天" },
            {"Hail", "冰雹" },
            {"Heavy Rain", "暴雨" },
            {"Rain", "雨天" },
            {"Light Rain", "小雨" },
            {"Drizzle", "毛毛雨" },
            {"Heavy Snow", "暴雪" },
            {"Light Snow", "小雪" },
            {"Thunderstorm", "雷雨" },
            {"Thunder Snow", "雷声雪" },
            {"Lightning Bugs", "萤火虫" },
            {"Dust Storm", "沙尘暴" },
            {"Fire Rain", "下火雨" },
            {"Fire Storm", "火风暴" },
            {"Mostly Cloudy with Rain", "多云有雨" },
            {"Dry Thunder", "旱天雷" },
            {"Blue Auroras", "蓝色的极光" },
            {"Red Auroras", "红色的极光" },
            {"Mostly Cloudy Thunder", "多云雷雨" },
            {"Snow", "下雪天" }
        };
        public GameObject Conceal;
        public GameObject PropertiesItem;
        private bool loading;
        private GameObject items;

        public bool Loading
        {
            get
            {
                return loading;
            }
            set
            {
                loading = value;
                loadingObject.SetActive(loading);

            }
        }

        private bool paused = false;

        private string gameMode = "Fly"; // "Walk"

        public string GameMode
        {
            get
            {
                return gameMode;
            }
            set
            {
                gameMode = value;
                textGameMode.text = gameMode.Equals("Fly") ? "飞行模式" : "行走模式";
            }
        }

        private void Start()
        {
            Loading = false;
            stormUI.SetActive(false);
            sliderTime.onValueChanged.AddListener(v =>
            {
                CalculateTimeSlider();
            });
            StartCoroutine(WaitForInilialization());
            GameObject table = GameObject.Find("Canvas/PropertiesItems");
            items = table;
            characterController.gameObject.SetActive(false);
            freeCameraController.gameObject.SetActive(true);
        }

        IEnumerator WaitForInilialization()
        {
            yield return new WaitUntil(() => UniStormSystem.Instance.UniStormInitialized);
            CreateUniStormMenu();
        }

        public void SwitchGameMode()
        {
            if (GameMode == "Fly")
            {
                Ray ray = new Ray(Camera.main.transform.position, -Camera.main.transform.up);
                if (!Physics.Raycast(ray))
                {
                    Toast.Show("没有可以用来行走的地面，无法切换成行走模式");
                }
                else
                {
                    characterController.transform.position = freeCameraController.transform.position;
                    characterController.gameObject.SetActive(true);
                    paused = false;
                    freeCameraController.gameObject.SetActive(false);
                    GameMode = "Walk";
                    Toast.Show("当前是行走模式");
                }
            }
            else
            {
                characterController.gameObject.SetActive(false);
                freeCameraController.transform.position = characterController.transform.position;
                freeCameraController.transform.rotation = characterController.transform.rotation;
                freeCameraController.gameObject.SetActive(true);
                GameMode = "Fly";
                Toast.Show("当前是飞行模式");
            }
        }

        public void HandleOpen()
        {
            StartCoroutine(LoadScene());
        }
        public void IsConceal() {

            if (TreeView.gameObject.active) {
                TreeView.gameObject.SetActive(false);
                items.SetActive(false);
                Conceal.transform.GetComponentInChildren<Text>().text = "开启菜单属性";
            }
            else
            {
                TreeView.gameObject.SetActive(true);
                items.SetActive(true);
                Conceal.transform.GetComponentInChildren<Text>().text = "隐藏菜单属性";
            }

        }
        public void OnItemDataBinding(object sender, TreeViewItemDataBindingArgs e)
        {
            MyCustomData dataItem = e.Item as MyCustomData;
            if (dataItem != null)
            {
                //We display dataItem.name using UI.Text 
                Text text = e.ItemPresenter.GetComponentInChildren<Text>(true);
                text.text = dataItem.name;

                //Load icon from resources
                /*Image icon = e.ItemPresenter.GetComponentsInChildren<Image>()[4];
                icon.sprite = Resources.Load<Sprite>("cube");*/

                //And specify whether data item has children (to display expander arrow if needed)
                if (dataItem.name != "TreeView")
                {
                    e.HasChildren = dataItem.childs.Count > 0;
                }
            }
        }

        public void OnItemBeginDrag(object sender, ItemArgs e)
        {
            //Could be used to change cursor
        }

        public void OnItemDrop(object sender, ItemDropArgs e)
        {
            try
            {


            if (e.DropTarget == null)
            {
                return;
            }
           
            Transform dropT = ((GameObject)e.DropTarget).transform;
            //Set drag items as children of drop target
            if (e.Action == ItemDropAction.SetLastChild)
            {
                for (int i = 0; i < e.DragItems.Length; ++i)
                {
                    Transform dragT = ((GameObject)e.DragItems[i]).transform;
                    dragT.SetParent(dropT, true);
                    dragT.SetAsLastSibling();
                }
            }

            //Put drag items next to drop target
            else if (e.Action == ItemDropAction.SetNextSibling)
            {
                for (int i = e.DragItems.Length - 1; i >= 0; --i)
                {
                    Transform dragT = ((GameObject)e.DragItems[i]).transform;
                    int dropTIndex = dropT.GetSiblingIndex();
                    if (dragT.parent != dropT.parent)
                    {
                        dragT.SetParent(dropT.parent, true);
                        dragT.SetSiblingIndex(dropTIndex + 1);
                    }
                    else
                    {
                        int dragTIndex = dragT.GetSiblingIndex();
                        if (dropTIndex < dragTIndex)
                        {
                            dragT.SetSiblingIndex(dropTIndex + 1);
                        }
                        else
                        {
                            dragT.SetSiblingIndex(dropTIndex);
                        }
                    }
                }
            }

            //Put drag items before drop target
            else if (e.Action == ItemDropAction.SetPrevSibling)
            {
                for (int i = 0; i < e.DragItems.Length; ++i)
                {
                    Transform dragT = ((GameObject)e.DragItems[i]).transform;
                    if (dragT.parent != dropT.parent)
                    {
                        dragT.SetParent(dropT.parent, true);
                    }

                    int dropTIndex = dropT.GetSiblingIndex();
                    int dragTIndex = dragT.GetSiblingIndex();
                    if (dropTIndex > dragTIndex)
                    {
                        dragT.SetSiblingIndex(dropTIndex - 1);
                    }
                    else
                    {
                        dragT.SetSiblingIndex(dropTIndex);
                    }
                }
                }
            }
            catch (Exception ex) { }
        }

        public void OnItemEndDrag(object sender, ItemArgs e)
        {
        }
        public void OnItemExpanding(object sender, ItemExpandingArgs e)
        {
            MyCustomData dataItem = (MyCustomData)e.Item;
            if (dataItem.childs.Count > 0)
            {
                //Populate children collection
                e.Children = dataItem.childs;
            }
        }

        public void OnSelectionChanged(object sender, SelectionChangedArgs e)
        {
            try { 

            MyCustomData dataNew = (MyCustomData)e.NewItem;
            MyCustomData dataOld = (MyCustomData)e.NewItem;

            GameObject row;
            GameObject table = GameObject.Find("Canvas/PropertiesItems/Scroll View/Viewport/Content");
            for (int i = 1; i < table.transform.childCount; i++)
            {
                Transform transform;
                transform = table.transform.GetChild(i);
                GameObject.Destroy(transform.gameObject);
            }
            if (null != dataOld)
            {

            }

            GameObject value = dataNew.proBuilderMesh;
            if (null == value.GetComponent<GimLoadItem>())
            {
                return;
            }
            GimLoadItem gimLoadItem = value.GetComponent<GimLoadItem>();

            Dictionary<string, string> items1 = gimLoadItem.Items;
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





#if UNITY_EDITOR
            //Do something on selection changed (just syncronized with editor's hierarchy for demo purposes)
            UnityEditor.Selection.objects = e.NewItems.OfType<GameObject>().ToArray();
#endif
        }catch (Exception ex) { }
        }

        public void OnItemsRemoved(object sender, ItemsRemovedArgs e)
        {
            //Destroy removed dataitems
            for (int i = 0; i < e.Items.Length; ++i)
            {
                GameObject go = (GameObject)e.Items[i];
                if (go != null)
                {
                    Destroy(go);
                }
            }
        }
        public void OnItemBeginDrop(object sender, ItemDropCancelArgs e)
        {
            //object dropTarget = e.DropTarget;
            //if(e.Action == ItemDropAction.SetNextSibling || e.Action == ItemDropAction.SetPrevSibling)
            //{
            //    e.Cancel = true;
            //}

        }
        private IEnumerator LoadScene()
        {
            //Loading = true;

            //Terrain.active = false;
            createModel.Clear();
            path = UniversalFileBrowser.SingleFileDialog("加载GIM文件", null, new Filter[] {new Filter("GIM文件", "gim")});
            if (path != null)
            {
                yield return createModel.LoadModel(path);
            }
            TreeView.ItemDataBinding += OnItemDataBinding;
            TreeView.SelectionChanged += OnSelectionChanged;
            TreeView.ItemsRemoved += OnItemsRemoved;
            TreeView.ItemExpanding += OnItemExpanding;
            TreeView.ItemBeginDrag += OnItemBeginDrag;

            TreeView.ItemDrop += OnItemDrop;
            TreeView.ItemBeginDrop += OnItemBeginDrop;
            TreeView.ItemEndDrag += OnItemEndDrag;
            TreeView.Items = treeItems;
            Toast.Show("加载完成");
            //Loading = false;
            //freeCameraController.enabled = true;
        }

        public void UpdateTime()
        {
            //textTime.text = UniStormSystem.Instance.Hour.ToString() + ":" + UniStormSystem.Instance.Minute.ToString("00");
        }

        public void UpdateTemperature()
        {
            //textTemplture.text = UniStormSystem.Instance.Temperature.ToString() + "°";
        }

        public void UpdateTimeSlider()
        {
            sliderTime.value = UniStormSystem.Instance.m_TimeFloat;
        }

        private void Update()
        {
            if (gameMode == "Walk")
            {
                if (Input.GetKeyDown(KeyCode.Escape))
                {
                    paused = !paused;
                }

                if (paused)
                {
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                    mouseLook.enabled = false;
                }
                else
                {
                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false;
                    mouseLook.enabled = true;
                }
            }
        }

        public void QuitButton()
        {
            Application.Quit();
        }

        public void ChangeWeatherUI()
        {
            UniStormManager.Instance.ChangeWeatherWithTransition(UniStormSystem.Instance.AllWeatherTypes[dropdownWeather.value]);
        }

        public void CalculateTimeSlider()
        {
            UniStormSystem.Instance.m_TimeFloat = sliderTime.value;
        }

        void CreateUniStormMenu()
        {
            List<string> m_DropOptions = new List<string> { };

            for (int i = 0; i < UniStormSystem.Instance.AllWeatherTypes.Count; i++)
            {
                if (UniStormSystem.Instance.AllWeatherTypes[i] != null)
                {
                    Debug.Log(UniStormSystem.Instance.AllWeatherTypes[i].WeatherTypeName);
                    m_DropOptions.Add(weatherDic[UniStormSystem.Instance.AllWeatherTypes[i].WeatherTypeName]);
                }
            }
            dropdownWeather.AddOptions(m_DropOptions);
            dropdownWeather.itemText.fontSize = 8;
            dropdownWeather.itemText.fontStyle = FontStyle.Bold;
            dropdownWeather.value = UniStormSystem.Instance.AllWeatherTypes.IndexOf(UniStormSystem.Instance.CurrentWeatherType);
            dropdownWeather.onValueChanged.AddListener(delegate { ChangeWeatherUI(); });

            sliderTime.value = UniStormSystem.Instance.m_TimeFloat;

            dropdownWeather.value = UniStormSystem.Instance.AllWeatherTypes.IndexOf(UniStormSystem.Instance.CurrentWeatherType);

            if (FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                GameObject m_EventSystem = new GameObject();
                m_EventSystem.name = "EventSystem";
                m_EventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
                m_EventSystem.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }

            UniStormSystem.Instance.OnHourChangeEvent.AddListener(delegate { UpdateTimeSlider(); });
            UniStormSystem.Instance.OnHourChangeEvent.AddListener(delegate { UpdateTemperature(); });
            UpdateTemperature();
            InvokeRepeating("UpdateTime", 0.05f, 0.25f);

            stormUI.SetActive(true);
        }
       public class MyCustomData
        {
            public List<MyCustomData> childs;
            public string name;
            public object tag;
            public GameObject proBuilderMesh;

            public MyCustomData()
            {
            }

            public MyCustomData(string na, GameObject game)
            {
                childs = new List<MyCustomData>();
                name = na;
                proBuilderMesh = game;
            }
        }
    }
}
