using cn.cssoftstudio.gimParser;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Text;
using UFB;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Controller : MonoBehaviour
{
    public Button btnOpen;
    public Button btnExport;
    public Button btnAbout;
    public GimParser gimParser;
    public Transform target;
    public GameObject loading;
    public float zoomSpeed = 200;
    public GameObject dialogComplete;
    public GameObject dialogAbout;

    private Text propertyText;

    private bool CanExport
    {
        set
        {
            btnExport.interactable = value;
        }
    }

    private string savePath;

    private bool Loading
    {
        set
        {
            loading.SetActive(value);
        }
    }

    private bool DialogCompVisible
    {
        set
        {
            dialogComplete.SetActive(value);
        }
    }

    private bool DialogAboutVisible
    {
        set
        {
            dialogAbout.SetActive(value);
        }
    }

    public void HideDialogComp()
    {
        DialogCompVisible = false;
    }

    public void ShowDialogAbout()
    {
        DialogAboutVisible = false;
    }

    public void HideDialogAbout()
    {
        DialogAboutVisible = false;
    }

    public void ReportBug()
    {
        Application.OpenURL("https://gitee.com/cangyundashuju/gimviewer/issues");
    }

    public void OpenSaveDir()
    {
        if (savePath != null)
        {
            Process.Start("explorer.exe", $"/select,{savePath}");
        }
    }

    void Start()
    {
        btnOpen.onClick.AddListener(this.handleOpen);
        btnExport.onClick.AddListener(this.handleExport);
        btnAbout.onClick.AddListener(this.handleAbout);
        gimParser.onParseFinished.AddListener(this.handleParseFinished);
        CanExport = false;
        Loading = false;
        DialogCompVisible = false;

        EnsurePropertyPanel();

        int firstLaunch = PlayerPrefs.GetInt("firstLaunch", 1);
        if (firstLaunch == 1)
        {
            DialogAboutVisible = true;
            PlayerPrefs.SetInt("firstLaunch", 0);
        }
        else
        {
            DialogAboutVisible = false;
        }
    }

    private void EnsurePropertyPanel()
    {
        var canvas = GameObject.Find("Canvas");
        if (canvas == null)
        {
            return;
        }

        var panel = new GameObject("RuntimePropertiesPanel", typeof(Image));
        panel.transform.SetParent(canvas.transform, false);
        var panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(1, 0);
        panelRect.anchorMax = new Vector2(1, 1);
        panelRect.pivot = new Vector2(1, 1);
        panelRect.sizeDelta = new Vector2(360, -40);
        panelRect.anchoredPosition = new Vector2(-10, -10);
        panel.GetComponent<Image>().color = new Color(0, 0, 0, 0.45f);

        var textObj = new GameObject("RuntimePropertiesText", typeof(Text));
        textObj.transform.SetParent(panel.transform, false);
        propertyText = textObj.GetComponent<Text>();
        propertyText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        propertyText.fontSize = 14;
        propertyText.alignment = TextAnchor.UpperLeft;
        propertyText.horizontalOverflow = HorizontalWrapMode.Wrap;
        propertyText.verticalOverflow = VerticalWrapMode.Overflow;
        propertyText.color = Color.white;
        propertyText.text = "ģͿɲ鿴";

        var textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(10, 10);
        textRect.offsetMax = new Vector2(-10, -10);
    }

    private void handleAbout()
    {
        DialogAboutVisible = true;
    }

    private void handleParseFinished()
    {
        Loading = false;
        CanExport = true;
        Bounds bounds = new Bounds();
        var renderers = gimParser.Model.GetComponentsInChildren<Renderer>();
        foreach (var item in renderers)
        {
            bounds.Encapsulate(item.bounds);
        }
        target.position = bounds.center;

        var camPos = Camera.main.transform.localPosition;
        Camera.main.transform.localPosition = new Vector3(camPos.x, camPos.y, -Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z) * 1.2f);
        Camera.main.farClipPlane = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z) * 2;

    }
    private void handleExport()
    {
        }
    }
    private IEnumerator Export(string savePath)
    {
        AsciiFBXExporter.FBXExporter.ExportGameObjAtRuntime(gimParser.Model, savePath);
    }
    private async void handleOpen()
    {
        string gimFilePath = UniversalFileBrowser.SingleFileDialog("GIMļ", null, new Filter[] { new Filter("GIMļ", "gim") });
        gimParser.Clear();
    }
    // Update is called once per frame
    void Update()
            var angle = Camera.main.fieldOfView + Input.mouseScrollDelta.y * Time.deltaTime * zoomSpeed;
            Camera.main.fieldOfView = Mathf.Clamp(angle, 10, 80);

            if (Input.GetMouseButtonDown(0))
            {
                TryShowProperties();
            }
        }
    }

    private void TryShowProperties()
    {
        if (propertyText == null || Camera.main == null)
        {
            return;
        }

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (!Physics.Raycast(ray, out RaycastHit hitInfo))
        {
            return;
        }

        var loadItem = hitInfo.transform.GetComponent<GimLoadItem>();
        if (loadItem == null)
        {
            loadItem = hitInfo.transform.GetComponentInParent<GimLoadItem>();
        }

        if (loadItem == null || loadItem.Items == null || loadItem.Items.Count == 0)
        {
            propertyText.text = $": {hitInfo.transform.name}\nδ";
            return;
        }

        StringBuilder sb = new StringBuilder();
        sb.AppendLine($": {hitInfo.transform.name}");
        sb.AppendLine("------------------------------");
        foreach (var kv in loadItem.Items)
        {
            sb.AppendLine($"{kv.Key}: {kv.Value}");
        }
        propertyText.text = sb.ToString();
    }
	}

	private async void handleOpen()
	{
		string gimFilePath = UniversalFileBrowser.SingleFileDialog("打开GIM文件", null, new Filter[] { new Filter("GIM文件", "gim") });
        if (gimFilePath == null)
        {
            return;
        }
        gimParser.gimFilePath = gimFilePath;
		gimParser.Clear();

        Loading = true;
        await gimParser.ParseGim();
	}

	// Update is called once per frame
	void Update()
    {
        if (!EventSystem.current.IsPointerOverGameObject())
        {
			var angle = Camera.main.fieldOfView + Input.mouseScrollDelta.y * Time.deltaTime * zoomSpeed;
			Camera.main.fieldOfView = Mathf.Clamp(angle, 10, 80);
		}
	}
}
