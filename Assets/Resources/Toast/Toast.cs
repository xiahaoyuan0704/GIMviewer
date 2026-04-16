using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using UnityEngine.Events;

public class Toast : MonoBehaviour
{
    public Image bg;
    public Text text;

    private static Toast inst;
    private static Color bgColor = new Color(82, 82, 82, 0);
    private static Color textColor = new Color(1, 1, 1, 1);
    private Coroutine coroutine;
    private static UnityAction finishedCallback;

    public static void Show(string text, float duration = 3f, UnityAction callback = null)
    {
        if (callback != null)
        {
            finishedCallback = callback;
        }
        if (inst == null)
        {
            var containerObj = GameObject.FindGameObjectWithTag("ToastContainer");
            if (containerObj == null)
            {
                Debug.LogWarningFormat("未找到Tag为ToastContainer的容器对象");
                return;
            }
            var toastObj = Instantiate(Resources.Load<GameObject>("Toast/Toast"), containerObj.transform);
            inst = toastObj.GetComponent<Toast>();
            bgColor = inst.bg.color;
            textColor = inst.text.color;
        }
        inst.ShowInternal(text, duration);
    }

    private void ShowInternal(string text, float duration = 3f)
    {
        inst.gameObject.SetActive(true);
        inst.text.text = text;
        inst.bg.color = bgColor;
        inst.text.color = textColor;
        if (coroutine != null)
        {
            StopCoroutine(coroutine);
            coroutine = null;
        }
        coroutine = StartCoroutine(Wait(duration));
    }

    private IEnumerator Wait(float duration = 3f)
    {
        yield return new WaitForSeconds(duration);
        yield return bg.DOColor(new Color(bgColor.r, bgColor.g, bgColor.b, 0), 1);
        yield return text.DOColor(new Color(textColor.r, textColor.g, textColor.b, 0), 1);
        finishedCallback?.Invoke();
    }
}
