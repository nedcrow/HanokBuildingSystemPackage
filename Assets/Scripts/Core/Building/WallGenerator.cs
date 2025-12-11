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

        [Header("Placement Settings")]
        [SerializeField] private float heightOffset = 0f; // Y축 오프셋
        [Min(0)]
        [SerializeField] private int adjustmentCount = 1;
        [SerializeField] private bool useCornerPieces = true;
        [SerializeField] private bool useEndPieces = true;

        private List<GameObject> generatedWalls = new List<GameObject>();

        private void Start()
        {
            if (memberCatalog == null)
            {
                memberCatalog = HanokBuildingSystem.Instance.BuildingMemberCatalog;
                if (memberCatalog == null)
                {
                    memberCatalog = FindFirstObjectByType<BuildingMemberCatalog>();
                }

                if (memberCatalog == null)
                {
                    Debug.LogError("[WallGenerator] BuildingMemberCatalog not found!");
                }
            }
        }

        /// <summary>
        /// Plot 경계를 따라 담장을 생성
        /// </summary>
        public List<GameObject> GenerateWallsForPlot(Plot plot, WallBuilding wallBuilding)
        {
            if (plot == null || plot.LineList == null || plot.LineList.Count == 0)
            {
                Debug.LogWarning("[WallGenerator] Invalid plot provided.");
                return new List<GameObject>();
            }

            if (wallBuilding == null)
            {
                Debug.LogWarning("[WallGenerator] WallBuilding parameter is null!");
                return new List<GameObject>();
            }

            if (memberCatalog == null)
            {
                Debug.LogError("[WallGenerator] BuildingMemberCatalog is null!");
                return new List<GameObject>();
            }

            // WallBuilding의 prefab 유효성 검증
            if (wallBuilding.WallCenter == null)
            {
                Debug.LogError("[WallGenerator] WallBuilding.WallCenter prefab is null!");
                return new List<GameObject>();
            }

            if (useCornerPieces && wallBuilding.WallCorner == null)
            {
                Debug.LogWarning("[WallGenerator] WallCorner is null, corners will not be generated.");
            }

            if (useEndPieces && wallBuilding.WallEnd == null)
            {
                Debug.LogWarning("[WallGenerator] WallEnd is null, end pieces will not be generated.");
            }

            ClearGeneratedWalls();

            // 벽 크기에 맞춘 plot 반복 보정
            List<List<Vector3>> lineList = plot.LineList;
            for(int i=0; i<adjustmentCount; i++)
            {
                lineList = ResampleOutlineSegments(plot.LineList, wallBuilding.WallSegmentLength);
            }

            // 각 외곽선(outline)에 대해 담장 생성
            foreach (List<Vector3> outline in lineList)
            {
                if (outline == null || outline.Count < 2)
                {
                    continue;
                }

                GenerateWallSegmentsForOutline(outline, wallBuilding);
            }
            
            return new List<GameObject>(generatedWalls);
        }

        /// <summary>
        /// 하나의 외곽선에 대해 담장 세그먼트 생성
        /// </summary>
        private void GenerateWallSegmentsForOutline(List<Vector3> outline, WallBuilding wallBuilding)
        {
            int vertexCount = outline.Count;

            // 각 정점 위치에 벽 배치
            for (int i = 0; i < vertexCount; i++)
            {
                Vector3 currentPos = outline[i];
                Vector3 nextPos = outline[(i + 1) % vertexCount];
                Vector3 direction = (nextPos - currentPos).normalized;

                // 외향 법선 방향으로 회전
                Vector3 outwardNormal = Vector3.Cross(Vector3.up, direction).normalized;
                Quaternion rotation = Quaternion.LookRotation(outwardNormal);

                // currentPos를 그대로 사용 (y만 heightOffset 추가)
                Vector3 position = currentPos;
                position.y += heightOffset;

                GameObject wallPiece;
                if (i == 0)
                {
                    // 시작점 - y축 180도 회전
                    Quaternion startRotation = rotation * Quaternion.Euler(0, 180, 0);
                    wallPiece = memberCatalog.GetMember(wallBuilding.WallEnd, position, startRotation);
                }
                else if (i == vertexCount - 1)
                {
                    // 마지막점
                    Quaternion endRotation = rotation * Quaternion.Euler(0, 180, 0);
                    wallPiece = memberCatalog.GetMember(wallBuilding.WallEnd, position, endRotation);
                }
                else
                {
                    // 중앙점
                    wallPiece = memberCatalog.GetMember(wallBuilding.WallCenter, position, rotation);
                }

                PlaceWallPiece(wallPiece, wallBuilding.transform);
            }
        }

        private void PlaceWallPiece(GameObject wallPiece, Transform parent)
        {
            if (wallPiece != null)
            {
                if (parent != null)
                {
                    wallPiece.transform.SetParent(parent);
                }
                generatedWalls.Add(wallPiece);
            }
        }

        private List<List<Vector3>> ResampleOutlineSegments(List<List<Vector3>> outlines, float unit)
        {
            var result = new List<List<Vector3>>();

            if (outlines == null || outlines.Count == 0 || unit <= 0f)
                return result;

            foreach (List<Vector3> outline in outlines)
            {
                if (outline == null || outline.Count < 2)
                {
                    result.Add(new List<Vector3>(outline ?? new List<Vector3>()));
                    continue;
                }

                int pointCount = outline.Count;
                int segmentCount = pointCount - 1;

                var segmentLengths = new float[segmentCount];
                float totalLength = 0f;

                // 각 선분 길이 계산
                for (int i = 0; i < segmentCount; i++)
                {
                    float len = Vector3.Distance(outline[i], outline[i + 1]);
                    segmentLengths[i] = len;
                    totalLength += len;
                }

                if (totalLength <= Mathf.Epsilon)
                {
                    result.Add(new List<Vector3>(outline));
                    continue;
                }

                // 벽(타일) 개수: ceil(총 길이 / unit)
                int tileCount = Mathf.Max(1, Mathf.CeilToInt(totalLength / unit));

                // 벽 센터 간 간격 (항상 unit 이하)
                float step = totalLength / tileCount;

                var newOutline = new List<Vector3>(tileCount);

                // 벽 센터는 각 구간의 중간 위치: step/2, 3/2*step, ...
                float currentDistance = step * 0.5f;

                for (int i = 0; i < tileCount; i++)
                {
                    Vector3 p = SamplePointOnOpenOutline(outline, segmentLengths, totalLength, currentDistance);
                    newOutline.Add(p);

                    currentDistance += step;
                }

                result.Add(newOutline);
            }

            return result;
        }

        // 열린 폴리라인(마지막 점과 첫 점이 연결되지 않음)에서
        // targetDistance 지점의 위치를 선형보간으로 구한다.
        private Vector3 SamplePointOnOpenOutline(List<Vector3> outline, float[] segmentLengths, float totalLength, float targetDistance)
        {
            if (outline == null || outline.Count == 0)
                return Vector3.zero;

            if (segmentLengths == null || segmentLengths.Length == 0)
                return outline[0];

            // 혹시 오차로 범위 밖으로 나가면 클램프
            targetDistance = Mathf.Clamp(targetDistance, 0f, totalLength);

            float accumulated = 0f;

            for (int i = 0; i < segmentLengths.Length; i++)
            {
                float segLen = segmentLengths[i];
                if (segLen <= Mathf.Epsilon)
                    continue;

                if (accumulated + segLen >= targetDistance)
                {
                    float t = (targetDistance - accumulated) / segLen;
                    return Vector3.Lerp(outline[i], outline[i + 1], t);
                }

                accumulated += segLen;
            }

            // 마지막을 살짝 넘는 경우엔 끝점 반환
            return outline[outline.Count - 1];
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
