using System.Collections.Generic;
using UnityEngine;

namespace HanokBuildingSystem
{
    public enum DoorType
    {
        SwingDoor,
        SlidingDoor,
        FoldingDoor,
        RevolvingDoor
    }
    public class DoorBuilding : Building
    {
        [Header("Building member for Door")]
        [SerializeField] private List<GameObject> doors;

        [Header("Settings")]
        [SerializeField] private DoorType doortype = DoorType.SwingDoor;

        private GameObject lastHiddenWallPoint;
        public List<GameObject> Doors => doors;
        public DoorType Ddoortype => doortype;

        /// <summary>
        /// 벽이 있으면 가장 가까운 '벽 포인트(벽 오브젝트 위치)'로 스냅.
        /// 벽이 없으면 Plot 아웃라인 선분 위 최근접 점으로 스냅.
        /// 스냅 후 라인에 직교하도록 회전.
        /// </summary>
        public void SnapToClosestOutlinePoint(Plot plot, Vector3? targetPosition = null)
        {
            if (plot == null || plot.LineList == null)
            {
                Debug.LogWarning("[DoorBuilding] Cannot snap: plot is invalid");
                return;
            }

            Vector3 searchPos = targetPosition ?? transform.position;

            // 1) WallBuilding 찾기 (Building 리스트가 List<Building>이라면 캐스팅이 더 명확)
            House parentHouse = GetComponentInParent<House>();
            WallBuilding wall = null;

            if (parentHouse != null && parentHouse.Buildings != null)
            {
                foreach (var b in parentHouse.Buildings)
                {
                    if (b is WallBuilding w) { wall = w; break; }
                }
            }

            // best 후보
            Vector3 bestPoint = searchPos;
            Vector3 bestA = Vector3.zero;
            Vector3 bestB = Vector3.zero;
            float bestSqrDist = float.MaxValue;
            bool found = false;

            // 2) 벽이 있으면: "벽 포인트 위치"로 스냅 (선분 X)
            if (wall != null && wall.Walls != null && wall.Walls.Count > 0)
            {
                int bestIndex = -1;

                for (int i = 0; i < wall.Walls.Count; i++)
                {
                    var t = wall.Walls[i];
                    if (t == null) continue;

                    Vector3 p = t.transform.position;
                    float sqrDist = (searchPos - p).sqrMagnitude;

                    if (sqrDist < bestSqrDist)
                    {
                        bestSqrDist = sqrDist;
                        bestPoint = p;
                        bestIndex = i;
                        found = true;
                    }
                }

                if (!found)
                {
                    Debug.LogWarning("[DoorBuilding] Cannot snap: no valid wall points found");
                    return;
                }

                // 이전 벽 복원 및 타겟 벽 숨김
                RestoreLastHiddenWallPoint();
                var bestWallPoint = wall.Walls[bestIndex];
                if (bestWallPoint != null)
                    HideWallPoint(bestWallPoint.gameObject);

                // 회전 기준 세그먼트 선택: bestIndex의 이웃으로 방향 잡기
                // 우선순위: (bestIndex-1, bestIndex) 또는 (bestIndex, bestIndex+1)
                if (wall.Walls.Count >= 2)
                {
                    int prev = bestIndex - 1;
                    int next = bestIndex + 1;

                    if (prev >= 0 && wall.Walls[prev] != null)
                    {
                        bestA = wall.Walls[prev].transform.position;
                        bestB = wall.Walls[bestIndex].transform.position;
                    }
                    else if (next < wall.Walls.Count && wall.Walls[next] != null)
                    {
                        bestA = wall.Walls[bestIndex].transform.position;
                        bestB = wall.Walls[next].transform.position;
                    }
                    else
                    {
                        // 이웃을 못 잡으면 회전은 스킵 가능
                        bestA = bestPoint;
                        bestB = bestPoint;
                    }
                }
            }
            else
            {
                // 3) 벽이 없으면: Plot 아웃라인 선분 위 최근접 점
                foreach (var line in plot.LineList)
                {
                    if (line == null || line.Count < 2) continue;

                    for (int i = 0; i < line.Count - 1; i++)
                    {
                        Vector3 a = line[i];
                        Vector3 b = line[i + 1];

                        Vector3 candidate = GetClosestPointOnSegment(searchPos, a, b);
                        float sqrDist = (searchPos - candidate).sqrMagnitude;

                        if (sqrDist < bestSqrDist)
                        {
                            bestSqrDist = sqrDist;
                            bestPoint = candidate;
                            bestA = a;
                            bestB = b;
                            found = true;
                        }
                    }
                }

                if (!found)
                {
                    Debug.LogWarning("[DoorBuilding] Cannot snap: no valid outline segments found");
                    return;
                }
            }

            // 4) 스냅 적용: y는 기존 높이 유지
            bestPoint.y = searchPos.y;
            transform.position = bestPoint;

            // 5) 회전: 라인(혹은 wall neighbor) 방향에 직교
            Vector3 dir = bestB - bestA;
            dir.y = 0f; // 수평 회전 기준이면 안전

            if (dir.sqrMagnitude > Mathf.Epsilon)
            {
                dir.Normalize();
                Vector3 outwardNormal = Vector3.Cross(Vector3.up, dir).normalized;
                transform.rotation = Quaternion.LookRotation(outwardNormal);
            }
        }

        /// <summary>
        /// 점에서 선분까지 가장 가까운 점 계산
        /// </summary>
        private Vector3 GetClosestPointOnSegment(Vector3 point, Vector3 a, Vector3 b)
        {
            Vector3 ab = b - a;
            float abSqrMag = ab.sqrMagnitude;

            if (abSqrMag <= Mathf.Epsilon)
                return a;

            float t = Vector3.Dot(point - a, ab) / abSqrMag;
            t = Mathf.Clamp01(t);
            return a + ab * t;
        }

        private void RestoreLastHiddenWallPoint()
        {
            if (lastHiddenWallPoint != null)
            {
                lastHiddenWallPoint.SetActive(true);
                lastHiddenWallPoint = null;
            }
        }

        private void HideWallPoint(GameObject wallPointGo)
        {
            if (wallPointGo == null) return;

            wallPointGo.SetActive(false);
            lastHiddenWallPoint = wallPointGo;
        }


        public override void ShowModelBuilding(Plot plot, Transform parent = null)
        {
            base.ShowModelBuilding(plot, transform);

            if (plot != null)
            {
                SnapToClosestOutlinePoint(plot);
            }

            // 완성 단계의 모습
        }
    }
}
