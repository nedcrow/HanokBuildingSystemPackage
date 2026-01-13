using UnityEngine;

namespace HanokBuildingSystem
{
    public class HBSRemodelingRule_Door : MonoBehaviour, IRemodelingRule
    {
        public bool ControlBuilding(Building building, House house, Vector3 pos, out string reason, out bool enforce)
        {
            enforce = false;

            // DoorBuilding 타입 체크
            if (building is not DoorBuilding doorBuilding)
            {
                reason = "Only DoorBuilding can be placed on the outer boundary";
                return false;
            }

            // 커서 위치(pos)를 기준으로 가장 가까운 아웃라인 지점에 스냅
            doorBuilding.SnapToClosestOutlinePoint(house.BoundaryPlot, pos);

            reason = "Door snapped to closest outline point";
            return true;
        }
    }

}