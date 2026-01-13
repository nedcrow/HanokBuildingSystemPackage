using UnityEngine;

namespace HanokBuildingSystem
{
    public interface IRemodelingRule
    {
        bool ControlBuilding(Building building, House house, Vector3 pos, out string reason, out bool enforce);
    }
}
