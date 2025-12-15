using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HanokBuildingSystem
{
    public enum CollisionResponseType
    {
        None,             // 충돌해도 반응 없음
        ResetPosition,    // 충돌하면 원래 자리로 되돌림
        SwapTarget,   // 충돌체가 Building 인 경우 바꿔들음. 아니면 반응 없음.
    }

    /// <summary>
    /// 리모델링 모드에서 Building의 위치 수정, 추가, 삭제를 관리
    /// </summary>
    public class RemodelingController : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField] private HanokBuildingSystem buildingSystem;
        [SerializeField] private Camera mainCamera;
        [SerializeField] private bool clampPlacementPosition = true;

        [Header("Raycast Settings")]
        [SerializeField] private LayerMask buildingLayerMask;
        [SerializeField] private float raycastDistance = 1000f;

        [Header("Collision Settings")]
        [SerializeField] private bool shouldCheckCollision = true;
        [SerializeField] private CollisionResponseType collisionResponse = CollisionResponseType.None;

        [Header("Visual Feedback")]
        [SerializeField] private Color validColor = new Color(0, 1, 0, 0.5f);
        [SerializeField] private Color invalidColor = new Color(1, 0, 0, 0.5f);

        [Header("Custom rules")]
        [SerializeField] private List<MonoBehaviour> ruleSources; // IRemodelRule 구현 Mono들은 전부 여기 등록
        private readonly List<IRemodelingRule> rules = new();

        // Current state
        private Building selectedBuilding;
        private Building targetBuilding;
        private House targetHouse;
        [SerializeField] private bool isDragging = false;
        private Coroutine draggingCoroutine;
        private Vector3 originalPosition;
        private Quaternion originalRotation;

        // Mouse position from new Input System
        private Vector2 currentMousePosition;

        // Cached for performance
        private bool isValidPlacement = true;

        // Remodeling backup data
        private class BuildingSnapshot
        {
            public Building building;
            public Vector3 position;
            public Quaternion rotation;
            public int stageIndex;
        }
        private List<BuildingSnapshot> buildingBackup = new List<BuildingSnapshot>();

        private void Start()
        {
            if (buildingSystem == null)
            {
                buildingSystem = HanokBuildingSystem.Instance;
            }

            if (mainCamera == null)
            {
                mainCamera = Camera.main;
            }

            foreach (var src in ruleSources)
            {
                if (src is IRemodelingRule rule)
                    rules.Add(rule);
            }
        }

        #region Public API
        /// <summary>
        /// 화면 위치에서 Building을 선택 시도
        /// </summary>
        public bool TrySelectBuilding(Vector2 screenPosition, House house)
        {
            if (house == null)
            {
                Debug.LogWarning("[RemodelingController] Cannot select building: No target house provided.");
                return false;
            }

            Building building = RaycastBuilding(screenPosition);
            if (building == null)
            {
                return false;
            }

            // 선택한 Building이 해당 House에 속하는지 확인
            if (!house.Buildings.Contains(building))
            {
                Debug.LogWarning("[RemodelingController] Selected building does not belong to the target house.");
                return false;
            }

            SelectBuilding(building, house);
            return true;
        }

        /// <summary>
        /// 현재 선택된 Building을 배치 시도
        /// </summary>
        public bool TryPlaceBuilding()
        {
            if (!isDragging || selectedBuilding == null)
            {
                return false;
            }

            // 유효한 위치인지 체크 후 반응
            if (!isValidPlacement)
            {
                switch (collisionResponse)
                {
                    case CollisionResponseType.ResetPosition:
                        CancelSelection();
                        break;
                    case CollisionResponseType.SwapTarget:
                        if (targetBuilding != null && targetHouse != null)
                        {
                            StopDragging();
                            SelectBuilding(targetBuilding, targetHouse);

                            buildingSystem.Events.RaiseBuildingModified(targetHouse, selectedBuilding);
                        }
                        break;
                }
                return false;
            }
            else
            {
                // 배치 성공
                StopDragging();

                buildingSystem.Events.RaiseBuildingModified(targetHouse, selectedBuilding);

                selectedBuilding = null;

                return true;
            }
        }

        /// <summary>
        /// 현재 선택 취소 및 원래 위치로 복귀
        /// </summary>
        public void CancelSelection()
        {
            if (selectedBuilding != null && targetHouse != null)
            {
                // 원래 위치로 복원
                selectedBuilding.transform.position = originalPosition;
                selectedBuilding.transform.rotation = originalRotation;

                // 원래 위치의 유효성 검증
                bool isValid = ValidatePlacement(selectedBuilding, targetHouse);

                if (isValid)
                {
                    StopDragging();
                    selectedBuilding = null;
                    targetHouse = null;
                }
                else
                {
                    Debug.LogWarning($"[RemodelingController] Original position for {selectedBuilding.name} is no longer valid. " +
                    "Building may overlap or be outside house bounds.");

                    UpdateVisualFeedback(selectedBuilding, isValid);
                }
            }
        }

        /// <summary>
        /// 새 Input System에서 마우스 위치 업데이트
        /// HanokSystemController의 HandlePointerMove에서 호출
        /// </summary>
        public void UpdateMousePosition(Vector2 screenPosition)
        {
            currentMousePosition = screenPosition;
        }

        /// <summary>
        /// 현재 드래그 중인지 여부
        /// </summary>
        public bool IsDragging => isDragging;

        /// <summary>
        /// 현재 선택된 Building
        /// </summary>
        public Building SelectedBuilding => selectedBuilding;

        /// <summary>
        /// 리모델링 시작 - 현재 하우스 상태 백업
        /// </summary>
        public void StartRemodeling(House house)
        {
            if (house == null)
            {
                Debug.LogWarning("[RemodelingController] Cannot start remodeling: House is null.");
                return;
            }

            targetHouse = house;
            BackupHouseState(house);
        }

        /// <summary>
        /// 리모델링 완성 - 하우스를 UnderConstruction으로, 빌딩들을 0단계로 초기화
        /// </summary>
        public bool CompleteRemodeling()
        {
            if (targetHouse == null)
            {
                Debug.LogWarning("[RemodelingController] Cannot complete remodeling: No target house.");
                return false;
            }

            // 드래그 중이면 취소
            if (isDragging)
            {
                CancelSelection();
            }

            // 하우스를 UnderConstruction 상태로 변경
            targetHouse.SetUsageState(HouseOccupancyState.UnderConstruction);

            // 변경된 빌딩들의 건설 단계를 0으로 초기화
            ResetModifiedBuildingsToStageZero();

            buildingSystem.Events.RaiseRemodelingCompleted(targetHouse);
            Debug.Log($"[RemodelingController] Completed remodeling for {targetHouse.name}");

            ClearBackup();
            targetHouse = null;
            return true;
        }

        /// <summary>
        /// 리모델링 취소 - 백업된 상태로 복원
        /// </summary>
        public bool CancelRemodeling()
        {
            if (targetHouse == null)
            {
                Debug.LogWarning("[RemodelingController] Cannot cancel remodeling: No target house.");
                return false;
            }

            // 드래그 중이면 취소
            if (isDragging)
            {
                CancelSelection();
            }

            // 백업된 상태로 복원
            RestoreHouseState();

            buildingSystem.Events.RaiseRemodelingCancelled(targetHouse);
            Debug.Log($"[RemodelingController] Cancelled remodeling for {targetHouse.name}");

            ClearBackup();
            targetHouse = null;
            return true;
        }
        #endregion

        #region Building Selection & Dragging
        private void SelectBuilding(Building building, House house)
        {
            selectedBuilding = building;
            targetHouse = house;
            originalPosition = building.transform.position;
            originalRotation = building.transform.rotation;

            StartDragging();
        }

        private void StartDragging()
        {
            if (isDragging)
            {
                StopDragging();
            }

            isDragging = true;
            draggingCoroutine = StartCoroutine(DragBuildingCoroutine());
        }

        private void StopDragging()
        {
            if (draggingCoroutine != null)
            {
                StopCoroutine(draggingCoroutine);
                draggingCoroutine = null;
            }

            isDragging = false;
        }

        /// <summary>
        /// Building을 마우스 커서 위치로 이동
        /// </summary>
        private IEnumerator DragBuildingCoroutine()
        {
            while (isDragging && selectedBuilding != null)
            {
                Vector3 newPosition = ScreenToWorldPosition(currentMousePosition);

                // 하우스 영역 내부로 제한
                if (clampPlacementPosition)
                {
                    newPosition = ClampToHouseBounds(newPosition, targetHouse);
                } else
                {
                    // 배치 가능 여부 검사
                    isValidPlacement = ValidatePlacement(selectedBuilding, targetHouse);
                }
                
                selectedBuilding.transform.position = newPosition;

                // 임의 룰 추가
                foreach(IRemodelingRule rule in rules)
                {
                    string failReason;
                    bool enforce;
                    if (!rule.ControlBuilding(selectedBuilding, targetHouse, newPosition, out failReason, out enforce))
                    {
                        if(enforce) StopDragging();
                        yield return null;
                    }
                }                

                // 시각적 피드백 (옵션)
                UpdateVisualFeedback(selectedBuilding, isValidPlacement);

                yield return new WaitForSeconds(0.02f);
            }
        }
        #endregion

        #region Validation
        /// <summary>
        /// Building 배치가 유효한지 검사
        /// </summary>
        private bool ValidatePlacement(Building building, House house)
        {
            if (building == null || house == null)
            {
                return false;
            }

            // 하우스 영역 내부인지 확인
            if (!IsWithinHouseBounds(building.transform.position, house))
            {
                return false;
            }

            // 충돌 검사
            if (shouldCheckCollision && CheckBuildingCollision(building))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// 위치가 하우스 영역 내부인지 검사 (2D Point-in-Polygon)
        /// 모든 아웃라인을 탐색하여 하나라도 내부에 있으면 true 반환
        /// </summary>
        private bool IsWithinHouseBounds(Vector3 position, House house)
        {
            if (house == null || house.BoundaryPlot == null || house.BoundaryPlot.LineList == null || house.BoundaryPlot.LineList.Count == 0)
            {
                return true; // 경계가 없으면 허용
            }

            Vector2 point = new Vector2(position.x, position.z);

            // 모든 아웃라인을 탐색
            int intersectCount = 0;
            foreach (List<Vector3> outline in house.BoundaryPlot.LineList)
            {
                if (outline == null || outline.Count < 2)
                    continue;

                // 2D Point-in-Polygon 알고리즘 (Ray Casting)
                for (int i = 0; i < outline.Count; i++)
                {
                    Vector2 v1 = new Vector2(outline[i].x, outline[i].z);
                    Vector2 v2 = new Vector2(outline[i + 1].x, outline[i + 1].z);

                    if (RayIntersectsSegment(point, v1, v2))
                    {
                        intersectCount++;
                    }
                }
            }

            Debug.Log($"intersectCount: {intersectCount}");
            // 홀수 번 교차하면 내부 - 하나라도 내부에 있으면 true 반환
            if ((intersectCount % 2) == 1)
            {
                return true;
            }

            // 모든 아웃라인을 확인했지만 내부가 아님
            return false;
        }

        /// <summary>
        /// Ray Casting 알고리즘: 점에서 오른쪽으로 뻗은 광선이 선분과 교차하는지 검사
        /// </summary>
        private bool RayIntersectsSegment(Vector2 point, Vector2 v1, Vector2 v2)
        {
            // 선분이 두 점 모두 point.y 보다 큰지, 작은지 확인 ((T == T) || (F == F))
            if ((v1.y > point.y) == (v2.y > point.y))
            {
                return false;
            }

            // 교차점의 x 좌표 계산
            float intersectX = v1.x + (point.y - v1.y) * (v2.x - v1.x) / (v2.y - v1.y);

            // 교차점이 점의 오른쪽에 있는지 확인
            return intersectX > point.x;
        }

        /// <summary>
        /// 위치를 하우스 경계 내부로 클램핑
        /// 모든 아웃라인을 탐색하여 가장 가까운 경계 지점 찾기
        /// </summary>
        private Vector3 ClampToHouseBounds(Vector3 position, House house)
        {
            if (house == null || house.BoundaryPlot == null || house.BoundaryPlot.LineList == null || house.BoundaryPlot.LineList.Count == 0)
            {
                return position;
            }

            // 이미 내부에 있으면 그대로 반환
            if (IsWithinHouseBounds(position, house))
            {
                return position;
            }

            // 경계 내부로 강제 이동: 모든 아웃라인에서 가장 가까운 경계 지점 찾기
            Vector3 closestPoint = position;
            float minDistance = float.MaxValue;

            foreach (var outline in house.BoundaryPlot.LineList)
            {
                if (outline == null || outline.Count < 2)
                    continue;

                // 각 선분에 대해 가장 가까운 점 찾기
                for (int i = 0; i < outline.Count; i++)
                {
                    Vector3 v1 = outline[i];
                    Vector3 v2 = outline[(i + 1) % outline.Count];

                    Vector3 pointOnSegment = ClosestPointOnSegment(position, v1, v2);
                    float distance = Vector3.Distance(position, pointOnSegment);

                    if (distance < minDistance)
                    {
                        minDistance = distance;
                        closestPoint = pointOnSegment;
                    }
                }
            }

            // 경계 안쪽으로 살짝 이동 (0.5 유닛)
            Vector3 center = house.transform.position;
            Vector3 direction = (center - closestPoint).normalized;
            closestPoint += direction * 0.5f;

            // Y 좌표는 원래 위치 유지
            closestPoint.y = position.y;

            return closestPoint;
        }

        /// <summary>
        /// 선분 위의 가장 가까운 점 계산
        /// </summary>
        private Vector3 ClosestPointOnSegment(Vector3 point, Vector3 segmentStart, Vector3 segmentEnd)
        {
            Vector3 segment = segmentEnd - segmentStart;
            Vector3 pointVector = point - segmentStart;

            float segmentLengthSquared = segment.sqrMagnitude;
            if (segmentLengthSquared < 0.0001f)
            {
                return segmentStart;
            }

            float t = Mathf.Clamp01(Vector3.Dot(pointVector, segment) / segmentLengthSquared);
            return segmentStart + segment * t;
        }

        /// <summary>
        /// 다른 Building과 충돌하는지 검사
        /// </summary>
        private bool CheckBuildingCollision(Building building)
        {
            if (building == null || targetHouse == null)
            {
                Debug.LogWarning($"Null [targetHouse] for {building.name}`s collision check.");
                return false;
            }

            Collider buildingCollider = building.GetComponent<Collider>();
            if (buildingCollider == null)
            {
                Debug.LogWarning($"Null [Collider] for {building.name}`s collision check.");
                return false;
            }

            Physics.SyncTransforms();

            Bounds bounds = buildingCollider.bounds;
            Vector3 center = bounds.center;    // 월드 좌표 기준 중심
            Vector3 halfExtents = bounds.extents;   // 월드 기준 반지름(가로/세로/높이의 절반)

            Collider[] overlappingColliders = Physics.OverlapBox(
                center,
                halfExtents,
                building.transform.rotation,
                buildingLayerMask
            );

            foreach (Collider otherCollider in overlappingColliders)
            {
                // 필터
                if (otherCollider == buildingCollider)
                {
                    continue;
                }

                Building otherBuilding = otherCollider.GetComponent<Building>();
                if (otherBuilding == null)
                {
                    continue;
                }

                if (otherBuilding != null && otherBuilding != building)
                {
                    // 같은 하우스 내의 빌딩인지 확인
                    if (targetHouse.Buildings.Contains(otherBuilding))
                    {
                        targetBuilding = otherBuilding;
                        return true; // 충돌 발생
                    }
                }
            }

            return false;
        }
        #endregion

        #region Raycast & Utilities
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

        private Vector3 ScreenToWorldPosition(Vector2 screenPosition)
        {
            if (mainCamera == null)
            {
                return Vector3.zero;
            }

            Ray ray = mainCamera.ScreenPointToRay(screenPosition);

            // 지면과의 교차점 계산
            Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
            if (groundPlane.Raycast(ray, out float enter))
            {
                return ray.GetPoint(enter);
            }

            return Vector3.zero;
        }

        private void UpdateVisualFeedback(Building building, bool isValid)
        {
            Renderer renderer = building.GetComponentInChildren<Renderer>();
            if (renderer != null)
            {
                MaterialPropertyBlock propertyBlock = new MaterialPropertyBlock();
                propertyBlock.SetColor("_BaseMap", isValid ? validColor : invalidColor);
                renderer.SetPropertyBlock(propertyBlock);
            }
        }
        #endregion

        #region Backup & Restore
        /// <summary>
        /// 하우스의 빌딩 상태 백업
        /// </summary>
        private void BackupHouseState(House house)
        {
            buildingBackup.Clear();

            if (house == null || house.Buildings == null)
            {
                return;
            }

            foreach (Building building in house.Buildings)
            {
                if (building == null) continue;

                BuildingSnapshot snapshot = new BuildingSnapshot
                {
                    building = building,
                    position = building.transform.position,
                    rotation = building.transform.rotation,
                    stageIndex = building.CurrentStageIndex
                };

                buildingBackup.Add(snapshot);
            }
        }

        /// <summary>
        /// 백업된 상태로 복원
        /// </summary>
        private void RestoreHouseState()
        {
            foreach (BuildingSnapshot snapshot in buildingBackup)
            {
                if (snapshot.building == null) continue;

                snapshot.building.transform.position = snapshot.position;
                snapshot.building.transform.rotation = snapshot.rotation;
                snapshot.building.SetStageIndex(snapshot.stageIndex);
            }

            Debug.Log($"[RemodelingController] Restored {buildingBackup.Count} buildings to original state.");
        }

        /// <summary>
        /// 백업 데이터와 비교하여 변경된 빌딩들의 건설 단계를 0으로 초기화
        /// </summary>
        private void ResetModifiedBuildingsToStageZero()
        {
            int modifiedCount = 0;

            foreach (BuildingSnapshot snapshot in buildingBackup)
            {
                if (snapshot.building == null) continue;

                // 위치나 회전이 변경되었는지 확인
                bool positionChanged = Vector3.Distance(snapshot.building.transform.position, snapshot.position) > 0.01f;
                bool rotationChanged = Quaternion.Angle(snapshot.building.transform.rotation, snapshot.rotation) > 0.1f;

                if (positionChanged || rotationChanged)
                {
                    snapshot.building.SetStageIndex(0);
                    modifiedCount++;
                    Debug.Log($"[RemodelingController] Reset {snapshot.building.name} to construction stage 0 (modified)");
                }
            }

            // 새로 추가된 빌딩들도 0단계로 설정
            if (targetHouse != null && targetHouse.Buildings != null)
            {
                foreach (Building building in targetHouse.Buildings)
                {
                    if (building == null) continue;

                    // 백업에 없는 빌딩 = 새로 추가된 빌딩
                    bool isNewBuilding = !buildingBackup.Exists(s => s.building == building);
                    if (isNewBuilding)
                    {
                        building.SetStageIndex(0);
                        modifiedCount++;
                        Debug.Log($"[RemodelingController] Reset {building.name} to construction stage 0 (newly added)");
                    }
                }
            }

            Debug.Log($"[RemodelingController] Reset {modifiedCount} modified/added buildings to stage 0.");
        }

        /// <summary>
        /// 백업 데이터 클리어
        /// </summary>
        private void ClearBackup()
        {
            buildingBackup.Clear();
        }
        #endregion
    }
}
