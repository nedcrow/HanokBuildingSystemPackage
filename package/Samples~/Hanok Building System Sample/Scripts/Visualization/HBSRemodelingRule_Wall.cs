using System.Collections.Generic;
using UnityEngine;

namespace HanokBuildingSystem
{
    public class HBSRemodelingRule_Wall : MonoBehaviour, IRemodelingRule
    {
        public bool ControlBuilding(Building building, House house, Vector3 pos, out string reason, out bool enforce)
        {
            enforce = true;
            if (building is WallBuilding)
            {
                MarkerComponent[] markers = house.GetComponentsInChildren<MarkerComponent>();

                foreach(var marker in markers)
                {
                    if(building.StatusData.BuildingType != marker.BuildingType) continue;
                    building.transform.position = marker.transform.position;
                }

                reason = "Don't translate WallBuilding type";
                return false;
            }

            reason = "success";

            return true;
        }
    }

}