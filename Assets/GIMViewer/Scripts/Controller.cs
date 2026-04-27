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
    private ScrollRect propertyScrollRect;

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

        BuildPropertyPanel();
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

        if (propertyText != null)
        {
            propertyText.text = "点击模型部件显示属性";
            ResetPropertyScroll();
        }

        Toast.Show("加载完成");
    }

    private void handleExport()
    {
        string path = UniversalFileBrowser.SaveDialog("导出为FBX", null, gimParser.Model.name, new Filter[] { new Filter("FBX", "fbx") });
        if (path != null)
        {
            StartCoroutine(Export(path));
        }
    }

    private IEnumerator Export(string savePath)
    {
        this.savePath = savePath;
        Loading = true;
        yield return new WaitForSeconds(1);
        AsciiFBXExporter.FBXExporter.ExportGameObjAtRuntime(gimParser.Model, savePath);
        Loading = false;
        DialogCompVisible = true;
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
            if (Input.GetMouseButtonDown(0))
            {
                HandlePickProperty();
            }
            var angle = Camera.main.fieldOfView + Input.mouseScrollDelta.y * Time.deltaTime * zoomSpeed;
            Camera.main.fieldOfView = Mathf.Clamp(angle, 10, 80);
        }
    }

    private void BuildPropertyPanel()
    {
        var canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            return;
        }

        var panel = new GameObject("PropertyPanel", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(canvas.transform, false);
        var panelRt = panel.GetComponent<RectTransform>();
        panelRt.anchorMin = new Vector2(1, 0);
        panelRt.anchorMax = new Vector2(1, 1);
        panelRt.pivot = new Vector2(1, 0.5f);
        panelRt.sizeDelta = new Vector2(420, -20);
        panelRt.anchoredPosition = new Vector2(-10, 0);
        var panelImage = panel.GetComponent<Image>();
        panelImage.color = new Color(0, 0, 0, 0.45f);
        panel.AddComponent<PropertyPanelDrag>();

        var header = new GameObject("Header", typeof(RectTransform), typeof(Image));
        header.transform.SetParent(panel.transform, false);
        var headerRt = header.GetComponent<RectTransform>();
        headerRt.anchorMin = new Vector2(0, 1);
        headerRt.anchorMax = new Vector2(1, 1);
        headerRt.pivot = new Vector2(0.5f, 1);
        headerRt.sizeDelta = new Vector2(0, 32);
        var headerImage = header.GetComponent<Image>();
        headerImage.color = new Color(1, 1, 1, 0.12f);

        var headerTextGo = new GameObject("HeaderText", typeof(RectTransform), typeof(Text));
        headerTextGo.transform.SetParent(header.transform, false);
        var headerTextRt = headerTextGo.GetComponent<RectTransform>();
        headerTextRt.anchorMin = new Vector2(0, 0);
        headerTextRt.anchorMax = new Vector2(1, 1);
        headerTextRt.offsetMin = new Vector2(10, 0);
        headerTextRt.offsetMax = new Vector2(-10, 0);
        var headerText = headerTextGo.GetComponent<Text>();
        headerText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        headerText.fontSize = 14;
        headerText.alignment = TextAnchor.MiddleLeft;
        headerText.color = Color.white;
        headerText.text = "属性面板（可拖动，可上下滚动）";

        var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
        viewport.transform.SetParent(panel.transform, false);
        var viewportRt = viewport.GetComponent<RectTransform>();
        viewportRt.anchorMin = new Vector2(0, 0);
        viewportRt.anchorMax = new Vector2(1, 1);
        viewportRt.offsetMin = new Vector2(10, 10);
        viewportRt.offsetMax = new Vector2(-28, -38);
        viewport.GetComponent<Image>().color = new Color(0, 0, 0, 0.2f);
        viewport.GetComponent<Mask>().showMaskGraphic = false;

        var content = new GameObject("PropertyText", typeof(RectTransform), typeof(Text), typeof(ContentSizeFitter));
        content.transform.SetParent(viewport.transform, false);
        var contentRt = content.GetComponent<RectTransform>();
        contentRt.anchorMin = new Vector2(0, 1);
        contentRt.anchorMax = new Vector2(1, 1);
        contentRt.pivot = new Vector2(0.5f, 1);
        contentRt.anchoredPosition = Vector2.zero;
        contentRt.sizeDelta = Vector2.zero;

        propertyText = content.GetComponent<Text>();
        propertyText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        propertyText.fontSize = 16;
        propertyText.alignment = TextAnchor.UpperLeft;
        propertyText.color = Color.white;
        propertyText.horizontalOverflow = HorizontalWrapMode.Wrap;
        propertyText.verticalOverflow = VerticalWrapMode.Truncate;
        propertyText.text = "点击模型部件显示属性";

        var fitter = content.GetComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var scrollRect = panel.AddComponent<ScrollRect>();
        scrollRect.viewport = viewportRt;
        scrollRect.content = contentRt;
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        scrollRect.scrollSensitivity = 28;
        scrollRect.verticalNormalizedPosition = 1f;
        propertyScrollRect = scrollRect;

        var scrollbarObj = new GameObject("Scrollbar", typeof(RectTransform), typeof(Image), typeof(Scrollbar));
        scrollbarObj.transform.SetParent(panel.transform, false);
        var scrollbarRt = scrollbarObj.GetComponent<RectTransform>();
        scrollbarRt.anchorMin = new Vector2(1, 0);
        scrollbarRt.anchorMax = new Vector2(1, 1);
        scrollbarRt.pivot = new Vector2(1, 1);
        scrollbarRt.sizeDelta = new Vector2(14, 0);
        scrollbarRt.offsetMin = new Vector2(-14, 10);
        scrollbarRt.offsetMax = new Vector2(-4, -38);
        scrollbarObj.GetComponent<Image>().color = new Color(1, 1, 1, 0.18f);

        var slidingArea = new GameObject("SlidingArea", typeof(RectTransform));
        slidingArea.transform.SetParent(scrollbarObj.transform, false);
        var slidingRt = slidingArea.GetComponent<RectTransform>();
        slidingRt.anchorMin = Vector2.zero;
        slidingRt.anchorMax = Vector2.one;
        slidingRt.offsetMin = new Vector2(2, 2);
        slidingRt.offsetMax = new Vector2(-2, -2);

        var handleObj = new GameObject("Handle", typeof(RectTransform), typeof(Image));
        handleObj.transform.SetParent(slidingArea.transform, false);
        var handleRt = handleObj.GetComponent<RectTransform>();
        handleRt.anchorMin = Vector2.zero;
        handleRt.anchorMax = Vector2.one;
        handleRt.offsetMin = Vector2.zero;
        handleRt.offsetMax = Vector2.zero;
        var handleImg = handleObj.GetComponent<Image>();
        handleImg.color = new Color(1, 1, 1, 0.65f);

        var scrollbar = scrollbarObj.GetComponent<Scrollbar>();
        scrollbar.direction = Scrollbar.Direction.BottomToTop;
        scrollbar.targetGraphic = handleImg;
        scrollbar.handleRect = handleRt;
        scrollRect.verticalScrollbar = scrollbar;
        scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;
    }

    private void HandlePickProperty()
    {
        if (propertyText == null || gimParser == null || gimParser.Model == null)
        {
            return;
        }

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (!Physics.Raycast(ray, out var hitInfo, 10000f))
        {
            return;
        }

        if (gimParser.TryGetProperties(hitInfo.transform, out var properties))
        {
            var sb = new StringBuilder();
            sb.AppendLine($"对象：{hitInfo.transform.name}");
            foreach (var item in properties)
            {
                sb.AppendLine($"{item.Key}: {item.Value}");
            }
            propertyText.text = sb.ToString();
            ResetPropertyScroll();
        }
        else
        {
            propertyText.text = $"对象：{hitInfo.transform.name}\n未找到可显示属性";
            ResetPropertyScroll();
        }
    }

    private void ResetPropertyScroll()
    {
        if (propertyScrollRect == null)
        {
            return;
        }
        Canvas.ForceUpdateCanvases();
        propertyScrollRect.verticalNormalizedPosition = 1f;
    }
}
