using System.Collections.Generic;
using UnityEngine;

namespace HanokBuildingSystem
{
    /// <summary>
    /// Building 프리팹들의 인스턴스 풀링을 담당하는 카탈로그
    /// </summary>
    public class BuildingCatalog : CatalogBase<Building>
    {
        [Header("Building Prefabs")]
        [SerializeField] private List<GameObject> buildingPrefabs = new List<GameObject>();

        public List<GameObject> BuildingPrefabs => buildingPrefabs;

        public override List<GameObject> GetPrefabList() => buildingPrefabs;

        protected override bool ValidatePrefab(GameObject prefab)
        {
            return prefab.GetComponent<Building>() != null;
        }

        protected override Building ExtractComponent(GameObject obj)
        {
            return obj?.GetComponent<Building>();
        }

        public Building GetBuilding(GameObject prefab)
        {
            return GetFromPool(prefab);
        }

        public Building GetBuilding(GameObject prefab, Vector3 position, Quaternion rotation)
        {
            return GetFromPool(prefab, position, rotation);
        }

        public Building GetBuilding(int prefabIndex)
        {
            return GetFromPool(prefabIndex);
        }

        public Building GetBuilding(int prefabIndex, Vector3 position, Quaternion rotation)
        {
            return GetFromPool(prefabIndex, position, rotation);
        }

        public Building GetBuildingByType(BuildingTypeData type)
        {
            foreach (var prefab in buildingPrefabs)
            {
                if (prefab == null) continue;

                Building building = prefab.GetComponent<Building>();
                if (building != null && building.StatusData.BuildingType == type)
                {
                    return GetBuilding(prefab);
                }
            }

            Debug.LogWarning($"[BuildingCatalog] No building found for type: {type}");
            return null;
        }

        public Building GetBuildingByType(BuildingTypeData type, Vector3 position, Quaternion rotation)
        {
            foreach (var prefab in buildingPrefabs)
            {
                if (prefab == null) continue;

                Building building = prefab.GetComponent<Building>();
                if (building != null &&  building.StatusData != null && building.StatusData.BuildingType == type)
                {
                    return GetBuilding(prefab, position, rotation);
                }
            }

            Debug.LogWarning($"[BuildingCatalog] No building found for type: {type}");
            return null;
        }

        public void ReturnBuilding(Building building)
        {
            if (building == null) return;
            ReturnToPool(building.gameObject);
        }
    }
}
