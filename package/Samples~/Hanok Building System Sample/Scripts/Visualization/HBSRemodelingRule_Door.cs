using UnityEngine;

namespace HanokBuildingSystem
{
    public class HBSRemodelingRule_Door : MonoBehaviour, IRemodelingRule
    {
        public RemodelingRuleResult ControlBuilding(Building building, House house, Vector3 pos)
        {
            // DoorBuilding 타입이 아니면 스킵
            if (building is not DoorBuilding doorBuilding)
            {
                return RemodelingRuleResult.Skip("Not a DoorBuilding");
            }

            // 커서 위치(pos)를 기준으로 가장 가까운 아웃라인 지점에 스냅
            doorBuilding.SnapToClosestOutlinePoint(house.BoundaryPlot, pos);

            return RemodelingRuleResult.Success(enforce: true, reason: "Door snapped to closest outline point");
        }
    }

}