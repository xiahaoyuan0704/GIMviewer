using cn.cssoftstudio.gimParser;
using System.Collections;
using System.Diagnostics;
using System.IO;
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
            /*if (value)
            {
                ShowLoading();
            } 
            else
            {
                HideLoading();
            }*/
        }
    }

    private bool DialogCompVisible
    {
        set {
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
			var angle = Camera.main.fieldOfView + Input.mouseScrollDelta.y * Time.deltaTime * zoomSpeed;
			Camera.main.fieldOfView = Mathf.Clamp(angle, 10, 80);
		}
	}
}
