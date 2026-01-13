using System.Collections.Generic;
using UnityEngine;

namespace HanokBuildingSystem
{
    /// <summary>
    /// 단일 GameObject 풀링을 담당하는 기본 컴포넌트
    /// </summary>
    public class PoolingComponent : MonoBehaviour
    {
        [Header("Pooling Settings")]
        [SerializeField] protected GameObject prefab;
        [SerializeField] protected int initialPoolSize = 10;
        [SerializeField] protected bool autoExpand = true;

        protected Queue<GameObject> pool = new Queue<GameObject>();
        protected HashSet<GameObject> activeObjects = new HashSet<GameObject>();

        public GameObject Prefab => prefab;
        public int PoolSize => pool.Count;
        public int ActiveCount => activeObjects.Count;

        protected virtual void Awake()
        {
            if (prefab != null) Initialize();
        }

        protected virtual void Initialize()
        {
            for (int i = 0; i < initialPoolSize; i++)
            {
                CreateObject();
            }
        }

        public virtual void Initialize(GameObject prefab)
        {
            this.prefab = prefab;
            Initialize();
        }

        protected virtual GameObject CreateObject()
        {
            GameObject obj = Instantiate(prefab, transform);
            obj.SetActive(false);
            pool.Enqueue(obj);
            return obj;
        }

        public virtual GameObject Get()
        {
            GameObject obj = null;

            if (pool.Count > 0)
            {
                obj = pool.Dequeue();
            }
            else if (autoExpand)
            {
                obj = CreateObject();
                if (obj != null)
                {
                    pool.Dequeue();
                }
            }

            if (obj != null)
            {
                obj.SetActive(true);
                obj.transform.SetParent(null);
                activeObjects.Add(obj);
                OnGet(obj);
            }

            return obj;
        }

        public virtual GameObject Get(Vector3 position, Quaternion rotation)
        {
            GameObject obj = Get();
            if (obj != null)
            {
                obj.transform.position = position;
                obj.transform.rotation = rotation;
            }
            return obj;
        }

        public virtual void Return(GameObject obj)
        {
            if (obj == null || !activeObjects.Contains(obj))
                return;

            OnReturn(obj);

            obj.transform.SetParent(transform);
            obj.SetActive(false);
            activeObjects.Remove(obj);
            pool.Enqueue(obj);
        }

        protected virtual void OnGet(GameObject obj) { }
        protected virtual void OnReturn(GameObject obj) { }

        public void Clear()
        {
            foreach (var obj in activeObjects)
            {
                if (obj != null)
                {
                    Destroy(obj);
                }
            }
            activeObjects.Clear();

            while (pool.Count > 0)
            {
                GameObject obj = pool.Dequeue();
                if (obj != null)
                {
                    Destroy(obj);
                }
            }
        }
    }
}
