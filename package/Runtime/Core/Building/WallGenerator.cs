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
        /// Plot 경계를 따라 담장을 생성/업데이트
        /// 기존 벽을 재사용하고, 남는 벽은 풀로 반환, 부족한 벽은 새로 생성
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

            // 벽 크기에 맞춘 plot 반복 보정
            List<List<Vector3>> lineList = plot.LineList;
            for(int i=0; i<adjustmentCount; i++)
            {
                lineList = ResampleOutlineSegments(plot.LineList, wallBuilding.WallSegmentLength);
            }

            // 필요한 벽 위치/회전 정보 계산
            List<WallInfo> requiredWalls = new List<WallInfo>();
            foreach (List<Vector3> outline in lineList)
            {
                if (outline == null || outline.Count < 2) continue;
                CalculateWallsForOutline(outline, wallBuilding, requiredWalls);
            }

            // 기존 벽 재사용 및 업데이트
            UpdateWalls(wallBuilding, requiredWalls);

            return wallBuilding.Walls;
        }

        /// <summary>
        /// 벽 정보 (위치, 회전, 프리팹)
        /// </summary>
        private struct WallInfo
        {
            public Vector3 Position;
            public Quaternion Rotation;
            public GameObject Prefab;
        }

        /// <summary>
        /// 하나의 외곽선에 필요한 벽 정보를 계산
        /// </summary>
        private void CalculateWallsForOutline(List<Vector3> outline, WallBuilding wallBuilding, List<WallInfo> wallInfos)
        {
            int vertexCount = outline.Count;

            // 각 정점 위치에 필요한 벽 계산
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

                GameObject prefab;
                Quaternion finalRotation;

                if (i == 0)
                {
                    // 시작점 - y축 180도 회전
                    prefab = wallBuilding.WallEnd;
                    finalRotation = rotation * Quaternion.Euler(0, 180, 0);
                }
                else if (i == vertexCount - 1)
                {
                    // 마지막점
                    prefab = wallBuilding.WallEnd;
                    finalRotation = rotation * Quaternion.Euler(0, 180, 0);
                }
                else
                {
                    // 중앙점
                    prefab = wallBuilding.WallCenter;
                    finalRotation = rotation;
                }

                wallInfos.Add(new WallInfo
                {
                    Position = position,
                    Rotation = finalRotation,
                    Prefab = prefab
                });
            }
        }

        /// <summary>
        /// WallBuilding의 기존 벽을 재사용하고, 필요한 만큼 추가/제거
        /// </summary>
        private void UpdateWalls(WallBuilding wallBuilding, List<WallInfo> requiredWalls)
        {
            if (wallBuilding.Walls == null)
            {
                wallBuilding.Walls = new List<GameObject>();
            }

            int existingCount = wallBuilding.Walls.Count;
            int requiredCount = requiredWalls.Count;

            // 1. 기존 벽 재사용 (위치/회전 업데이트)
            int reuseCount = Mathf.Min(existingCount, requiredCount);
            for (int i = 0; i < reuseCount; i++)
            {
                GameObject wall = wallBuilding.Walls[i];
                WallInfo info = requiredWalls[i];

                if (wall != null)
                {
                    wall.transform.position = info.Position;
                    wall.transform.rotation = info.Rotation;
                    wall.SetActive(true);
                }
            }

            // 2. 남는 벽 풀로 반환
            if (existingCount > requiredCount)
            {
                for (int i = existingCount - 1; i >= requiredCount; i--)
                {
                    GameObject wall = wallBuilding.Walls[i];
                    if (wall != null)
                    {
                        wallBuilding.RemoveBuildingMember(wall);
                        memberCatalog.ReturnMember(wall);
                    }
                    wallBuilding.Walls.RemoveAt(i);
                }
            }

            // 3. 부족한 벽 새로 생성
            if (requiredCount > existingCount)
            {
                for (int i = existingCount; i < requiredCount; i++)
                {
                    WallInfo info = requiredWalls[i];
                    GameObject wall = memberCatalog.GetMember(info.Prefab, info.Position, info.Rotation);

                    if (wall != null)
                    {
                        wall.transform.SetParent(wallBuilding.transform);

                        // BuildingMember 컴포넌트 추가 (없을 경우)
                        BuildingMember member = wall.GetComponent<BuildingMember>();
                        if (member == null)
                        {
                            member = wall.AddComponent<BuildingMember>();
                        }

                        wallBuilding.Walls.Add(wall);
                        wallBuilding.AddBuildingMember(wall);
                    }
                }
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
    }
}
