using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace HanokBuildingSystem
{
    public class HBSInputHandler : MonoBehaviour
    {
    [Header("Input Actions")]
    [SerializeField] private InputActionReference mousePositionAction; // <Mouse>/position
    [SerializeField] private InputActionReference leftButtonAction;      // <Mouse>/leftButton
    [SerializeField] private InputActionReference rightButtonAction;     // <Mouse>/rightButton

    [Header("Double Click")]
    [SerializeField] private float doubleClickInterval = 0.25f; // 두 번 클릭 사이 최대 간격(초)
    private float _lastClickUpTime = -999f;

    [Header("Drag")]
    [SerializeField] private float dragStartDistance = 5f; // 드래그 시작 판정 픽셀 거리
    private bool _isPointerDown;
    private bool _isDragging;
    private Vector2 _dragStartPos;

    // 외부에서 구독해서 쓰는 콜백들
    public event Action<Vector2> OnLeftClickDown;  // 좌클릭 눌렀을 때
    public event Action<Vector2> OnLeftClickUp;    // 좌클릭 뗐을 때
    public event Action<Vector2> OnRightClickDown; // 우클릭 눌렀을 때
    public event Action<Vector2> OnRightClickUp;   // 우클릭 뗐을 때
    public event Action<Vector2> OnPointerMove;    // 마우스 움직일 때
    public event Action<Vector2> OnDoubleClickUp;  // 더블클릭 후 손 뗐을 때
    public event Action<Vector2> OnDragStart;      // 드래그 시작 순간
    public event Action<Vector2> OnDragging;       // 드래그 중 계속 (마우스 위치 전달)
    public event Action<Vector2> OnDragEnd;        // 드래그 끝났을 때(손 뗐을 때)

    private void OnEnable()
    {
        var pos = mousePositionAction.action;
        var leftBtn = leftButtonAction.action;
        var rightBtn = rightButtonAction?.action;

        pos.performed += OnPointerMoveInternal;
        leftBtn.started += OnLeftButtonDownInternal;
        leftBtn.canceled += OnLeftButtonUpInternal;

        if (rightBtn != null)
        {
            rightBtn.started += OnRightButtonDownInternal;
            rightBtn.canceled += OnRightButtonUpInternal;
        }

        pos.Enable();
        leftBtn.Enable();
        rightBtn?.Enable();
    }

    private void OnDisable()
    {
        var pos = mousePositionAction.action;
        var leftBtn = leftButtonAction.action;
        var rightBtn = rightButtonAction?.action;

        pos.performed -= OnPointerMoveInternal;
        leftBtn.started -= OnLeftButtonDownInternal;
        leftBtn.canceled -= OnLeftButtonUpInternal;

        if (rightBtn != null)
        {
            rightBtn.started -= OnRightButtonDownInternal;
            rightBtn.canceled -= OnRightButtonUpInternal;
        }

        pos.Disable();
        leftBtn.Disable();
        rightBtn?.Disable();
    }

    // 왼쪽 버튼 눌렀을 때
    private void OnLeftButtonDownInternal(InputAction.CallbackContext ctx)
    {
        _isPointerDown = true;
        _isDragging = false;
        _dragStartPos = mousePositionAction.action.ReadValue<Vector2>();

        OnLeftClickDown?.Invoke(_dragStartPos);
    }

    // 왼쪽 버튼 뗄 때
    private void OnLeftButtonUpInternal(InputAction.CallbackContext ctx)
    {
        _isPointerDown = false;

        Vector2 currentPos = mousePositionAction.action.ReadValue<Vector2>();

        // 드래그 끝
        if (_isDragging)
        {
            OnDragEnd?.Invoke(currentPos);
            _isDragging = false;
        }
        else
        {
            OnLeftClickUp?.Invoke(currentPos);
        }

        // 더블클릭 판정 (손 뗐을 때 기준)
        float now = Time.time;
        if (now - _lastClickUpTime <= doubleClickInterval)
        {
            // 두 번째 click up
            OnDoubleClickUp?.Invoke(currentPos);
        }

        _lastClickUpTime = now;
    }

    // 우클릭 버튼 눌렀을 때
    private void OnRightButtonDownInternal(InputAction.CallbackContext ctx)
    {
        Vector2 currentPos = mousePositionAction.action.ReadValue<Vector2>();
        OnRightClickDown?.Invoke(currentPos);
    }

    // 우클릭 버튼 뗄 때
    private void OnRightButtonUpInternal(InputAction.CallbackContext ctx)
    {
        Vector2 currentPos = mousePositionAction.action.ReadValue<Vector2>();
        OnRightClickUp?.Invoke(currentPos);
    }

    // 마우스 위치가 바뀔 때마다 호출됨
    private void OnPointerMoveInternal(InputAction.CallbackContext ctx)
    {
        Vector2 pos = ctx.ReadValue<Vector2>();

        // OnPointerMove 이벤트 발생
        OnPointerMove?.Invoke(pos);

        // 버튼이 안 눌려있으면 드래그 아님
        if (!_isPointerDown)
            return;

        // 아직 드래그 시작 전이면, 일정 거리 이상 움직였는지 체크
        if (!_isDragging)
        {
            float sqrDist = (pos - _dragStartPos).sqrMagnitude;
            if (sqrDist >= dragStartDistance * dragStartDistance)
            {
                _isDragging = true;
                OnDragStart?.Invoke(_dragStartPos);
            }
        }

        // 드래그 중이면 위치를 계속 콜백
        if (_isDragging)
        {
            OnDragging?.Invoke(pos);
        }
    }
    }
}
