using System.Collections.Generic;
using UnityEngine;

namespace HanokBuildingSystem
{
    /// <summary>
    /// 리모델링 시스템 코어 - 상태 관리 및 빌딩 배치 로직을 담당
    /// 입력 처리와 드래그 루프는 샘플 컨트롤러에서 구현
    /// </summary>
    public class RemodelingSystem : MonoBehaviour
    {
        #region Inspector Fields
        [Header("Dependencies")]
        [SerializeField] private HanokBuildingSystem buildingSystem;
        [SerializeField] private BuildingCatalog buildingCatalog;

        [Header("Placement Settings")]
        [SerializeField] private bool clampPlacementPosition = true;
        [SerializeField] private bool shouldCheckCollision = true;
        [SerializeField] private LayerMask collisionCheckLayers;
        [SerializeField] private CollisionResponseType collisionResponse = CollisionResponseType.None;

        [Header("Custom Rules")]
        [SerializeField] private List<MonoBehaviour> ruleSources;

        [Header("Current State (ReadOnly)")]
        [SerializeField, ReadOnly] private RemodelingPhase currentPhase = RemodelingPhase.Idle;
        [SerializeField, ReadOnly] private House targetHouse;
        [SerializeField, ReadOnly] private Building selectedBuilding;
        [SerializeField, ReadOnly] private Building targetBuilding;
        [SerializeField, ReadOnly] private bool isValidPlacement = true;
        #endregion

        #region Properties
        public RemodelingPhase CurrentPhase => currentPhase;
        public IRemodelingState CurrentState => currentState;
        public House TargetHouse => targetHouse;
        public Building SelectedBuilding => selectedBuilding;
        public Building TargetBuilding => targetBuilding;
        public bool IsValidPlacement => isValidPlacement;
        public bool ClampPlacementPosition { get => clampPlacementPosition; set => clampPlacementPosition = value; }
        public bool ShouldCheckCollision { get => shouldCheckCollision; set => shouldCheckCollision = value; }
        public CollisionResponseType CollisionResponse { get => collisionResponse; set => collisionResponse = value; }
        public List<IRemodelingRule> Rules => rules;
        /// <summary>
        /// 마지막 ApplyCustomRules 호출 시 각 룰의 결과 (캐시됨, GC 부담 없음)
        /// </summary>
        public IReadOnlyList<RemodelingRuleResult> LastRuleResults => lastRuleResults;
        #endregion

        #region Private State
        private IRemodelingState currentState;
        private readonly List<IRemodelingRule> rules = new();
        private readonly List<RemodelingRuleResult> lastRuleResults = new(); // 캐시된 룰 결과 (GC 부담 없음)
        private Vector3 originalPosition;
        private Quaternion originalRotation;
        private bool isNewlyAddedBuilding = false;

        // Remodeling backup data
        private class BuildingSnapshot
        {
            public Building building;
            public Vector3 position;
            public Quaternion rotation;
            public int stageIndex;
        }
        private List<BuildingSnapshot> buildingBackup = new List<BuildingSnapshot>();
        #endregion

        #region Unity Lifecycle
        private void Start()
        {
            if (buildingSystem == null)
            {
                buildingSystem = HanokBuildingSystem.Instance;
            }

            foreach (var src in ruleSources)
            {
                if (src is IRemodelingRule rule)
                    rules.Add(rule);
            }
        }

        private void Update()
        {
            currentState?.OnUpdate(this);
        }
        #endregion

        #region Session Management
        /// <summary>
        /// 리모델링 세션 시작 - 현재 하우스 상태 백업
        /// </summary>
        public void StartSession(House house)
        {
            if (house == null)
            {
                Debug.LogWarning("[RemodelingSystem] Cannot start session: House is null.");
                return;
            }

            targetHouse = house;

            // HanokBuildingSystem에서 지형 설정 가져오기
            bool useTerrainHeight = HanokBuildingSystem.Instance != null && HanokBuildingSystem.Instance.UseTerrainHeight;
            LayerMask terrainLayer = HanokBuildingSystem.Instance != null ? HanokBuildingSystem.Instance.GroundLayerMask : default;

            BackupHouseState();
            targetHouse.ShowModelHouse(targetHouse.BoundaryPlot, useTerrainHeight, terrainLayer);

            SetPhase(RemodelingPhase.Inspect);
        }

