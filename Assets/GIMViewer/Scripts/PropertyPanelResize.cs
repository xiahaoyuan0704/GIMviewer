using UnityEngine;
using UnityEngine.EventSystems;

public class PropertyPanelResize : MonoBehaviour, IBeginDragHandler, IDragHandler
{
    private RectTransform rectTransform;
    private Canvas parentCanvas;
    private bool canResize;

    private const float HandleSize = 26f;
    private const float MinWidth = 280f;
    private const float MinHeight = 220f;

    private void Awake()
    {
        rectTransform = transform as RectTransform;
        parentCanvas = GetComponentInParent<Canvas>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (rectTransform == null)
        {
            rectTransform = transform as RectTransform;
        }
        if (parentCanvas == null)
        {
            parentCanvas = GetComponentInParent<Canvas>();
        }
        canResize = false;
        if (rectTransform == null)
        {
            return;
        }
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, eventData.position, eventData.pressEventCamera, out var localPoint))
        {
            canResize = localPoint.x >= (rectTransform.rect.xMax - HandleSize) && localPoint.y <= (rectTransform.rect.yMin + HandleSize);
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!canResize || rectTransform == null || parentCanvas == null)
        {
            return;
        }

        var scaleFactor = parentCanvas.scaleFactor <= 0 ? 1f : parentCanvas.scaleFactor;
        var delta = eventData.delta / scaleFactor;
        var current = rectTransform.sizeDelta;

        var width = Mathf.Max(MinWidth, current.x - delta.x);
        var height = Mathf.Min(-MinHeight, current.y + delta.y);
        rectTransform.sizeDelta = new Vector2(width, height);
    }
}
