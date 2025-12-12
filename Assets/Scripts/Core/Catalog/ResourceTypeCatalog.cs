using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace HanokBuildingSystem
{
    /// <summary>
    /// ResourceTypeData를 중앙에서 관리하는 카탈로그
    /// 자원 검색, 대체 가능한 자원 찾기 등의 기능 제공
    /// </summary>
    public class ResourceTypeCatalog : MonoBehaviour
    {
        [Header("Resource Types")]
        [SerializeField] private List<ResourceTypeData> resourceTypes = new List<ResourceTypeData>();

        private Dictionary<string, ResourceTypeData> resourceCache;

        public List<ResourceTypeData> ResourceTypes => resourceTypes;

        private void Awake()
        {
            BuildCache();
        }

        /// <summary>
        /// 자원 타입 캐시 구축
        /// </summary>
        private void BuildCache()
        {
            resourceCache = new Dictionary<string, ResourceTypeData>();

            foreach (var resource in resourceTypes)
            {
                if (resource != null && !string.IsNullOrEmpty(resource.ResourceTypeID))
                {
                    if (resourceCache.ContainsKey(resource.ResourceTypeID))
                    {
                        Debug.LogWarning($"[ResourceTypeCatalog] Duplicate resource ID: {resource.ResourceTypeID}");
                    }
                    else
                    {
                        resourceCache[resource.ResourceTypeID] = resource;
                    }
                }
            }
        }

        /// <summary>
        /// ID로 자원 타입 찾기
        /// </summary>
        public ResourceTypeData GetResourceByID(string resourceID)
        {
            if (resourceCache == null)
            {
                BuildCache();
            }

            if (resourceCache.TryGetValue(resourceID, out ResourceTypeData resource))
            {
                return resource;
            }

            Debug.LogWarning($"[ResourceTypeCatalog] Resource not found: {resourceID}");
            return null;
        }

        /// <summary>
        /// 요구되는 자원 타입을 만족하는 모든 자원 타입 반환
        /// 예: Wood 요구 시 → [Wood, SoftWood, HardWood] 반환
        /// </summary>
        public List<ResourceTypeData> GetCompatibleResources(ResourceTypeData requiredType)
        {
            if (requiredType == null) return new List<ResourceTypeData>();

            List<ResourceTypeData> compatible = new List<ResourceTypeData>();

            foreach (var resource in resourceTypes)
            {
                if (resource != null && resource.CanSatisfy(requiredType))
                {
                    compatible.Add(resource);
                }
            }

            return compatible;
        }

        /// <summary>
        /// 두 자원이 호환되는지 확인
        /// availableResource가 requiredType 요구사항을 만족하는지 검사
        /// </summary>
        public bool AreCompatible(ResourceTypeData availableResource, ResourceTypeData requiredType)
        {
            if (availableResource == null || requiredType == null)
            {
                return false;
            }

            return availableResource.CanSatisfy(requiredType);
        }

        /// <summary>
        /// 특정 카테고리의 하위 자원들을 모두 반환
        /// 예: Wood 카테고리 → [SoftWood, HardWood] 반환
        /// </summary>
        public List<ResourceTypeData> GetSubResources(ResourceTypeData parentCategory)
        {
            if (parentCategory == null) return new List<ResourceTypeData>();

            List<ResourceTypeData> subResources = new List<ResourceTypeData>();

            foreach (var resource in resourceTypes)
            {
                if (resource != null && resource.ParentCategory == parentCategory)
                {
                    subResources.Add(resource);
                }
            }

            return subResources;
        }

        /// <summary>
        /// 특정 카테고리의 모든 하위 자원들을 재귀적으로 반환
        /// 예: Material → [Wood, SoftWood, HardWood, Stone, Granite, Marble]
        /// </summary>
        public List<ResourceTypeData> GetAllSubResourcesRecursive(ResourceTypeData parentCategory)
        {
            if (parentCategory == null) return new List<ResourceTypeData>();

            List<ResourceTypeData> allSubResources = new List<ResourceTypeData>();
            GetSubResourcesRecursive(parentCategory, allSubResources);
            return allSubResources;
        }

        private void GetSubResourcesRecursive(ResourceTypeData parent, List<ResourceTypeData> result)
        {
            foreach (var resource in resourceTypes)
            {
                if (resource != null && resource.ParentCategory == parent)
                {
                    result.Add(resource);
                    // 재귀적으로 하위 자원 탐색
                    GetSubResourcesRecursive(resource, result);
                }
            }
        }

        /// <summary>
        /// 최상위 카테고리 자원들만 반환
        /// </summary>
        public List<ResourceTypeData> GetRootCategories()
        {
            return resourceTypes.Where(r => r != null && r.IsRootCategory()).ToList();
        }

        /// <summary>
        /// 자원 타입을 등록 (런타임에 추가하는 경우)
        /// </summary>
        public void RegisterResourceType(ResourceTypeData resourceType)
        {
            if (resourceType == null)
            {
                Debug.LogWarning("[ResourceTypeCatalog] Cannot register null resource type");
                return;
            }

            if (!resourceTypes.Contains(resourceType))
            {
                resourceTypes.Add(resourceType);

                if (resourceCache != null && !string.IsNullOrEmpty(resourceType.ResourceTypeID))
                {
                    resourceCache[resourceType.ResourceTypeID] = resourceType;
                }
            }
        }

        /// <summary>
        /// 자원 타입 등록 해제
        /// </summary>
        public void UnregisterResourceType(ResourceTypeData resourceType)
        {
            if (resourceType == null) return;

            resourceTypes.Remove(resourceType);

            if (resourceCache != null && !string.IsNullOrEmpty(resourceType.ResourceTypeID))
            {
                resourceCache.Remove(resourceType.ResourceTypeID);
            }
        }

#if UNITY_EDITOR
        [Header("Debug")]
        [SerializeField] private bool showDebugInfo = false;

        private void OnValidate()
        {
            if (showDebugInfo)
            {
                Debug.Log($"[ResourceTypeCatalog] Total resources: {resourceTypes.Count}");
            }
        }
#endif
    }
}
