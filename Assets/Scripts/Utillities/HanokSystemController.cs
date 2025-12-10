using UnityEngine;
using UnityEngine.InputSystem;
using HanokBuildingSystem;

/// <summary>
/// HanokBuildingSystem 컨트롤러
///
/// 역할:
/// - Core를 활용하는 예제
/// - Input에 따른 명령 할당
/// - HanokBuildingSystem의 API를 호출하여 Core와 통신
/// </summary>
public class HanokSystemController : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private HanokBuildingSystem.HanokBuildingSystem buildingSystem;
    [SerializeField] private HBSInputHandler inputHandler;

    [Header("Raycast Settings")]
    [SerializeField] private LayerMask houseLayerMask;
    [SerializeField] private Camera mainCamera;
    [SerializeField] private float minYPosition = 0f;

    private Vector3 dragStartPosition;
    private Vector3 dragEndPosition;
    private House currentHoveredHouse;

    void Start()
    {
        if (buildingSystem == null)
        {
            buildingSystem = HanokBuildingSystem.HanokBuildingSystem.Instance;
        }

        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        if (inputHandler == null)
        {
            inputHandler = GetComponent<HBSInputHandler>();
        }
    }

    void OnEnable()
    {
        if (inputHandler != null)
        {
            inputHandler.OnLeftClickUp += HandleLeftClick;
            inputHandler.OnRightClickUp += HandleRightClick;
            inputHandler.OnDoubleClickUp += HandleDoubleClick;
            inputHandler.OnPointerMove += HandlePointerMove;
            inputHandler.OnDragStart += HandleDragStart;
            inputHandler.OnDragging += HandleDragging;
            inputHandler.OnDragEnd += HandleDragEnd;
        }
    }

    void OnDisable()
    {
        if (inputHandler != null)
        {
            inputHandler.OnLeftClickUp -= HandleLeftClick;
            inputHandler.OnRightClickUp -= HandleRightClick;
            inputHandler.OnDoubleClickUp -= HandleDoubleClick;
            inputHandler.OnPointerMove -= HandlePointerMove;
            inputHandler.OnDragStart -= HandleDragStart;
            inputHandler.OnDragging -= HandleDragging;
            inputHandler.OnDragEnd -= HandleDragEnd;
        }
    }

    #region Input Handlers
    private void HandleRemodelingLeftClick(Vector2 screenPosition)
    {
        if (buildingSystem.RemodelingController == null)
        {
            Debug.LogWarning("[HanokSystemController] RemodelingController is not assigned.");
            return;
        }

        // 이미 드래그 중이면 배치 시도
        if (buildingSystem.RemodelingController.IsDragging)
        {
            buildingSystem.RemodelingController.TryPlaceBuilding();
        }
        // 아니면 Building 선택 시도
        else
        {
            if (buildingSystem.CurrentHouses.Count > 0)
            {
                House targetHouse = buildingSystem.CurrentHouses[0];
                buildingSystem.RemodelingController.TrySelectBuilding(screenPosition, targetHouse);
            }
        }
    }

    private void HandleLeftClick(Vector2 screenPosition)
    {
        switch (buildingSystem.CurrentState)
        {
            case SystemState.Off:
                House house = RaycastHouse(screenPosition);
                if (house != null)
                {
                    buildingSystem.SelectHouse(house);
                }
                break;

            case SystemState.NewBuilding:
                Plot currentPlot = buildingSystem.GetCurrentPlot();
                if (currentPlot != null && currentPlot.LineList.Count < buildingSystem.MaxVertexCount)
                {
                    Vector3 worldPos = ScreenToWorldPosition(screenPosition);
                    buildingSystem.AddVertex(worldPos);
                }
                else
                {
                    buildingSystem.SetState(SystemState.Off);
                }
                break;
            case SystemState.Remodeling:
                HandleRemodelingLeftClick(screenPosition);
                break;
        }
    }

    private void HandleRightClick(Vector2 screenPosition)
    {
        switch (buildingSystem.CurrentState)
        {
            case SystemState.Off:
                buildingSystem.ClearSelection();
                break;

            case SystemState.Remodeling:
                // 드래그 중이면 선택 취소
                if (buildingSystem.RemodelingController != null && buildingSystem.RemodelingController.IsDragging)
                {
                    buildingSystem.RemodelingController.CancelSelection();
                }
                else
                {
                    buildingSystem.ClearSelection();
                }
                break;

            case SystemState.NewBuilding:
                if(buildingSystem.GetCurrentPlot() == null || buildingSystem.GetCurrentPlot().GetLineCount() < 1) break;

                buildingSystem.RemoveLastVertex();
                if (buildingSystem.GetCurrentPlot().GetLineCount() < 3)
                {
                    buildingSystem.ReturnCurrentHouses();
                    buildingSystem.ClearSelection();
                    if (buildingSystem.GetCurrentPlot().GetLineCount() <= 1)
                    {
                        buildingSystem.CancelConstruction();
                    }
                }                
                break;
        }
    }

    private void HandleDoubleClick(Vector2 screenPosition)
    {
        House house = RaycastHouse(screenPosition);
        if (house != null)
        {
            RaycastMultipleHouses(house.HouseType);
        }
    }

    private void HandlePointerMove(Vector2 screenPosition)
    {
        // Update vertex position in NewBuilding mode
        if (buildingSystem.CurrentState == SystemState.NewBuilding)
        {
            Vector3 worldPos = ScreenToWorldPosition(screenPosition);
            buildingSystem.UpdateCurrentVertexPosition(worldPos);
            buildingSystem.UpdateVisualization();
        }

        // Update building position in Remodeling mode
        if (buildingSystem.CurrentState == SystemState.Remodeling)
        {
            if (buildingSystem.RemodelingController != null)
            {
                buildingSystem.RemodelingController.UpdateMousePosition(screenPosition);
            }
        }

        // Handle house hover for outline/highlight
        House house = RaycastHouse(screenPosition);
        if (house != currentHoveredHouse)
        {
            if (currentHoveredHouse != null)
            {
                buildingSystem.Events.RaiseHouseHoverExit(currentHoveredHouse);
            }

            if (house != null)
            {
                buildingSystem.Events.RaiseHouseHoverEnter(house);
            }

            currentHoveredHouse = house;
        }
    }

    private void HandleDragStart(Vector2 screenPosition)
    {
        dragStartPosition = ScreenToWorldPosition(screenPosition);
    }

    private void HandleDragging(Vector2 screenPosition)
    {
        // 드래그 중 시각적 피드백 (예: 선택 영역 표시) 가능
    }

    private void HandleDragEnd(Vector2 screenPosition)
    {
        dragEndPosition = ScreenToWorldPosition(screenPosition);

        if (buildingSystem.CurrentState == SystemState.Off)
        {
            float distance = Vector3.Distance(dragStartPosition, dragEndPosition);
            if (distance > 0.5f)
            {
                RaycastHousesInArea(dragStartPosition, dragEndPosition);
            }
        }
    }
    #endregion

    #region Raycast Methods
    private House RaycastHouse(Vector2 screenPosition)
    {
        if (Camera.main == null) return null;

        Ray ray = Camera.main.ScreenPointToRay(screenPosition);
        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, houseLayerMask))
        {
            if (hit.collider != null)
            {
                return hit.collider.GetComponentInParent<House>();
            }
        }
        return null;
    }

    private void RaycastMultipleHouses(HouseType targetType)
    {
        House[] allHouses = FindObjectsByType<House>(FindObjectsSortMode.None);

        foreach (House house in allHouses)
        {
            if (house.HouseType == targetType)
            {
                buildingSystem.SelectHouse(house);
            }
        }
    }

    private void RaycastHousesInArea(Vector3 startPos, Vector3 endPos, HouseType houseType = HouseType.None)
    {
        Vector3 center = (startPos + endPos) / 2f;
        Vector3 size = new Vector3(
            Mathf.Abs(endPos.x - startPos.x),
            100f,
            Mathf.Abs(endPos.z - startPos.z)
        );
        Vector3 halfExtents = size / 2f;

        Collider[] colliders = Physics.OverlapBox(center, halfExtents, Quaternion.identity, houseLayerMask);

        foreach (Collider col in colliders)
        {
            House house = col.GetComponentInParent<House>();
            if (house != null)
            {
                if (houseType == HouseType.None || house.HouseType == houseType)
                {
                    buildingSystem.SelectHouse(house);
                }
            }
        }
    }
    #endregion

    #region Utility
    private Vector3 ScreenToWorldPosition(Vector2 screenPosition)
    {
        if (mainCamera == null) return Vector3.zero;

        Ray ray = mainCamera.ScreenPointToRay(screenPosition);
        Vector3 worldPos = Vector3.zero;

        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            worldPos = hit.point;
        }
        else
        {
            Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
            if (groundPlane.Raycast(ray, out float enter))
            {
                worldPos = ray.GetPoint(enter);
            }
        }

        if (worldPos.y < minYPosition)
        {
            worldPos.y = minYPosition;
        }

        return worldPos;
    }
    #endregion
}
