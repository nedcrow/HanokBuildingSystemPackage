using System.Collections.Generic;
using UnityEngine;

namespace HanokBuildingSystem
{
    /// <summary>
    /// 프리팹 풀링을 담당하는 카탈로그의 베이스 클래스
    /// </summary>
    /// <typeparam name="T">카탈로그가 관리하는 컴포넌트 타입 (Building, House 등). GameObject를 직접 관리할 경우 Transform 사용</typeparam>
    public abstract class CatalogBase<T> : MonoBehaviour where T : class
    {
        [Header("Pool Settings")]
        [SerializeField] protected int initialPoolSizePerPrefab = 5;
        [SerializeField] protected bool autoExpand = true;

        protected Dictionary<GameObject, PoolingComponent> prefabPools = new Dictionary<GameObject, PoolingComponent>();
        protected Transform activeContainer;        
        public abstract List<GameObject> GetPrefabList();

        protected virtual void Awake()
        {
            Initialize();
        }

        protected virtual void Initialize()
        {
            // Active 컨테이너 생성
            GameObject activeObj = new GameObject("Active");
            activeObj.transform.SetParent(transform);
            activeContainer = activeObj.transform;

            var prefabs = GetPrefabList();
            foreach (var prefab in prefabs)
            {
                if (prefab == null) continue;

                if (!ValidatePrefab(prefab))
                {
                    Debug.LogWarning($"[{GetType().Name}] Prefab '{prefab.name}' failed validation!");
                    continue;
                }

                CreatePoolForPrefab(prefab);
            }
        }

        /// <summary>
        /// 프리팹이 이 카탈로그에서 관리 가능한지 검증합니다.
        /// </summary>
        protected abstract bool ValidatePrefab(GameObject prefab);

        protected virtual void CreatePoolForPrefab(GameObject prefab)
        {
            GameObject poolObj = new GameObject($"Pool_{prefab.name}");
            poolObj.transform.SetParent(transform);

            PoolingComponent pool = poolObj.AddComponent<PoolingComponent>();
            pool.GetType().GetField("initialPoolSize", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(pool, initialPoolSizePerPrefab);
            pool.GetType().GetField("autoExpand", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(pool, autoExpand);
            pool.Initialize(prefab);

            prefabPools[prefab] = pool;
        }

        /// <summary>
        /// GameObject에서 관리 대상 컴포넌트를 추출합니다.
        /// </summary>
        protected abstract T ExtractComponent(GameObject obj);

        protected virtual T GetFromPool(GameObject prefab)
        {
            if (prefab == null || !prefabPools.ContainsKey(prefab))
            {
                Debug.LogWarning($"[{GetType().Name}] Prefab not found in catalog!");
                return null;
            }

            GameObject obj = prefabPools[prefab].Get();
            if (obj != null && activeContainer != null)
            {
                obj.transform.SetParent(activeContainer);
            }
            return ExtractComponent(obj);
        }

        protected virtual T GetFromPool(GameObject prefab, Vector3 position, Quaternion rotation)
        {
            if (prefab == null || !prefabPools.ContainsKey(prefab))
            {
                Debug.LogWarning($"[{GetType().Name}] Prefab not found in catalog!");
                return null;
            }

            GameObject obj = prefabPools[prefab].Get(position, rotation);
            if (obj != null && activeContainer != null)
            {
                obj.transform.SetParent(activeContainer);
            }
            return ExtractComponent(obj);
        }

        protected virtual T GetFromPool(int prefabIndex)
        {
            var prefabs = GetPrefabList();
            if (prefabIndex < 0 || prefabIndex >= prefabs.Count)
            {
                Debug.LogWarning($"[{GetType().Name}] Invalid prefab index: {prefabIndex}");
                return null;
            }

            return GetFromPool(prefabs[prefabIndex]);
        }

        protected virtual T GetFromPool(int prefabIndex, Vector3 position, Quaternion rotation)
        {
            var prefabs = GetPrefabList();
            if (prefabIndex < 0 || prefabIndex >= prefabs.Count)
            {
                Debug.LogWarning($"[{GetType().Name}] Invalid prefab index: {prefabIndex}");
                return null;
            }

            return GetFromPool(prefabs[prefabIndex], position, rotation);
        }

        /// <summary>
        /// GameObject를 풀로 반환합니다.
        /// </summary>
        public virtual void ReturnToPool(GameObject obj)
        {
            if (obj == null) return;

            foreach (var kvp in prefabPools)
            {
                if (obj.name.StartsWith(kvp.Key.name))
                {
                    kvp.Value.Return(obj);
                    return;
                }
            }

            Debug.LogWarning($"[{GetType().Name}] Could not find pool for object: {obj.name}");
        }

        public virtual void ReturnAll()
        {
            foreach (var pool in prefabPools.Values)
            {
                if (pool != null)
                {
                    foreach (var activeObj in new List<GameObject>(pool.GetType()
                        .GetField("activeObjects", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                        ?.GetValue(pool) as HashSet<GameObject> ?? new HashSet<GameObject>()))
                    {
                        pool.Return(activeObj);
                    }
                }
            }
        }

        public virtual void AddPrefab(GameObject prefab)
        {
            if (prefab == null) return;

            if (!ValidatePrefab(prefab))
            {
                Debug.LogWarning($"[{GetType().Name}] Prefab '{prefab.name}' failed validation!");
                return;
            }

            var prefabs = GetPrefabList();
            if (prefabs.Contains(prefab))
            {
                Debug.LogWarning($"[{GetType().Name}] Prefab '{prefab.name}' is already in the catalog!");
                return;
            }

            prefabs.Add(prefab);
            CreatePoolForPrefab(prefab);
        }

        public virtual void RemovePrefab(GameObject prefab)
        {
            if (prefab == null) return;

            if (prefabPools.ContainsKey(prefab))
            {
                prefabPools[prefab].Clear();
                Destroy(prefabPools[prefab].gameObject);
                prefabPools.Remove(prefab);
            }

            GetPrefabList().Remove(prefab);
        }

        protected virtual void OnDestroy()
        {
            foreach (var pool in prefabPools.Values)
            {
                if (pool != null)
                {
                    pool.Clear();
                }
            }
        }

#if UNITY_EDITOR
        protected virtual void OnValidate()
        {
            var prefabs = GetPrefabList();
            for (int i = prefabs.Count - 1; i >= 0; i--)
            {
                if (prefabs[i] != null && !ValidatePrefab(prefabs[i]))
                {
                    Debug.LogWarning($"[{GetType().Name}] Removing invalid prefab: {prefabs[i].name}");
                    prefabs.RemoveAt(i);
                }
            }
        }
#endif
    }
}
