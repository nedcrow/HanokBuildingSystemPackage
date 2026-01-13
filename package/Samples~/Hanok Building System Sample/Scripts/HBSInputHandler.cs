using System;
using System.ComponentModel;
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
    [SerializeField] private InputActionReference rotateLeftAction;      // Q key
    [SerializeField] private InputActionReference rotateRightAction;     // E key
    [SerializeField] private InputActionReference deleteAction;          // Delete key

    [Header("Double Click")]
    [SerializeField] private float doubleClickInterval = 0.25f; // 두 번 클릭 사이 최대 간격(초)
    private float _lastClickUpTime = -999f;

    [Header("Drag")][Description("드래그 시작 판정 픽셀 거리")]
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
    public event Action OnRotateLeft;              // Q 키 눌렀을 때
    public event Action OnRotateRight;             // E 키 눌렀을 때
    public event Action OnDelete;                  // Delete 키 눌렀을 때

    private void OnEnable()
    {
        // Check for unassigned InputActionReferences
        var unassignedActions = new System.Collections.Generic.List<string>();

        if (mousePositionAction == null) unassignedActions.Add("mousePositionAction");
        if (leftButtonAction == null) unassignedActions.Add("leftButtonAction");
        if (rightButtonAction == null) unassignedActions.Add("rightButtonAction");
        if (rotateLeftAction == null) unassignedActions.Add("rotateLeftAction");
        if (rotateRightAction == null) unassignedActions.Add("rotateRightAction");
        if (deleteAction == null) unassignedActions.Add("deleteAction");

        if (unassignedActions.Count > 0)
        {
            Debug.LogWarning($"[HBSInputHandler] Unassigned InputActionReferences: {string.Join(", ", unassignedActions)}");
        }

        var pos = mousePositionAction?.action;
        var leftBtn = leftButtonAction?.action;
        var rightBtn = rightButtonAction?.action;
        var rotateLeft = rotateLeftAction?.action;
        var rotateRight = rotateRightAction?.action;
        var delete = deleteAction?.action;

        if (pos != null)
        {
            pos.performed += OnPointerMoveInternal;
            pos.Enable();
        }

        if (leftBtn != null)
        {
            leftBtn.started += OnLeftButtonDownInternal;
            leftBtn.canceled += OnLeftButtonUpInternal;
            leftBtn.Enable();
        }

        if (rightBtn != null)
        {
            rightBtn.started += OnRightButtonDownInternal;
            rightBtn.canceled += OnRightButtonUpInternal;
            rightBtn.Enable();
        }

        if (rotateLeft != null)
        {
            rotateLeft.performed += OnRotateLeftInternal;
            rotateLeft.Enable();
        }

        if (rotateRight != null)
        {
            rotateRight.performed += OnRotateRightInternal;
            rotateRight.Enable();
        }

        if (delete != null)
        {
            delete.performed += OnDeleteInternal;
            delete.Enable();
        }
    }

    private void OnDisable()
    {
        var pos = mousePositionAction?.action;
        var leftBtn = leftButtonAction?.action;
        var rightBtn = rightButtonAction?.action;
        var rotateLeft = rotateLeftAction?.action;
        var rotateRight = rotateRightAction?.action;
        var delete = deleteAction?.action;

        if (pos != null)
        {
            pos.performed -= OnPointerMoveInternal;
            pos.Disable();
        }

        if (leftBtn != null)
        {
            leftBtn.started -= OnLeftButtonDownInternal;
            leftBtn.canceled -= OnLeftButtonUpInternal;
            leftBtn.Disable();
        }

        if (rightBtn != null)
        {
            rightBtn.started -= OnRightButtonDownInternal;
            rightBtn.canceled -= OnRightButtonUpInternal;
            rightBtn.Disable();
        }

        if (rotateLeft != null)
        {
            rotateLeft.performed -= OnRotateLeftInternal;
            rotateLeft.Disable();
        }

        if (rotateRight != null)
        {
            rotateRight.performed -= OnRotateRightInternal;
            rotateRight.Disable();
        }

        if (delete != null)
        {
            delete.performed -= OnDeleteInternal;
            delete.Disable();
        }
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

    // Q 키 눌렀을 때
    private void OnRotateLeftInternal(InputAction.CallbackContext ctx)
    {
        Debug.Log("HandleRotateLeft");
        OnRotateLeft?.Invoke();
    }

    // E 키 눌렀을 때
    private void OnRotateRightInternal(InputAction.CallbackContext ctx)
    {
        Debug.Log("HandleRotateRight");
        OnRotateRight?.Invoke();
    }

    // Delete 키 눌렀을 때
    private void OnDeleteInternal(InputAction.CallbackContext ctx)
    {
        Debug.Log("HandleDelete");
        OnDelete?.Invoke();
    }
    }
}