        /// <summary>
        /// 리모델링 완료 - 이벤트 발생 후 상태 초기화
        /// 실제 빌딩 상태 변경은 이벤트 핸들러(샘플 컨트롤러)에서 처리
        /// </summary>
        public bool CompleteSession()
        {
            if (targetHouse == null)
            {
                Debug.LogWarning("[RemodelingSystem] Cannot complete session: No target house.");
                return false;
            }

            // 드래그 중이면 배치 취소
            if (currentPhase == RemodelingPhase.Move || currentPhase == RemodelingPhase.Add)
            {
                CancelPlacement();
            }

            House completedHouse = targetHouse;

            // 이벤트 발생 (핸들러에서 ResetModifiedBuildingsToStageZero 등 처리)
            buildingSystem.Events.RaiseRemodelingCompleted(completedHouse);
            Debug.Log($"[RemodelingSystem] Completed session for {completedHouse.name}");

            ClearBackup();
            SetPhase(RemodelingPhase.Idle);
            targetHouse = null;
            return true;
        }

        /// <summary>
        /// 리모델링 취소 - 이벤트 발생 후 상태 초기화
        /// 실제 백업 복원은 이벤트 핸들러(샘플 컨트롤러)에서 처리
        /// </summary>
        public bool CancelSession()
        {
            if (targetHouse == null)
            {
                Debug.LogWarning("[RemodelingSystem] Cannot cancel session: No target house.");
                return false;
            }

            // 드래그 중이면 배치 취소
            if (currentPhase == RemodelingPhase.Move || currentPhase == RemodelingPhase.Add)
            {
                CancelPlacement();
            }

            House cancelledHouse = targetHouse;

            // 이벤트 발생 (핸들러에서 RestoreHouseState 등 처리)
            buildingSystem.Events.RaiseRemodelingCancelled(cancelledHouse);
            Debug.Log($"[RemodelingSystem] Cancelled session for {cancelledHouse.name}");

            ClearBackup();
            SetPhase(RemodelingPhase.Idle);
            targetHouse = null;
            return true;
        }
        #endregion

        #region Building Selection
        /// <summary>
        /// 기존 빌딩 선택
        /// </summary>
        public void SelectBuilding(Building building)
        {
            if (building == null || targetHouse == null)
            {
                Debug.LogWarning("[RemodelingSystem] Cannot select building: Building or target house is null.");
                return;
            }

            if (!targetHouse.Buildings.Contains(building))
            {
                Debug.LogWarning("[RemodelingSystem] Selected building does not belong to the target house.");
                return;
            }

            selectedBuilding = building;
            originalPosition = building.transform.position;
            originalRotation = building.transform.rotation;
            isNewlyAddedBuilding = false;

            buildingSystem.Events.RaiseRemodelingBuildingSelected(building);

            Debug.Log($"[RemodelingSystem] Selected building: {building.name}");
        }

        /// <summary>
        /// 빌딩 선택 해제
        /// </summary>
        public void DeselectBuilding()
        {
            if (selectedBuilding == null) return;

            selectedBuilding = null;
            targetBuilding = null;
            isNewlyAddedBuilding = false;

            SetPhase(RemodelingPhase.Inspect);
            buildingSystem.Events.RaiseRemodelingBuildingDeselected();

            Debug.Log("[RemodelingSystem] Deselected building");
        }
        #endregion

