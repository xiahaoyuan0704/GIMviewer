using UnityEngine;
using UnityEngine.EventSystems;

public class PropertyPanelDrag : MonoBehaviour, IBeginDragHandler, IDragHandler
{
    private RectTransform rectTransform;
    private Canvas parentCanvas;
    private bool canDrag;

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
        canDrag = false;
        if (rectTransform == null)
        {
            return;
        }
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, eventData.position, eventData.pressEventCamera, out var localPoint))
        {
            canDrag = localPoint.y >= (rectTransform.rect.yMax - 40f);
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!canDrag || rectTransform == null || parentCanvas == null)
        {
            return;
        }

        var scaleFactor = parentCanvas.scaleFactor <= 0 ? 1f : parentCanvas.scaleFactor;
        rectTransform.anchoredPosition += eventData.delta / scaleFactor;
    }
}
