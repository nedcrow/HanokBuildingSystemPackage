using System.Collections.Generic;
using UnityEngine;

namespace HanokBuildingSystem
{
    public class HBSRemodelingRule_Wall : MonoBehaviour, IRemodelingRule
    {
        public RemodelingRuleResult ControlBuilding(Building building, House house, Vector3 pos)
        {
            // WallBuilding 타입이 아니면 스킵
            if (building is not WallBuilding)
            {
                return RemodelingRuleResult.Skip("Not a WallBuilding");
            }

            MarkerComponent[] markers = house.GetComponentsInChildren<MarkerComponent>();

            foreach(var marker in markers)
            {
                if(building.StatusData.BuildingType != marker.BuildingType) continue;
                building.transform.position = marker.transform.position;
            }

            return RemodelingRuleResult.Fail("Don't translate WallBuilding type", enforce: true);
        }
    }

}