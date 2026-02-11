using System.Collections.Generic;
using System.Linq;
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

        private List<GameObject> FindPrefabsByType(BuildingTypeData type)
        {
            return buildingPrefabs
                .Where(p => p != null)
                .Where(p =>
                {
                    Building b = p.GetComponent<Building>();
                    return b != null && b.StatusData != null && b.StatusData.BuildingType == type;
                })
                .ToList();
        }

        public Building GetBuildingByType(BuildingTypeData type)
        {
            var matched = FindPrefabsByType(type);
            if (matched.Count == 0)
            {
                Debug.LogWarning($"[BuildingCatalog] No building found for type: {type}");
                return null;
            }

            return GetBuilding(matched[0]);
        }

        public Building GetBuildingByType(BuildingTypeData type, Vector3 position, Quaternion rotation)
        {
            var matched = FindPrefabsByType(type);
            if (matched.Count == 0)
            {
                Debug.LogWarning($"[BuildingCatalog] No building found for type: {type}");
                return null;
            }

            return GetBuilding(matched[0], position, rotation);
        }

        public Building GetRandomBuildingByType(BuildingTypeData type)
        {
            var matched = FindPrefabsByType(type);
            if (matched.Count == 0)
            {
                Debug.LogWarning($"[BuildingCatalog] No building found for type: {type}");
                return null;
            }

            return GetBuilding(matched[Random.Range(0, matched.Count)]);
        }

        public Building GetRandomBuildingByType(BuildingTypeData type, Vector3 position, Quaternion rotation)
        {
            var matched = FindPrefabsByType(type);
            if (matched.Count == 0)
            {
                Debug.LogWarning($"[BuildingCatalog] No building found for type: {type}");
                return null;
            }

            return GetBuilding(matched[Random.Range(0, matched.Count)], position, rotation);
        }

        public void ReturnBuilding(Building building)
        {
            if (building == null) return;
            ReturnToPool(building.gameObject);
        }
    }
}
