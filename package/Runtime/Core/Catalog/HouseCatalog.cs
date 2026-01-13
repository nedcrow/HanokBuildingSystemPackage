using System.Collections.Generic;
using UnityEngine;

namespace HanokBuildingSystem
{
    /// <summary>
    /// House 프리팹들의 인스턴스 풀링을 담당하는 카탈로그
    /// </summary>
    public class HouseCatalog : CatalogBase<House>
    {
        [Header("House Prefabs")]
        [SerializeField] private List<GameObject> housePrefabs = new List<GameObject>();

        public List<GameObject> HousePrefabs => housePrefabs;

        public override List<GameObject> GetPrefabList() => housePrefabs;

        protected override bool ValidatePrefab(GameObject prefab)
        {
            return prefab.GetComponent<House>() != null;
        }

        protected override House ExtractComponent(GameObject obj)
        {
            return obj?.GetComponent<House>();
        }

        public House GetHouse()
        {
            if (housePrefabs.Count == 0)
            {
                Debug.LogWarning($"[HouseCatalog] No house prefabs available!");
                return null;
            }

            GameObject firstPrefab = housePrefabs[0];
            firstPrefab.SetActive(true);
            return GetHouse(firstPrefab);
        }

        public House GetHouse(GameObject prefab)
        {
            return GetFromPool(prefab);
        }

        public House GetHouse(GameObject prefab, Vector3 position, Quaternion rotation)
        {
            return GetFromPool(prefab, position, rotation);
        }

        public House GetHouse(int prefabIndex)
        {
            return GetFromPool(prefabIndex);
        }

        public House GetHouse(int prefabIndex, Vector3 position, Quaternion rotation)
        {
            return GetFromPool(prefabIndex, position, rotation);
        }

        public void ReturnHouse(House house)
        {
            if (house == null) return;
            ReturnToPool(house.gameObject);
        }
    }
}