        #region Building Placement
        /// <summary>
        /// 새 빌딩 배치 시작
        /// </summary>
        public Building BeginPlacingNew(GameObject prefab, Vector3? position = null)
        {
            if (targetHouse == null)
            {
                Debug.LogWarning("[RemodelingSystem] Cannot add building: No target house in session.");
                return null;
            }

            if (prefab == null)
            {
                Debug.LogWarning("[RemodelingSystem] Cannot add building: Prefab is null.");
                return null;
            }

            // 이미 배치 중이면 취소
            if (currentPhase == RemodelingPhase.Move || currentPhase == RemodelingPhase.Add)
            {
                CancelPlacement();
            }

            // 배치 위치 결정 (지정되지 않으면 하우스 중심)
            Vector3 spawnPosition = position ?? targetHouse.transform.position;

            // Building 인스턴스 생성
            Building newBuilding;
            if (buildingCatalog != null)
            {
                newBuilding = buildingCatalog.GetBuilding(prefab, spawnPosition, Quaternion.identity);
            }
            else
            {
                newBuilding = Instantiate(prefab, spawnPosition, Quaternion.identity).GetComponent<Building>();
            }

            if (newBuilding == null)
            {
                Debug.LogError("[RemodelingSystem] Failed to create building instance.");
                return null;
            }

            newBuilding.transform.SetParent(targetHouse.transform);

            // 상태 설정 (하우스에는 아직 추가하지 않음)
            selectedBuilding = newBuilding;
            originalPosition = spawnPosition;
            originalRotation = Quaternion.identity;
            isNewlyAddedBuilding = true;

            SetPhase(RemodelingPhase.Add);
            buildingSystem.Events.RaiseRemodelingBuildingSelected(newBuilding);

            Debug.Log($"[RemodelingSystem] Started placing new building: {newBuilding.name}");

            return newBuilding;
        }

        /// <summary>
        /// 드래그 시작 알림 (컨트롤러에서 호출)
        /// </summary>
        public void NotifyDragStarted()
        {
            if (selectedBuilding == null) return;

            SetPhase(RemodelingPhase.Move);

            buildingSystem.Events.RaiseRemodelingDragStarted();
        }

        /// <summary>
        /// 드래그 종료 알림 (컨트롤러에서 호출)
        /// </summary>
        public void NotifyDragEnded()
        {
            buildingSystem.Events.RaiseRemodelingDragEnded();
        }

        /// <summary>
        /// 배치 확정
        /// </summary>
        public bool TryConfirmPlacement()
        {
            if (selectedBuilding == null)
            {
                return false;
            }

            // 유효한 위치인지 체크
            if (!isValidPlacement)
            {
                switch (collisionResponse)
                {
                    case CollisionResponseType.ResetPosition:
                        CancelPlacement();
                        return false;

                    case CollisionResponseType.SwapTarget:
                        if (targetBuilding != null && targetHouse != null)
                        {
                            // 새로 추가된 빌딩인 경우 하우스에 추가
                            if (isNewlyAddedBuilding)
                            {
                                targetHouse.AddBuilding(selectedBuilding);
                                Debug.Log($"[RemodelingSystem] Added new building '{selectedBuilding.name}' to {targetHouse.name} (via swap)");
                                isNewlyAddedBuilding = false;
                            }

                            // 충돌 대상 빌딩을 선택
                            Building swapTarget = targetBuilding;
                            buildingSystem.Events.RaiseRemodelingBuildingModified(targetHouse, selectedBuilding);

                            selectedBuilding = swapTarget;
                            originalPosition = swapTarget.transform.position;
                            originalRotation = swapTarget.transform.rotation;
                            isNewlyAddedBuilding = false;

                            SetPhase(RemodelingPhase.Move);
                            buildingSystem.Events.RaiseRemodelingBuildingSelected(swapTarget);
                        }
                        return false;
                }
                return false;
            }

            // 배치 성공
            if (isNewlyAddedBuilding && targetHouse != null)
            {
                targetHouse.AddBuilding(selectedBuilding);
                Debug.Log($"[RemodelingSystem] Successfully added new building '{selectedBuilding.name}' to {targetHouse.name}");
                buildingSystem.Events.RaiseRemodelingBuildingAdded(targetHouse, selectedBuilding);
            }

            buildingSystem.Events.RaiseRemodelingBuildingModified(targetHouse, selectedBuilding);

            Building placedBuilding = selectedBuilding;
            selectedBuilding = null;
            targetBuilding = null;
            isNewlyAddedBuilding = false;

            SetPhase(RemodelingPhase.Inspect);
            buildingSystem.Events.RaiseRemodelingBuildingDeselected();

            Debug.Log($"[RemodelingSystem] Confirmed placement of {placedBuilding.name}");
            return true;
        }

