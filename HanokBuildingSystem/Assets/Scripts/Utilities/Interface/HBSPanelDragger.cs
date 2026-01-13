using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// UI 패널을 드래그하여 이동할 수 있게 하는 컴포넌트
/// 드래그가 필요한 패널에만 선택적으로 추가하여 사용합니다.
/// </summary>
public class HBSPanelDragger : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Drag Settings")]
    [SerializeField] private bool constrainToScreen = true;
    [Tooltip("드래그할 RectTransform (null이면 자동으로 현재 GameObject의 RectTransform 사용)")]
    [SerializeField] private RectTransform targetRectTransform;

    [Header("Boundary Padding")]
    [SerializeField] private float leftPadding = 0f;
    [SerializeField] private float rightPadding = 0f;
    [SerializeField] private float topPadding = 0f;
    [SerializeField] private float bottomPadding = 0f;

    private RectTransform rectTransform;
    private Canvas canvas;
    private Vector2 originalPosition;

    void Awake()
    {
        rectTransform = targetRectTransform != null ? targetRectTransform : GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();

        if (rectTransform == null)
        {
            Debug.LogError("HBSPanelDragger: RectTransform을 찾을 수 없습니다.");
        }

        if (canvas == null)
        {
            Debug.LogError("HBSPanelDragger: Canvas를 찾을 수 없습니다.");
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (rectTransform == null) return;
        originalPosition = rectTransform.anchoredPosition;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (rectTransform == null || canvas == null) return;

        // 캔버스 스케일을 고려한 이동
        Vector2 delta = eventData.delta / canvas.scaleFactor;
        rectTransform.anchoredPosition += delta;

        // 화면 밖으로 나가지 않도록 제한
        if (constrainToScreen)
        {
            ConstrainToScreen();
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        // 드래그 종료 시 추가 처리가 필요하면 여기에 작성
    }

    /// <summary>
    /// Pivot과 Anchor를 고려해 패널이 화면 밖으로 나가지 않도록 제한
    /// </summary>
    private void ConstrainToScreen()
    {
        if (rectTransform == null || canvas == null) return;

        // Canvas의 RectTransform 가져오기
        RectTransform canvasRect = canvas.GetComponent<RectTransform>();
        if (canvasRect == null) return;

        Vector2 pos = rectTransform.anchoredPosition;
        Vector2 size = rectTransform.sizeDelta;
        Vector2 pivot = rectTransform.pivot;
        Vector2 canvasSize = canvasRect.sizeDelta;

        // Anchor에 따른 기준점 계산
        Vector2 anchorMin = rectTransform.anchorMin;
        Vector2 anchorMax = rectTransform.anchorMax;

        // Anchor 중심점 (Canvas 좌표계에서)
        float anchorCenterX = (anchorMin.x + anchorMax.x) * 0.5f * canvasSize.x - canvasSize.x * 0.5f;
        float anchorCenterY = (anchorMin.y + anchorMax.y) * 0.5f * canvasSize.y - canvasSize.y * 0.5f;

        // Pivot 기준으로 패널의 실제 경계 계산
        float panelLeft = pos.x - (size.x * pivot.x);
        float panelRight = pos.x + (size.x * (1 - pivot.x));
        float panelBottom = pos.y - (size.y * pivot.y);
        float panelTop = pos.y + (size.y * (1 - pivot.y));

        // Canvas 경계 (Canvas 중앙 기준)
        float canvasLeft = -canvasSize.x * 0.5f - anchorCenterX;
        float canvasRight = canvasSize.x * 0.5f - anchorCenterX;
        float canvasBottom = -canvasSize.y * 0.5f - anchorCenterY;
        float canvasTop = canvasSize.y * 0.5f - anchorCenterY;

        // 패널이 Canvas 밖으로 나가지 않도록 조정
        if (panelLeft < canvasLeft + leftPadding)
        {
            pos.x += (canvasLeft + leftPadding) - panelLeft;
        }
        if (panelRight > canvasRight - rightPadding)
        {
            pos.x -= panelRight - (canvasRight - rightPadding);
        }
        if (panelBottom < canvasBottom + bottomPadding)
        {
            pos.y += (canvasBottom + bottomPadding) - panelBottom;
        }
        if (panelTop > canvasTop - topPadding)
        {
            pos.y -= panelTop - (canvasTop - topPadding);
        }

        rectTransform.anchoredPosition = pos;
    }

    /// <summary>
    /// 패널을 원래 위치로 되돌림
    /// </summary>
    public void ResetPosition()
    {
        if (rectTransform != null)
        {
            rectTransform.anchoredPosition = originalPosition;
        }
    }

    /// <summary>
    /// 드래그 활성화/비활성화
    /// </summary>
    public void SetDraggable(bool draggable)
    {
        enabled = draggable;
    }
}
