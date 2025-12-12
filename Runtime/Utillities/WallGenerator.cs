using System.Collections.Generic;
using UnityEngine;

namespace HanokBuildingSystem
{
    /// <summary>
    /// Plot 경계를 따라 담장을 자동으로 생성하는 유틸리티
    /// BuildingMemberCatalog의 풀링 시스템을 사용하여 담장 GameObject를 배치합니다.
    /// </summary>
    public class WallGenerator : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField] private BuildingMemberCatalog memberCatalog;

        [Header("Wall Settings")]
        [SerializeField] private string wallPrefabName = "Dam_A_Center";
        [SerializeField] private string cornerPrefabName = "Dam_A_Corner";
        [SerializeField] private string endPrefabName = "Dam_A_End";

        [Header("Placement Settings")]
        [SerializeField] private float wallSegmentLength = 1.0f; // 담장 한 조각의 길이
        [SerializeField] private float heightOffset = 0f; // Y축 오프셋
        [SerializeField] private bool useCornerPieces = true;
        [SerializeField] private bool useEndPieces = true;

        private List<GameObject> generatedWalls = new List<GameObject>();

        private void Start()
        {
            if (memberCatalog == null)
            {
                memberCatalog = FindObjectOfType<BuildingMemberCatalog>();
                if (memberCatalog == null)
                {
                    Debug.LogError("[WallGenerator] BuildingMemberCatalog not found!");
                }
            }
        }

        /// <summary>
        /// Plot 경계를 따라 담장을 생성
        /// </summary>
        public List<GameObject> GenerateWallsForPlot(Plot plot, Transform parent = null)
        {
            if (plot == null || plot.LineList == null || plot.LineList.Count == 0)
            {
                Debug.LogWarning("[WallGenerator] Invalid plot provided.");
                return new List<GameObject>();
            }

            if (memberCatalog == null)
            {
                Debug.LogError("[WallGenerator] BuildingMemberCatalog is null!");
                return new List<GameObject>();
            }

            ClearGeneratedWalls();

            // 각 외곽선(outline)에 대해 담장 생성
            foreach (List<Vector3> outline in plot.LineList)
            {
                if (outline == null || outline.Count < 2)
                {
                    continue;
                }

                GenerateWallSegmentsForOutline(outline, parent);
            }

            Debug.Log($"[WallGenerator] Generated {generatedWalls.Count} wall pieces.");
            return new List<GameObject>(generatedWalls);
        }

        /// <summary>
        /// 하나의 외곽선에 대해 담장 세그먼트 생성
        /// </summary>
        private void GenerateWallSegmentsForOutline(List<Vector3> outline, Transform parent)
        {
            bool isClosed = Vector3.Distance(outline[0], outline[outline.Count - 1]) < 0.01f;
            int segmentCount = isClosed ? outline.Count - 1 : outline.Count - 1;

            for (int i = 0; i < segmentCount; i++)
            {
                Vector3 start = outline[i];
                Vector3 end = outline[(i + 1) % outline.Count];

                // 모서리 판정
                bool isCorner = false;
                if (useCornerPieces && i > 0 && i < segmentCount - 1)
                {
                    Vector3 prev = outline[i - 1];
                    Vector3 next = outline[(i + 2) % outline.Count];
                    isCorner = IsCorner(prev, start, end, next);
                }

                // 시작/끝 판정
                bool isStart = useEndPieces && !isClosed && i == 0;
                bool isEnd = useEndPieces && !isClosed && i == segmentCount - 1;

                if (isCorner)
                {
                    PlaceCornerPiece(start, end, parent);
                }
                else if (isStart || isEnd)
                {
                    PlaceEndPiece(start, end, parent, isStart);
                }
                else
                {
                    PlaceWallSegment(start, end, parent);
                }
            }
        }

        /// <summary>
        /// start에서 end까지 직선 구간에 담장 배치
        /// </summary>
        private void PlaceWallSegment(Vector3 start, Vector3 end, Transform parent)
        {
            float distance = Vector3.Distance(start, end);
            if (distance < 0.01f) return;

            Vector3 direction = (end - start).normalized;
            Quaternion rotation = Quaternion.LookRotation(direction);

            int pieceCount = Mathf.Max(1, Mathf.CeilToInt(distance / wallSegmentLength));
            float actualSegmentLength = distance / pieceCount;

            for (int i = 0; i < pieceCount; i++)
            {
                Vector3 position = start + direction * (actualSegmentLength * i + actualSegmentLength * 0.5f);
                position.y += heightOffset;

                GameObject wallPiece = memberCatalog.GetMemberByName(wallPrefabName, position, rotation);
                if (wallPiece != null)
                {
                    if (parent != null)
                    {
                        wallPiece.transform.SetParent(parent);
                    }
                    generatedWalls.Add(wallPiece);
                }
            }
        }

        /// <summary>
        /// 코너 지점에 코너 조각 배치
        /// </summary>
        private void PlaceCornerPiece(Vector3 cornerPos, Vector3 nextPos, Transform parent)
        {
            Vector3 direction = (nextPos - cornerPos).normalized;
            Quaternion rotation = Quaternion.LookRotation(direction);
            Vector3 position = cornerPos;
            position.y += heightOffset;

            GameObject cornerPiece = memberCatalog.GetMemberByName(cornerPrefabName, position, rotation);
            if (cornerPiece != null)
            {
                if (parent != null)
                {
                    cornerPiece.transform.SetParent(parent);
                }
                generatedWalls.Add(cornerPiece);
            }
        }

        /// <summary>
        /// 시작/끝 지점에 엔드 조각 배치
        /// </summary>
        private void PlaceEndPiece(Vector3 pos, Vector3 directionPoint, Transform parent, bool isStart)
        {
            Vector3 direction = isStart ? (directionPoint - pos).normalized : (pos - directionPoint).normalized;
            Quaternion rotation = Quaternion.LookRotation(direction);
            Vector3 position = pos;
            position.y += heightOffset;

            GameObject endPiece = memberCatalog.GetMemberByName(endPrefabName, position, rotation);
            if (endPiece != null)
            {
                if (parent != null)
                {
                    endPiece.transform.SetParent(parent);
                }
                generatedWalls.Add(endPiece);
            }
        }

        /// <summary>
        /// 세 점이 모서리를 이루는지 판단
        /// </summary>
        private bool IsCorner(Vector3 prev, Vector3 current, Vector3 next, Vector3 afterNext)
        {
            Vector3 dir1 = (current - prev).normalized;
            Vector3 dir2 = (next - current).normalized;

            float angle = Vector3.Angle(dir1, dir2);
            return angle > 30f; // 30도 이상 꺾이면 코너로 간주
        }

        /// <summary>
        /// 생성된 모든 담장을 제거하고 풀로 반환
        /// </summary>
        public void ClearGeneratedWalls()
        {
            if (memberCatalog != null)
            {
                foreach (GameObject wall in generatedWalls)
                {
                    if (wall != null)
                    {
                        memberCatalog.ReturnMember(wall);
                    }
                }
            }

            generatedWalls.Clear();
        }

        /// <summary>
        /// 특정 담장 조각들을 제거
        /// </summary>
        public void RemoveWalls(List<GameObject> wallsToRemove)
        {
            if (memberCatalog == null || wallsToRemove == null) return;

            foreach (GameObject wall in wallsToRemove)
            {
                if (wall != null && generatedWalls.Contains(wall))
                {
                    memberCatalog.ReturnMember(wall);
                    generatedWalls.Remove(wall);
                }
            }
        }

        private void OnDestroy()
        {
            ClearGeneratedWalls();
        }

#if UNITY_EDITOR
        [Header("Debug")]
        [SerializeField] private bool showDebugGizmos = true;
        [SerializeField] private Color gizmoColor = Color.cyan;

        private void OnDrawGizmos()
        {
            if (!showDebugGizmos) return;

            Gizmos.color = gizmoColor;
            foreach (GameObject wall in generatedWalls)
            {
                if (wall != null)
                {
                    Gizmos.DrawWireCube(wall.transform.position, Vector3.one * 0.5f);
                }
            }
        }
#endif
    }
}
