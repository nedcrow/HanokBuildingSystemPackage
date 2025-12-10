using System.Collections.Generic;
using UnityEngine;

namespace HanokBuildingSystem
{
    [CreateAssetMenu(fileName = "BuildingCatalogDB", menuName = "HanokBuildingSystem/Building Catalog DB", order = 1)]
    public class BuildingCatalogDB : ScriptableObject
    {
        [SerializeField] private List<BuildingStatusData> buildings = new List<BuildingStatusData>();

        public List<BuildingStatusData> Buildings => buildings;

        public BuildingStatusData GetBuildingByType(BuildingType type)
        {
            return buildings.Find(b => b != null && b.BuildingType == type);
        }

        public BuildingStatusData GetBuildingByName(string name)
        {
            return buildings.Find(b => b != null && b.BuildingName == name);
        }
    }
}
