using System.Collections.Generic;
using UnityEngine;

namespace HanokBuildingSystem
{
    /// <summary>
    /// BuildingMember 프리팹들의 인스턴스 풀링을 담당하는 카탈로그
    /// BuildingMember는 특별한 컴포넌트가 없는 일반 GameObject입니다.
    /// </summary>
    public class BuildingMemberCatalog : CatalogBase<GameObject>
    {
        [Header("Building Member Prefabs")]
        [SerializeField] private List<GameObject> memberPrefabs = new List<GameObject>();

        [Header("Filter Settings")]
        [SerializeField] private bool filterOutBuildingAndHouse = true;

        public List<GameObject> MemberPrefabs => memberPrefabs;

        public override List<GameObject> GetPrefabList() => memberPrefabs;

        protected override bool ValidatePrefab(GameObject prefab)
        {
            if (!filterOutBuildingAndHouse)
            {
                return true;
            }

            // BuildingMember는 Building이나 House 컴포넌트를 가지지 않아야 함
            return prefab.GetComponent<Building>() == null && prefab.GetComponent<House>() == null;
        }

        protected override GameObject ExtractComponent(GameObject obj)
        {
            return obj;
        }

        public GameObject GetMember(GameObject prefab)
        {
            return GetFromPool(prefab);
        }

        public GameObject GetMember(GameObject prefab, Vector3 position, Quaternion rotation)
        {
            return GetFromPool(prefab, position, rotation);
        }

        public GameObject GetMember(int prefabIndex)
        {
            return GetFromPool(prefabIndex);
        }

        public GameObject GetMember(int prefabIndex, Vector3 position, Quaternion rotation)
        {
            return GetFromPool(prefabIndex, position, rotation);
        }

        public GameObject GetMemberByName(string prefabName)
        {
            foreach (var prefab in memberPrefabs)
            {
                if (prefab != null && prefab.name == prefabName)
                {
                    return GetMember(prefab);
                }
            }

            Debug.LogWarning($"[BuildingMemberCatalog] No member found with name: {prefabName}");
            return null;
        }

        public GameObject GetMemberByName(string prefabName, Vector3 position, Quaternion rotation)
        {
            foreach (var prefab in memberPrefabs)
            {
                if (prefab != null && prefab.name == prefabName)
                {
                    return GetMember(prefab, position, rotation);
                }
            }

            Debug.LogWarning($"[BuildingMemberCatalog] No member found with name: {prefabName}");
            return null;
        }

        public void ReturnMember(GameObject member)
        {
            if (member == null) return;
            ReturnToPool(member);
        }
    }
}
