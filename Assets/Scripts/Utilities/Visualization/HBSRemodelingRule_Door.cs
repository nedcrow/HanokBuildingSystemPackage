using System.Collections.Generic;
using UnityEngine;

namespace HanokBuildingSystem
{
    public class HBSRemodelingRule_Door : MonoBehaviour, IRemodelingRule
    {
        public bool ControlBuilding(Building building, House house, Vector3 pos, out string reason, out bool enforce)
        {
            enforce = false;
            
            if (building is not DoorBuilding)
            {
                reason = "DoorBuilding can only be placed on the outer boundary";
                return false;
            }

            List<List<Vector3>> lineList = house.BoundaryPlot.LineList;

            Vector3 bestPoint = Vector3.zero;
            float bestDistance = float.MaxValue;

            foreach (var line in lineList)
            {
                if (line.Count < 2) continue;

                // 라인의 모든 세그먼트 순회
                for (int i = 0; i < line.Count - 1; i++)
                {
                    Vector3 a = line[i];
                    Vector3 b = line[i + 1];

                    Vector3 candidate = GetClosestPointOnSegment(pos, a, b);
                    float distance = Vector3.Distance(pos, candidate);

                    if (distance < bestDistance)
                    {
                        bestDistance = distance;
                        bestPoint = candidate;
                    }
                }
            }

            // 스냅 적용: y는 기존 높이 유지 가능
            bestPoint.y = pos.y;
            building.transform.position = bestPoint;

            reason = "success";

            return true;
        }

        Vector3 GetClosestPointOnSegment(Vector3 point, Vector3 a, Vector3 b)
        {
            Vector3 ab = b - a;
            float abSqrMag = ab.sqrMagnitude;

            if (abSqrMag == 0f)
            {
                // A와 B가 같은 점이면 그냥 그 점 반환
                return a;
            }

            // point에서 A까지 벡터
            Vector3 ap = point - a;

            // t = (AP · AB) / |AB|^2
            float t = Vector3.Dot(ap, ab) / abSqrMag;

            // 0~1 사이로 클램프 (선분 범위)
            t = Mathf.Clamp01(t);

            // 선분 위의 위치
            return a + ab * t;
        }
    }

}