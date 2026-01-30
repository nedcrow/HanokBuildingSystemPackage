using System.Collections;
using UnityEngine;
using HanokBuildingSystem;

/// <summary>
/// [Sample] 리모델링 모드의 입력 처리 및 드래그 로직을 담당하는 컨트롤러
/// 코어 로직은 RemodelingSystem에 위임하고, 입력 처리와 드래그 코루틴만 담당
/// </summary>
public class RemodelingController : MonoBehaviour
{
    #region Dependencies
    [Header("Dependencies")]
    [SerializeField] private HanokBuildingSystem.HanokBuildingSystem buildingSystem;
    [SerializeField] private Camera mainCamera;

    [Header("Raycast Settings")]
    [SerializeField] private LayerMask buildingLayerMask;
    [SerializeField] private float raycastDistance = 1000f;
    #endregion

    #region State
    private bool isDragging;
    private bool isEraserMode;
    private Vector2 currentMousePosition;
    private Coroutine draggingCoroutine;
    #endregion

    #region Properties
    public bool IsDragging => isDragging;
    public bool IsEraserMode => isEraserMode;
    private RemodelingSystem System => buildingSystem?.RemodelingSystem;
    #endregion

    #region Unity Lifecycle
    private void Start()
    {
        if (buildingSystem == null)
        {
            buildingSystem = HanokBuildingSystem.HanokBuildingSystem.Instance;
        }

        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }
    }

    private void OnEnable()
    {
        if (buildingSystem != null)
        {
            buildingSystem.Events.OnRemodelingCompleted += HandleRemodelingCompleted;
            buildingSystem.Events.OnRemodelingCancelled += HandleRemodelingCancelled;
        }
    }

    private void OnDisable()
    {
        if (buildingSystem != null)
        {
            buildingSystem.Events.OnRemodelingCompleted -= HandleRemodelingCompleted;
            buildingSystem.Events.OnRemodelingCancelled -= HandleRemodelingCancelled;
        }
    }
    #endregion

    #region Event Handlers
    /// <summary>
    /// 리모델링 완료 이벤트 핸들러 - 빌딩 상태 변경 처리
    /// </summary>
    private void HandleRemodelingCompleted(House house)
    {
        if (house == null || System == null) return;

        // 하우스를 UnderConstruction 상태로 변경
        house.SetUsageState(HouseOccupancyState.UnderConstruction);

        // 변경된 빌딩들의 건설 단계를 0으로 초기화
        System.ResetModifiedBuildingsToStageZero(house);

        Debug.Log($"[RemodelingController] Applied remodeling changes for {house.name}");
    }

    /// <summary>
    /// 리모델링 취소 이벤트 핸들러 - 백업 복원 처리
    /// </summary>
    private void HandleRemodelingCancelled(House house)
    {
        if (house == null || System == null) return;

        // 백업된 상태로 복원
        System.RestoreHouseState();

        Debug.Log($"[RemodelingController] Restored backup for {house.name}");
    }
    #endregion

    #region Public API - Input Handling
    /// <summary>
    /// 지우개 모드 설정/해제
    /// </summary>
    public void SetEraserMode(bool enabled)
    {
        isEraserMode = enabled;
        Debug.Log($"[RemodelingController] Eraser mode {(enabled ? "enabled" : "disabled")}");
    }

    /// <summary>
    /// 화면 위치에서 Building을 선택 시도
    /// 지우개 모드일 때는 Building을 제거
    /// </summary>
    public bool TrySelectBuilding(Vector2 screenPosition, House house)
    {
        if (System == null || house == null)
        {
            Debug.LogWarning("[RemodelingController] Cannot select building: System or house is null.");
            return false;
        }

        Building building = RaycastBuilding(screenPosition);
        if (building == null)
        {
            return false;
        }

        // 지우개 모드일 때는 건물 제거
        if (isEraserMode)
        {
            return RemoveBuildingDuringRemodeling(building, house);
        }

        // 선택한 Building이 해당 House에 속하는지 확인
        if (!house.Buildings.Contains(building))
        {
            Debug.LogWarning("[RemodelingController] Selected building does not belong to the target house.");
            return false;
        }

        // 일반 모드일 때는 건물 선택
        System.SelectBuilding(building);
        StartDragging();
        return true;
    }

    /// <summary>
    /// 현재 선택된 Building을 배치 시도
    /// </summary>
    public bool TryPlaceBuilding()
    {
        if (System == null || !isDragging || System.SelectedBuilding == null)
        {
            return false;
        }

        bool success = System.TryConfirmPlacement();

        if (success)
        {
            StopDragging();
        }
        // 실패 시 CollisionResponse에 따라 System이 처리
        // SwapTarget의 경우 드래그 유지, ResetPosition의 경우 System에서 CancelPlacement 호출

        // SwapTarget으로 다른 빌딩이 선택된 경우 드래그 유지
        if (!success && System.SelectedBuilding != null &&
            System.CollisionResponse == CollisionResponseType.SwapTarget)
        {
            // 드래그 계속 유지
        }
        else if (!success && System.SelectedBuilding == null)
        {
            StopDragging();
        }

        return success;
    }

    /// <summary>
    /// 현재 선택 취소 및 원래 위치로 복귀
    /// </summary>
    public void CancelSelection()
    {
        if (System != null)
        {
            System.CancelPlacement();
        }
        StopDragging();
    }

    /// <summary>
    /// 마우스 위치 업데이트 (HanokSystemController의 HandlePointerMove에서 호출)
    /// </summary>
    public void UpdateMousePosition(Vector2 screenPosition)
    {
        currentMousePosition = screenPosition;
    }

    /// <summary>
    /// 선택된 빌딩을 왼쪽으로 회전
    /// </summary>
    public void RotateLeft(float angle = 90f)
    {
        if (System != null)
        {
            System.RotateBuilding(-angle);
        }
    }

    /// <summary>
    /// 선택된 빌딩을 오른쪽으로 회전
    /// </summary>
    public void RotateRight(float angle = 90f)
    {
        if (System != null)
        {
            System.RotateBuilding(angle);
        }
    }
    #endregion

    #region Compatibility API (기존 RemodelingController API 호환)
    /// <summary>
    /// 리모델링 시작 (RemodelingSystem에 위임)
    /// </summary>
    public void StartRemodeling(House house)
    {
        if (System != null)
        {
            System.StartSession(house);
        }
    }

    /// <summary>
    /// 리모델링 완료 (RemodelingSystem에 위임)
    /// </summary>
    public bool CompleteRemodeling()
    {
        StopDragging();
        return System?.CompleteSession() ?? false;
    }

    /// <summary>
    /// 리모델링 취소 (RemodelingSystem에 위임)
    /// </summary>
    public bool CancelRemodeling()
    {
        StopDragging();
        return System?.CancelSession() ?? false;
    }

    /// <summary>
    /// 리모델링 중 새 Building 추가 시작 (RemodelingSystem에 위임)
    /// </summary>
    public Building AddBuildingDuringRemodeling(GameObject buildingPrefab, Vector3? position = null)
    {
        if (System == null)
        {
            Debug.LogWarning("[RemodelingController] Cannot add building: System is null.");
            return null;
        }

        Building newBuilding = System.BeginPlacingNew(buildingPrefab, position);

        if (newBuilding != null)
        {
            StartDragging();
        }

        return newBuilding;
    }

    /// <summary>
    /// 리모델링 중 Building 제거 (RemodelingSystem에 위임)
    /// </summary>
    public bool RemoveBuildingDuringRemodeling(Building building, House house)
    {
        return System?.RemoveBuilding(building) ?? false;
    }
    #endregion

    #region Drag Handling
    private void StartDragging()
    {
        if (isDragging)
        {
            StopDragging();
        }

        isDragging = true;
        System?.NotifyDragStarted();
        draggingCoroutine = StartCoroutine(DragBuildingCoroutine());
    }

    private void StopDragging()
    {
        if (draggingCoroutine != null)
        {
            StopCoroutine(draggingCoroutine);
            draggingCoroutine = null;
        }

        if (isDragging)
        {
            isDragging = false;
            System?.NotifyDragEnded();
        }
    }

    /// <summary>
    /// Building을 마우스 커서 위치로 이동
    /// </summary>
    private IEnumerator DragBuildingCoroutine()
    {
        if (System == null) yield break;

        // HanokBuildingSystem에서 지형 설정 가져오기
        bool useTerrainHeight = HanokBuildingSystem.HanokBuildingSystem.Instance != null &&
                               HanokBuildingSystem.HanokBuildingSystem.Instance.UseTerrainHeight;
        LayerMask terrainLayer = HanokBuildingSystem.HanokBuildingSystem.Instance != null ?
                                HanokBuildingSystem.HanokBuildingSystem.Instance.GroundLayerMask : default;

        while (isDragging && System.SelectedBuilding != null)
        {
            Vector3 newPosition = ScreenToWorldPosition(currentMousePosition, useTerrainHeight, terrainLayer);

            // RemodelingSystem에 위치 업데이트 위임
            System.UpdateBuildingPosition(newPosition);

            // 배치 유효성 검사
            PlacementInvalidReason invalidReason;
            bool isValid = System.ValidateCurrentPlacement(out invalidReason);

            // 커스텀 룰 적용
            System.ApplyCustomRules(newPosition, ref invalidReason, ref isValid);

            // 강제 실패 시 드래그 중지 (enforce가 true인 Failed 룰에서 처리)
            if (!isValid && (invalidReason & PlacementInvalidReason.CustomRule) != 0)
            {
                // 커스텀 룰로 인한 강제 실패는 System.ApplyCustomRules에서 처리됨
                // 필요시 여기서 추가 처리
                StopDragging();
            }

            yield return new WaitForSeconds(0.02f);
        }

        StopDragging();
    }
    #endregion

    #region Input Utilities
    private Building RaycastBuilding(Vector2 screenPosition)
    {
        if (mainCamera == null)
        {
            return null;
        }

        Ray ray = mainCamera.ScreenPointToRay(screenPosition);
        if (Physics.Raycast(ray, out RaycastHit hit, raycastDistance, buildingLayerMask))
        {
            return hit.collider.GetComponentInParent<Building>();
        }

        return null;
    }

    private Vector3 ScreenToWorldPosition(Vector2 screenPosition, bool useTerrainHeight = false, LayerMask terrainLayer = default)
    {
        if (mainCamera == null)
        {
            return Vector3.zero;
        }

        Ray ray = mainCamera.ScreenPointToRay(screenPosition);

        // 지형 높이 사용 시 Raycast로 지형과의 교차점 찾기
        if (useTerrainHeight && terrainLayer != 0)
        {
            if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, terrainLayer))
            {
                return hit.point;
            }
        }

        // 지형 높이 미사용 또는 Raycast 실패 시 y=0 평면 사용
        Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
        if (groundPlane.Raycast(ray, out float enter))
        {
            return ray.GetPoint(enter);
        }

        return Vector3.zero;
    }
    #endregion
}