        /// <summary>
        /// 배치 취소 (새 빌딩은 카탈로그에 반환, 기존 빌딩은 원래 위치로)
        /// </summary>
        public void CancelPlacement()
        {
            if (selectedBuilding == null) return;

            if (isNewlyAddedBuilding)
            {
                // 새로 추가된 빌딩인 경우 카탈로그에 반환
                if (buildingCatalog != null)
                {
                    buildingCatalog.ReturnBuilding(selectedBuilding);
                    Debug.Log($"[RemodelingSystem] Cancelled adding new building '{selectedBuilding.name}' and returned to catalog.");
                }
                else
                {
                    Destroy(selectedBuilding.gameObject);
                    Debug.LogWarning($"[RemodelingSystem] BuildingCatalog is null. Destroyed building '{selectedBuilding.name}' instead.");
                }
            }
            else
            {
                // 기존 빌딩인 경우 원래 위치로 복원
                selectedBuilding.transform.position = originalPosition;
                selectedBuilding.transform.rotation = originalRotation;
                Debug.Log($"[RemodelingSystem] Restored {selectedBuilding.name} to original position");
            }

            selectedBuilding = null;
            targetBuilding = null;
            isNewlyAddedBuilding = false;

            SetPhase(RemodelingPhase.Inspect);
            buildingSystem.Events.RaiseRemodelingBuildingDeselected();
        }

        /// <summary>
        /// 빌딩 위치 업데이트 (드래그 중 호출)
        /// </summary>
        public void UpdateBuildingPosition(Vector3 worldPosition)
        {
            if (selectedBuilding == null || targetHouse == null) return;

            // 하우스 영역 내부로 제한
            if (clampPlacementPosition)
            {
                worldPosition = ClampToHouseBounds(worldPosition);
            }

            selectedBuilding.transform.position = worldPosition;
        }

        /// <summary>
        /// 빌딩 회전
        /// </summary>
        public void RotateBuilding(float angle)
        {
            if (selectedBuilding != null && selectedBuilding.AllowManualRotation)
            {
                selectedBuilding.transform.Rotate(Vector3.up, angle);
            }
        }

        /// <summary>
        /// 배치 유효성 검사 수행 및 결과 반환
        /// </summary>
        public bool ValidateCurrentPlacement(out PlacementInvalidReason invalidReason)
        {
            bool wasValid = isValidPlacement;
            isValidPlacement = ValidatePlacement(selectedBuilding, out invalidReason);

            // 배치 상태가 변경되었을 때 이벤트 발생
            if (wasValid != isValidPlacement)
            {
                if (!isValidPlacement)
                {
                    buildingSystem.Events.RaiseRemodelingPlacementInvalid(selectedBuilding, invalidReason);
                }
                else
                {
                    buildingSystem.Events.RaiseRemodelingPlacementValid(selectedBuilding);
                }
            }

            return isValidPlacement;
        }

        /// <summary>
        /// 커스텀 룰 적용. 결과는 LastRuleResults 프로퍼티로 확인 가능.
        /// </summary>
        public void ApplyCustomRules(Vector3 position, ref PlacementInvalidReason invalidReason, ref bool validPlacement)
        {
            lastRuleResults.Clear(); // 캐시 리스트 재사용 (GC 부담 없음)

            if (selectedBuilding == null || targetHouse == null) return;

            foreach (IRemodelingRule rule in rules)
            {
                var result = rule.ControlBuilding(selectedBuilding, targetHouse, position);
                lastRuleResults.Add(result); // 모든 결과 저장 (Skipped 포함)

                // 스킵된 룰은 로직에 영향 없음
                if (result.Status == RemodelingRuleStatus.Skipped)
                {
                    continue;
                }

                string ruleName = rule.GetType().Name;

                if (result.Status == RemodelingRuleStatus.Failed)
                {
                    // enforce가 true일 때만 invalid 처리
                    if (result.Enforce)
                    {
                        invalidReason |= PlacementInvalidReason.CustomRule;
                        validPlacement = false;
                        Debug.LogWarning($"[RemodelingSystem] Placement invalidated by rule: {ruleName}");
                    }
                }
                else if (result.Status == RemodelingRuleStatus.Succeeded && result.Enforce)
                {
                    // 룰이 성공이고 enforce가 true일 때 강제로 valid 처리
                    validPlacement = true;
                }
            }

            isValidPlacement = validPlacement;
        }
        #endregion

