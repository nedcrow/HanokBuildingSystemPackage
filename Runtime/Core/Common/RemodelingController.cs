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

        [Header("Raycast Settings")]
        [SerializeField] private LayerMask buildingLayerMask;
        [SerializeField] private float raycastDistance = 1000f;

        [Header("Collision Settings")]
        [SerializeField] private bool shouldCheckCollision = true;
        [SerializeField] private float radiusOfCollisionCheck = 1f;
        [SerializeField] private CollisionResponseType collisionResponse = CollisionResponseType.None;

        [Header("Visual Feedback")]
        [SerializeField] private Color validColor = new Color(0, 1, 0, 0.5f);
        [SerializeField] private Color invalidColor = new Color(1, 0, 0, 0.5f);

        // Current state
        private Building selectedBuilding;
        private Building targetBuilding;
        private House targetHouse;
        private bool isDragging = false;
        private Coroutine draggingCoroutine;
        private Vector3 originalPosition;
        private Quaternion originalRotation;

        // Mouse position from new Input System
        private Vector2 currentMousePosition;

        // Cached for performance
        private bool isValidPlacement = true;

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

            // 유효한 위치인지 확인
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
                targetHouse = null;

                return true;
            }
        }

        /// <summary>
        /// 현재 선택 취소 및 원래 위치로 복귀
        /// </summary>
        public void CancelSelection()
        {
            if (selectedBuilding != null)
            {
                selectedBuilding.transform.position = originalPosition;
                selectedBuilding.transform.rotation = originalRotation;
            }

            StopDragging();
            selectedBuilding = null;
            targetHouse = null;
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
        #endregion

        #region Building Selection & Dragging
        private void SelectBuilding(Building building, House house)
        {
            selectedBuilding = building;
            targetHouse = house;
            originalPosition = building.transform.position;
            originalRotation = building.transform.rotation;

            StartDragging();

            Debug.Log($"[RemodelingController] Building {building.name} selected for remodeling.");
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
                Vector3 worldPosition = ScreenToWorldPosition(currentMousePosition);

                // 하우스 영역 내부로 제한
                Vector3 clampedPosition = ClampToHouseBounds(worldPosition, targetHouse);
                selectedBuilding.transform.position = clampedPosition;

                // 배치 가능 여부 검사
                isValidPlacement = ValidatePlacement(selectedBuilding, targetHouse);

                // 시각적 피드백 (옵션)
                UpdateVisualFeedback(selectedBuilding, isValidPlacement);

                yield return null; // 다음 프레임까지 대기
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
        /// </summary>
        private bool IsWithinHouseBounds(Vector3 position, House house)
        {
            if (house == null || house.OutlineVertices == null || house.OutlineVertices.Count == 0)
            {
                return true; // 경계가 없으면 허용
            }

            // 첫 번째 외곽선 사용 (주 경계)
            List<Vector3> outline = house.OutlineVertices[0];
            if (outline == null || outline.Count < 3)
            {
                return true;
            }

            // 2D Point-in-Polygon 알고리즘 (Ray Casting)
            Vector2 point = new Vector2(position.x, position.z);
            int intersectCount = 0;

            for (int i = 0; i < outline.Count; i++)
            {
                Vector2 v1 = new Vector2(outline[i].x, outline[i].z);
                Vector2 v2 = new Vector2(outline[(i + 1) % outline.Count].x, outline[(i + 1) % outline.Count].z);

                if (RayIntersectsSegment(point, v1, v2))
                {
                    intersectCount++;
                }
            }

            // 홀수 번 교차하면 내부
            return (intersectCount % 2) == 1;
        }

        /// <summary>
        /// Ray Casting 알고리즘: 점에서 오른쪽으로 뻗은 광선이 선분과 교차하는지 검사
        /// </summary>
        private bool RayIntersectsSegment(Vector2 point, Vector2 v1, Vector2 v2)
        {
            // 선분이 점의 y 범위에 있는지 확인
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
        /// </summary>
        private Vector3 ClampToHouseBounds(Vector3 position, House house)
        {
            if (house == null || house.OutlineVertices == null || house.OutlineVertices.Count == 0)
            {
                return position;
            }

            // 이미 내부에 있으면 그대로 반환
            if (IsWithinHouseBounds(position, house))
            {
                return position;
            }

            // 경계 내부로 강제 이동: 가장 가까운 경계 지점 찾기
            List<Vector3> outline = house.OutlineVertices[0];
            if (outline == null || outline.Count < 3)
            {
                return position;
            }

            Vector3 closestPoint = position;
            float minDistance = float.MaxValue;

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
                return false;
            }

            Collider buildingCollider = building.GetComponent<Collider>();
            if (buildingCollider == null)
            {
                return false;
            }

            // 같은 하우스 내의 다른 Building들과 충돌 검사
            foreach (Building otherBuilding in targetHouse.Buildings)
            {
                if (otherBuilding == building)
                {
                    continue;
                }

                Collider otherCollider = otherBuilding.GetComponent<Collider>();
                if (otherCollider != null && buildingCollider.bounds.Intersects(otherCollider.bounds))
                {
                    targetBuilding = otherCollider.GetComponentInChildren<Building>();
                    return true; // 충돌 발생
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
                propertyBlock.SetColor("_Color", isValid ? validColor : invalidColor);
                renderer.SetPropertyBlock(propertyBlock);
            }
        }
        #endregion

        #region Debug
        private void OnDrawGizmosSelected()
        {
            if (targetHouse != null && targetHouse.OutlineVertices != null && targetHouse.OutlineVertices.Count > 0)
            {
                Gizmos.color = Color.yellow;
                List<Vector3> outline = targetHouse.OutlineVertices[0];

                for (int i = 0; i < outline.Count; i++)
                {
                    Vector3 v1 = outline[i];
                    Vector3 v2 = outline[(i + 1) % outline.Count];
                    Gizmos.DrawLine(v1, v2);
                }
            }

            if (selectedBuilding != null)
            {
                Gizmos.color = isValidPlacement ? Color.green : Color.red;
                Gizmos.DrawWireSphere(selectedBuilding.transform.position, radiusOfCollisionCheck);
            }
        }
        #endregion
    }
}
