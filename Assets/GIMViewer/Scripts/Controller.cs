using cn.cssoftstudio.gimParser;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Text;
using UFB;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System;

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
    private GameObject propertyPanel;
    private GameObject settingsPanel;

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
        btnAbout.onClick.AddListener(this.TogglePropertyPanel);
        gimParser.onParseFinished.AddListener(this.handleParseFinished);
        SetPropertyButtonLabel("属性");
        CanExport = false;
        Loading = false;
        DialogCompVisible = false;
        DialogAboutVisible = false;

        BuildPropertyPanel();
        BuildSettingsPanel();
        SetPropertyPanelVisible(false);
    }

    private void SetPropertyButtonLabel(string text)
    {
        if (btnAbout == null)
        {
            return;
        }
        var label = btnAbout.GetComponentInChildren<Text>();
        if (label != null)
        {
            label.text = text;
        }
    }

    private void TogglePropertyPanel()
    {
        SetPropertyPanelVisible(propertyPanel == null || !propertyPanel.activeSelf);
    }

    private void SetPropertyPanelVisible(bool visible)
    {
        if (propertyPanel != null)
        {
            propertyPanel.SetActive(visible);
        }
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

        StartCoroutine(PlayModelOpenAnimation());

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
        propertyPanel = panel;
        panel.transform.SetParent(canvas.transform, false);
        var panelRt = panel.GetComponent<RectTransform>();
        panelRt.anchorMin = new Vector2(1, 0);
        panelRt.anchorMax = new Vector2(1, 1);
        panelRt.pivot = new Vector2(1, 0.5f);
        panelRt.sizeDelta = new Vector2(420, -20);
        panelRt.anchoredPosition = new Vector2(-10, 0);
        var panelImage = panel.GetComponent<Image>();
        panelImage.color = new Color(0.09f, 0.11f, 0.14f, 0.92f);
        panel.AddComponent<PropertyPanelDrag>();
        panel.AddComponent<PropertyPanelResize>();

        var header = new GameObject("Header", typeof(RectTransform), typeof(Image));
        header.transform.SetParent(panel.transform, false);
        var headerRt = header.GetComponent<RectTransform>();
        headerRt.anchorMin = new Vector2(0, 1);
        headerRt.anchorMax = new Vector2(1, 1);
        headerRt.pivot = new Vector2(0.5f, 1);
        headerRt.sizeDelta = new Vector2(0, 40);
        var headerImage = header.GetComponent<Image>();
        headerImage.color = new Color(0.15f, 0.19f, 0.24f, 0.98f);

        var headerTextGo = new GameObject("HeaderText", typeof(RectTransform), typeof(Text));
        headerTextGo.transform.SetParent(header.transform, false);
        var headerTextRt = headerTextGo.GetComponent<RectTransform>();
        headerTextRt.anchorMin = new Vector2(0, 0);
        headerTextRt.anchorMax = new Vector2(1, 1);
        headerTextRt.offsetMin = new Vector2(10, 4);
        headerTextRt.offsetMax = new Vector2(-10, -4);
        var headerText = headerTextGo.GetComponent<Text>();
        headerText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        headerText.fontSize = 14;
        headerText.alignment = TextAnchor.MiddleLeft;
        headerText.color = Color.white;
        headerText.text = "属性面板（拖动标题移动，右下角缩放）";

        var body = new GameObject("Body", typeof(RectTransform));
        body.transform.SetParent(panel.transform, false);
        var bodyRt = body.GetComponent<RectTransform>();
        bodyRt.anchorMin = new Vector2(0, 0);
        bodyRt.anchorMax = new Vector2(1, 1);
        bodyRt.offsetMin = new Vector2(0, 0);
        bodyRt.offsetMax = new Vector2(0, -46);

        var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
        viewport.transform.SetParent(body.transform, false);
        var viewportRt = viewport.GetComponent<RectTransform>();
        viewportRt.anchorMin = new Vector2(0, 0);
        viewportRt.anchorMax = new Vector2(1, 1);
        viewportRt.offsetMin = new Vector2(10, 10);
        viewportRt.offsetMax = new Vector2(-28, -48);
        viewport.GetComponent<Image>().color = new Color(0.05f, 0.07f, 0.09f, 0.95f);
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

        var scrollRect = body.AddComponent<ScrollRect>();
        scrollRect.viewport = viewportRt;
        scrollRect.content = contentRt;
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        scrollRect.scrollSensitivity = 28;
        scrollRect.verticalNormalizedPosition = 1f;
        propertyScrollRect = scrollRect;

        var scrollbarObj = new GameObject("Scrollbar", typeof(RectTransform), typeof(Image), typeof(Scrollbar));
        scrollbarObj.transform.SetParent(body.transform, false);
        var scrollbarRt = scrollbarObj.GetComponent<RectTransform>();
        scrollbarRt.anchorMin = new Vector2(1, 0);
        scrollbarRt.anchorMax = new Vector2(1, 1);
        scrollbarRt.pivot = new Vector2(1, 1);
        scrollbarRt.sizeDelta = new Vector2(14, 0);
        scrollbarRt.offsetMin = new Vector2(-14, 10);
        scrollbarRt.offsetMax = new Vector2(-4, -48);
        scrollbarObj.GetComponent<Image>().color = new Color(0.2f, 0.24f, 0.30f, 1f);

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
        handleImg.color = new Color(0.43f, 0.69f, 1f, 0.95f);

        var scrollbar = scrollbarObj.GetComponent<Scrollbar>();
        scrollbar.direction = Scrollbar.Direction.BottomToTop;
        scrollbar.targetGraphic = handleImg;
        scrollbar.handleRect = handleRt;
        scrollRect.verticalScrollbar = scrollbar;
        scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;

        header.transform.SetAsLastSibling();
    }

    private void BuildSettingsPanel()
    {
        if (gimParser == null)
        {
            return;
        }
        var canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            return;
        }

        settingsPanel = new GameObject("SettingsPanel", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        settingsPanel.transform.SetParent(canvas.transform, false);
        var rt = settingsPanel.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(0, 1);
        rt.pivot = new Vector2(0, 1);
        rt.anchoredPosition = new Vector2(8, -130);
        rt.sizeDelta = new Vector2(260, 0);
        settingsPanel.GetComponent<Image>().color = new Color(0.09f, 0.11f, 0.14f, 0.88f);

        var layout = settingsPanel.GetComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(10, 10, 10, 10);
        layout.spacing = 8;
        layout.childForceExpandHeight = false;
        layout.childControlHeight = true;
        settingsPanel.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        CreatePanelTitle(settingsPanel.transform, "性能设置");
        CreateToggleRow(settingsPanel.transform, "轻量拾取碰撞体", gimParser.useLightweightPickCollider, v => gimParser.useLightweightPickCollider = v);
        CreateToggleRow(settingsPanel.transform, "静态合批优化", gimParser.enableStaticBatchingOptimization, v => gimParser.enableStaticBatchingOptimization = v);
        CreateToggleRow(settingsPanel.transform, "关闭模型阴影", gimParser.disableRendererShadows, v => gimParser.disableRendererShadows = v);
    }

    private void CreatePanelTitle(Transform parent, string title)
    {
        var go = new GameObject("Title", typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);
        var text = go.GetComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        text.fontSize = 16;
        text.color = new Color(0.80f, 0.89f, 1f, 1f);
        text.alignment = TextAnchor.MiddleLeft;
        text.text = title;
    }

    private void CreateToggleRow(Transform parent, string labelText, bool initial, Action<bool> onValueChanged)
    {
        var row = new GameObject(labelText, typeof(RectTransform), typeof(Toggle), typeof(Image));
        row.transform.SetParent(parent, false);
        row.GetComponent<Image>().color = new Color(0.13f, 0.16f, 0.20f, 0.95f);
        var rowRt = row.GetComponent<RectTransform>();
        rowRt.sizeDelta = new Vector2(0, 30);

        var toggle = row.GetComponent<Toggle>();

        var checkmarkBg = new GameObject("CheckBg", typeof(RectTransform), typeof(Image));
        checkmarkBg.transform.SetParent(row.transform, false);
        var checkBgRt = checkmarkBg.GetComponent<RectTransform>();
        checkBgRt.anchorMin = new Vector2(0, 0.5f);
        checkBgRt.anchorMax = new Vector2(0, 0.5f);
        checkBgRt.anchoredPosition = new Vector2(12, 0);
        checkBgRt.sizeDelta = new Vector2(18, 18);
        checkmarkBg.GetComponent<Image>().color = new Color(0.22f, 0.27f, 0.34f, 1f);

        var checkmark = new GameObject("Checkmark", typeof(RectTransform), typeof(Image));
        checkmark.transform.SetParent(checkmarkBg.transform, false);
        var checkmarkRt = checkmark.GetComponent<RectTransform>();
        checkmarkRt.anchorMin = Vector2.zero;
        checkmarkRt.anchorMax = Vector2.one;
        checkmarkRt.offsetMin = new Vector2(3, 3);
        checkmarkRt.offsetMax = new Vector2(-3, -3);
        checkmark.GetComponent<Image>().color = new Color(0.43f, 0.69f, 1f, 1f);

        var label = new GameObject("Label", typeof(RectTransform), typeof(Text));
        label.transform.SetParent(row.transform, false);
        var labelRt = label.GetComponent<RectTransform>();
        labelRt.anchorMin = new Vector2(0, 0);
        labelRt.anchorMax = new Vector2(1, 1);
        labelRt.offsetMin = new Vector2(38, 0);
        labelRt.offsetMax = new Vector2(-8, 0);
        var labelTextComp = label.GetComponent<Text>();
        labelTextComp.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        labelTextComp.fontSize = 14;
        labelTextComp.alignment = TextAnchor.MiddleLeft;
        labelTextComp.color = Color.white;
        labelTextComp.text = labelText;

        toggle.targetGraphic = row.GetComponent<Image>();
        toggle.graphic = checkmark.GetComponent<Image>();
        toggle.isOn = initial;
        toggle.onValueChanged.AddListener(v => onValueChanged(v));
    }

    private IEnumerator PlayModelOpenAnimation()
    {
        if (gimParser == null || gimParser.Model == null || Camera.main == null)
        {
            yield break;
        }

        var model = gimParser.Model.transform;
        var startScale = model.localScale * 0.96f;
        var endScale = model.localScale;
        model.localScale = startScale;

        var cam = Camera.main;
        var targetFov = cam.fieldOfView;
        var startFov = Mathf.Min(80f, targetFov + 6f);
        cam.fieldOfView = startFov;

        var duration = 0.28f;
        var elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            var t = Mathf.Clamp01(elapsed / duration);
            var eased = 1f - Mathf.Pow(1f - t, 3f);
            model.localScale = Vector3.LerpUnclamped(startScale, endScale, eased);
            cam.fieldOfView = Mathf.LerpUnclamped(startFov, targetFov, eased);
            yield return null;
        }
        model.localScale = endScale;
        cam.fieldOfView = targetFov;
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