        #region Building Management
        /// <summary>
        /// 빌딩 제거
        /// </summary>
        public bool RemoveBuilding(Building building)
        {
            if (building == null || targetHouse == null)
            {
                Debug.LogWarning("[RemodelingSystem] Cannot remove building: Building or House is null.");
                return false;
            }

            // House의 Buildings 목록에 없으면 제거 불가
            if (!targetHouse.Buildings.Contains(building))
            {
                Debug.LogWarning($"[RemodelingSystem] Cannot remove building: '{building.name}' does not belong to {targetHouse.name}");
                return false;
            }

            // 필수 건물인지 확인
            if (IsRequiredBuilding(building))
            {
                Debug.LogWarning($"[RemodelingSystem] Cannot remove building: '{building.name}' is a required building for {targetHouse.name}");
                return false;
            }

            // House에서 Building 제거
            targetHouse.RemoveBuilding(building);

            // Building을 카탈로그에 반환
            if (buildingCatalog != null)
            {
                buildingCatalog.ReturnBuilding(building);
            }
            else
            {
                Destroy(building.gameObject);
            }

            buildingSystem.Events.RaiseRemodelingBuildingRemoved(targetHouse, building);
            Debug.Log($"[RemodelingSystem] Removed building '{building.name}' from {targetHouse.name}");

            return true;
        }

        /// <summary>
        /// Building이 House의 필수 건물인지 확인
        /// 필수 건물 타입이더라도 같은 타입이 2개 이상 있으면 제거 가능
        /// </summary>
        public bool IsRequiredBuilding(Building building)
        {
            if (building == null || targetHouse == null || building.StatusData == null)
                return false;

            BuildingTypeData buildingType = building.StatusData.BuildingType;
            if (buildingType == null)
                return false;

            // House의 RequiredBuildingTypes에 포함되어 있는지 확인
            if (!targetHouse.RequiredBuildingTypes.Contains(buildingType))
                return false;

            // 같은 타입의 건물이 몇 개 있는지 확인
            int count = 0;
            foreach (var b in targetHouse.Buildings)
            {
                if (b != null && b.StatusData != null &&
                    b.StatusData.BuildingType == buildingType)
                {
                    count++;
                }
            }

            // 마지막 하나만 남은 경우에만 제거 불가 (true 반환)
            return count <= 1;
        }
        #endregion

        #region Validation
        /// <summary>
        /// Building 배치가 유효한지 검사
        /// </summary>
        public bool ValidatePlacement(Building building, out PlacementInvalidReason invalidReason)
        {
            invalidReason = PlacementInvalidReason.None;

            if (building == null || targetHouse == null)
            {
                return false;
            }

            // 하우스 영역 내부인지 확인
            if (!IsWithinHouseBounds(building.transform.position))
            {
                invalidReason |= PlacementInvalidReason.OutOfBounds;
            }

            // 충돌 검사
            if (shouldCheckCollision)
            {
                Building collidingBuilding;
                if (CheckPlacementCollision(building, out collidingBuilding))
                {
                    invalidReason |= PlacementInvalidReason.Collision;
                    targetBuilding = collidingBuilding;
                }
                else
                {
                    targetBuilding = null;
                }
            }

            return invalidReason == PlacementInvalidReason.None;
        }

        /// <summary>
        /// 위치가 하우스 영역 내부인지 검사 (2D Point-in-Polygon)
        /// </summary>
        public bool IsWithinHouseBounds(Vector3 position)
        {
            if (targetHouse == null || targetHouse.BoundaryPlot == null ||
                targetHouse.BoundaryPlot.LineList == null || targetHouse.BoundaryPlot.LineList.Count == 0)
            {
                return true; // 경계가 없으면 허용
            }

            Vector2 point = new Vector2(position.x, position.z);

            // 모든 아웃라인을 탐색
            int intersectCount = 0;
            foreach (List<Vector3> outline in targetHouse.BoundaryPlot.LineList)
            {
                if (outline == null || outline.Count < 2)
                    continue;

                // 2D Point-in-Polygon 알고리즘
                for (int i = 0; i < outline.Count - 1; i++)
                {
                    Vector2 v1 = new Vector2(outline[i].x, outline[i].z);
                    Vector2 v2 = new Vector2(outline[i + 1].x, outline[i + 1].z);

                    if (RayIntersectsSegment(point, v1, v2))
                    {
                        intersectCount++;
                    }
                }
            }

            // 홀수 번 교차하면 내부
            return (intersectCount % 2) == 1;
        }

        /// <summary>
        /// 위치를 하우스 경계 내부로 클램핑
        /// </summary>
        public Vector3 ClampToHouseBounds(Vector3 position)
        {
            if (targetHouse == null || targetHouse.BoundaryPlot == null ||
                targetHouse.BoundaryPlot.LineList == null || targetHouse.BoundaryPlot.LineList.Count == 0)
            {
                return position;
            }

            // 이미 내부에 있으면 그대로 반환
            if (IsWithinHouseBounds(position))
            {
                return position;
            }

            // 경계 내부로 강제 이동: 모든 아웃라인에서 가장 가까운 경계 지점 찾기
            Vector3 closestPoint = position;
            float minDistance = float.MaxValue;

            foreach (var outline in targetHouse.BoundaryPlot.LineList)
            {
                if (outline == null || outline.Count < 2)
                    continue;

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
            Vector3 center = targetHouse.transform.position;
            Vector3 direction = (center - closestPoint).normalized;
            closestPoint += direction * 0.5f;

            // Y 좌표는 원래 위치 유지
            closestPoint.y = position.y;

            return closestPoint;
        }

        /// <summary>
        /// 빌딩 배치 시 충돌 검사
        /// </summary>
        public bool CheckPlacementCollision(Building building, out Building collidingBuilding)
        {
            collidingBuilding = null;

            if (building == null || targetHouse == null)
            {
                return false;
            }

            Collider buildingCollider = building.GetComponent<Collider>();
            if (buildingCollider == null)
            {
                return false;
            }

            Physics.SyncTransforms();

            Bounds bounds = buildingCollider.bounds;
            Vector3 center = bounds.center;
            Vector3 halfExtents = bounds.extents;

            Collider[] overlappingColliders = Physics.OverlapBox(
                center,
                halfExtents,
                building.transform.rotation,
                collisionCheckLayers
            );

            // 바운더리 콜라이더 캐싱
            MeshCollider houseBoundaryCollider = targetHouse.GetBoundaryCollider();

            foreach (Collider otherCollider in overlappingColliders)
            {
                // 자기 자신은 제외
                if (otherCollider == buildingCollider)
                {
                    continue;
                }

                // 현재 하우스의 바운더리 콜라이더 제외
                if (houseBoundaryCollider != null && otherCollider == houseBoundaryCollider)
                {
                    continue;
                }

                Building otherBuilding = otherCollider.GetComponentInParent<Building>();
                if (otherBuilding == building)
                {
                    continue;
                }

                // 같은 하우스 내의 빌딩만 타겟으로 지정
                if (otherBuilding != null && targetHouse.Buildings.Contains(otherBuilding))
                {
                    collidingBuilding = otherBuilding;
                }

                return true;
            }

            return false;
        }
        #endregion

        #region State Management
        /// <summary>
        /// Phase 내부의 세부 상태를 전환한다.
        /// </summary>
        public void SetState(IRemodelingState newState)
        {
            if (currentState == newState) return;

            IRemodelingState oldState = currentState;
            currentState?.OnExit(this);
            currentState = newState;
            currentState?.OnEnter(this);

            buildingSystem.Events.RaiseRemodelingStateChanged(oldState, newState);
            Debug.Log($"[RemodelingSystem] State changed: {oldState?.StateName ?? "null"} -> {newState?.StateName ?? "null"}");
        }

        private void SetPhase(RemodelingPhase newPhase)
        {
            if (currentPhase == newPhase) return;

            RemodelingPhase oldPhase = currentPhase;
            currentPhase = newPhase;

            buildingSystem.Events.RaiseRemodelingPhaseChanged(oldPhase, newPhase);
            Debug.Log($"[RemodelingSystem] Phase changed: {oldPhase} -> {newPhase}");
        }
        #endregion

        #region Backup/Restore
        private void BackupHouseState()
        {
            buildingBackup.Clear();

            if (targetHouse == null || targetHouse.Buildings == null)
            {
                return;
            }

            foreach (Building building in targetHouse.Buildings)
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

            Debug.Log($"[RemodelingSystem] Backed up {buildingBackup.Count} buildings");
        }

        /// <summary>
        /// 백업된 상태로 빌딩들 복원 (CancelSession 이벤트 핸들러에서 호출)
        /// </summary>
        public void RestoreHouseState()
        {
            foreach (BuildingSnapshot snapshot in buildingBackup)
            {
                if (snapshot.building == null) continue;

                snapshot.building.transform.position = snapshot.position;
                snapshot.building.transform.rotation = snapshot.rotation;
                snapshot.building.SetStageIndex(snapshot.stageIndex);
            }

            Debug.Log($"[RemodelingSystem] Restored {buildingBackup.Count} buildings to original state.");
        }

        private void ClearBackup()
        {
            buildingBackup.Clear();
        }

        /// <summary>
        /// 변경된 빌딩들의 건설 단계를 0으로 초기화 (CompleteSession 이벤트 핸들러에서 호출)
        /// </summary>
        public void ResetModifiedBuildingsToStageZero(House house)
        {
            int modifiedCount = 0;
            int unchangedCount = 0;

            foreach (BuildingSnapshot snapshot in buildingBackup)
            {
                if (snapshot.building == null) continue;

                // 위치나 회전이 변경되었는지 확인
                bool positionChanged = Vector3.Distance(snapshot.building.transform.position, snapshot.position) > 0.01f;
                bool rotationChanged = Quaternion.Angle(snapshot.building.transform.rotation, snapshot.rotation) > 0.1f;

                if (positionChanged || rotationChanged)
                {
                    // 변경된 빌딩 -> stage 0
                    snapshot.building.SetStageIndex(0);
                    modifiedCount++;
                    Debug.Log($"[RemodelingSystem] Reset {snapshot.building.name} to construction stage 0 (modified)");
                }
                else
                {
                    // 변경되지 않은 빌딩 -> 백업된 원래 stage로 복원
                    snapshot.building.SetStageIndex(snapshot.stageIndex);
                    unchangedCount++;
                    Debug.Log($"[RemodelingSystem] Restored {snapshot.building.name} to original stage {snapshot.stageIndex} (unchanged)");
                }
            }

            // 새로 추가된 빌딩들도 0단계로 설정
            if (house != null && house.Buildings != null)
            {
                foreach (Building building in house.Buildings)
                {
                    if (building == null) continue;

                    // 백업에 없는 빌딩 = 새로 추가된 빌딩
                    bool isNewBuilding = !buildingBackup.Exists(s => s.building == building);
                    if (isNewBuilding)
                    {
                        building.SetStageIndex(0);
                        modifiedCount++;
                        Debug.Log($"[RemodelingSystem] Reset {building.name} to construction stage 0 (newly added)");
                    }
                }
            }

            Debug.Log($"[RemodelingSystem] Applied remodeling: {modifiedCount} modified/new buildings to stage 0, {unchangedCount} unchanged buildings restored.");
        }
        #endregion

        #region Utility
        private bool RayIntersectsSegment(Vector2 point, Vector2 v1, Vector2 v2)
        {
            if ((v1.y > point.y) == (v2.y > point.y))
            {
                return false;
            }

            float intersectX = v1.x + (point.y - v1.y) * (v2.x - v1.x) / (v2.y - v1.y);
            return intersectX > point.x;
        }

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
        #endregion
    }
}
