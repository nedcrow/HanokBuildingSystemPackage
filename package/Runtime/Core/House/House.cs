using System;
using System.Collections.Generic;
using UnityEngine;

namespace HanokBuildingSystem
{
    public enum HouseConditionState
    {
        Normal,
        Fire,
        Flooded,
        SnowLoad,
        Derelict
    }

    public enum HouseOccupancyState
    {
        UnderConstruction,
        Vacant,
        Occupied
    }

    [Serializable]
    public class SatisfactionCondition
    {
        public string conditionName;
        public bool isSatisfied;
    }

    /// <summary>
    /// House 이벤트 시스템
    /// </summary>
    public class HouseEvents
    {
        public event Action<House, Plot> OnConstructionStarted;
        public event Action<House, Plot> OnShowModelHouse;

        public void RaiseConstructionStarted(House house, Plot plot)
        {
            OnConstructionStarted?.Invoke(house, plot);
        }

        public void RaiseShowModelHouse(House house, Plot plot)
        {
            OnShowModelHouse?.Invoke(house, plot);
        }
    }

    public class House : MonoBehaviour
    {
    [Header("Sample Size")]
    [Tooltip("Sample boundary size for editor visualization")]
    [SerializeField] private Vector2 sampleSize = new Vector2(10f, 10f);

    [Header("House Configuration")]
    [SerializeField] private List<BuildingTypeData> requiredBuildingTypes = new List<BuildingTypeData>();

    [SerializeField] private HouseTypeData houseType;

    [Header("Area Information")]
    [Tooltip("Plot defining the house boundary area")]
    [SerializeField] private Plot boundaryPlot;

    [Header("Current Buildings")]
    [SerializeField] private List<Building> buildings = new List<Building>();

    [Header("Ownership & Population")]
    [SerializeField] private GameObject owner;
    [SerializeField] private int currentResidents = 0;
    [SerializeField] private int maxResidents = 4;

    [Header("Storage")]
    [SerializeField] private int storageCapacity = 100;
    [SerializeField] private int currentStorageUsed = 0;

    [Header("House Status")]
    [SerializeField] private float totalArea = 0f;
    [SerializeField] private List<SatisfactionCondition> satisfactionConditions = new List<SatisfactionCondition>();
    [SerializeField] private float satisfactionLevel = 0f;
    [SerializeField] private float durability = 100f;
    [SerializeField] private HouseConditionState state = HouseConditionState.Normal;
    [SerializeField] private HouseOccupancyState usageState = HouseOccupancyState.UnderConstruction;

    private HouseEvents events = new HouseEvents();
    public HouseEvents Events => events;

    public Vector2 SampleSize => sampleSize;
    public HouseTypeData HouseType => houseType;
    public List<BuildingTypeData> RequiredBuildingTypes => requiredBuildingTypes;
    public Plot BoundaryPlot => boundaryPlot;
    public List<Building> Buildings => buildings;
    public GameObject Owner => owner;
    public int CurrentResidents => currentResidents;
    public int MaxResidents => maxResidents;
    public int StorageCapacity => storageCapacity;
    public int CurrentStorageUsed => currentStorageUsed;
    public float TotalArea => totalArea;
    public float SatisfactionLevel => satisfactionLevel;
    public float Durability => durability;
    public HouseConditionState State => state;
    public HouseOccupancyState UsageState => usageState;

    private void Awake()
    {
        InitializeHouse();
    }

    private void InitializeHouse()
    {
        if (buildings == null)
        {
            buildings = new List<Building>();
        }

        if (requiredBuildingTypes == null)
        {
            requiredBuildingTypes = new List<BuildingTypeData>();
        }
    }

    public void AddBuilding(Building building)
    {
        if (building == null)
        {
            Debug.LogWarning("Cannot add null building to house");
            return;
        }

        if (!buildings.Contains(building))
        {
            buildings.Add(building);
            building.transform.SetParent(transform);
        }
    }

    public void RemoveBuilding(Building building)
    {
        if (building != null && buildings.Contains(building))
        {
            buildings.Remove(building);
        }
    }

    public bool HasRequiredBuildings()
    {
        foreach (BuildingTypeData requiredType in requiredBuildingTypes)
        {
            bool hasType = false;
            foreach (Building building in buildings)
            {
                if (building.StatusData.BuildingType == requiredType)
                {
                    hasType = true;
                    break;
                }
            }

            if (!hasType)
            {
                return false;
            }
        }

        return true;
    }

    public void SetBoundaryPlot(Plot plot)
    {
        boundaryPlot = plot;
    }

    public void SetOwner(GameObject newOwner)
    {
        owner = newOwner;
    }

    public void SetResidents(int count)
    {
        currentResidents = Mathf.Clamp(count, 0, maxResidents);
    }

    public void SetMaxResidents(int max)
    {
        maxResidents = Mathf.Max(0, max);
        currentResidents = Mathf.Min(currentResidents, maxResidents);
    }

    public void SetStorageUsed(int used)
    {
        currentStorageUsed = Mathf.Clamp(used, 0, storageCapacity);
    }

    public void SetDurability(float value)
    {
        durability = Mathf.Clamp(value, 0f, 100f);
    }

    public void SetState(HouseConditionState newState)
    {
        state = newState;
    }

    public void SetUsageState(HouseOccupancyState newUsageState)
    {
        usageState = newUsageState;
    }

    public void SetSatisfactionLevel(float level)
    {
        satisfactionLevel = Mathf.Clamp01(level);
    }

    public void UpdateSatisfactionConditions()
    {
        int satisfiedCount = 0;
        foreach (SatisfactionCondition condition in satisfactionConditions)
        {
            if (condition.isSatisfied)
            {
                satisfiedCount++;
            }
        }

        if (satisfactionConditions.Count > 0)
        {
            satisfactionLevel = (float)satisfiedCount / satisfactionConditions.Count;
        }
    }

    /// <summary>
    /// 하우스의 모든 빌딩을 건설 시작 단계(Stage 0)로 초기화
    /// </summary>
    public void StartConstruction(Plot plot)
    {
        if (plot == null)
        {
            Debug.LogWarning("[House] Cannot start construction: plot is null");
            return;
        }

        // Plot 저장
        boundaryPlot = plot;

        Vector3 plotCenter = plot.GetCenter();
        transform.position = plotCenter;

        // 이벤트 발행 - 각 빌딩이 자율적으로 반응
        events.RaiseConstructionStarted(this, plot);

        Debug.Log($"[House] {name}: Construction started with {buildings.Count} buildings");
    }

    public void ShowModelHouse(Plot plot, bool useTerrainHeight = false, LayerMask terrainLayer = default)
    {
        if (plot == null)
        {
            Debug.LogWarning("[House] Cannot show model house: plot is null");
            return;
        }

        // Plot 저장
        boundaryPlot = plot;

        Vector3 plotCenter = plot.GetCenter();
        transform.position = plotCenter;

        Transform markersParent = transform.Find("Markers");
        if (markersParent == null)
        {
            Debug.LogWarning("[House] Cannot show model house: Markers not found");
            return;
        }

        BuildingCatalog buildingCatalog = HanokBuildingSystem.Instance?.BuildingCatalog;
        if (buildingCatalog == null)
        {
            Debug.LogWarning("[House] Cannot show model house: BuildingCatalog not found");
            return;
        }

        // 빌딩 인스턴스 생성
        foreach (Transform child in markersParent)
        {
            MarkerComponent marker = child.GetComponent<MarkerComponent>();
            if (marker == null || marker.IsAreaMarker || marker.BuildingType == null)
                continue;

            // 이미 빌딩이 있으면 스킵
            if (marker.CurrentBuilding != null)
                continue;

            // 마커 위치에서 지형 높이 계산
            Vector3 buildingPosition = child.position;
            if (useTerrainHeight && terrainLayer != 0)
            {
                buildingPosition = GetTerrainPosition(child.position, terrainLayer);
            }

            Building building = buildingCatalog.GetBuildingByType(
                marker.BuildingType,
                buildingPosition,
                child.rotation
            );

            if (building != null)
            {
                marker.SetCurrentBuilding(building);
                AddBuilding(building);
            }
            else
            {
                Debug.LogWarning($"[House] Failed to get building of type {marker.BuildingType}");
            }
        }

        // 이벤트 발행 - 각 빌딩이 자율적으로 모델 비주얼 표시
        events.RaiseShowModelHouse(this, plot);

        SetUsageState(HouseOccupancyState.UnderConstruction);
    }

    /// <summary>
    /// XZ 위치에서 지형 높이를 Raycast로 구하여 반환
    /// </summary>
    private Vector3 GetTerrainPosition(Vector3 markerPosition, LayerMask terrainLayer)
    {
        // 위에서 아래로 Raycast
        Vector3 rayOrigin = new Vector3(markerPosition.x, 1000f, markerPosition.z);
        Ray ray = new Ray(rayOrigin, Vector3.down);

        if (Physics.Raycast(ray, out RaycastHit hit, 2000f, terrainLayer))
        {
            return hit.point;
        }

        // Raycast 실패 시 원래 위치 반환
        return markerPosition;
    }

#if UNITY_EDITOR
    private Vector2 previousSampleSize;

    private void OnValidate()
    {
        if (!Application.isPlaying)
        {
            if (previousSampleSize != sampleSize)
            {
                previousSampleSize = sampleSize;
                UnityEditor.SceneView.RepaintAll();
            }

            UpdateBuildingMarkers();
        }
    }

    private void UpdateBuildingMarkers()
    {
        if (requiredBuildingTypes == null) return;

        Transform markersParent = transform.Find("Markers");
        if (markersParent == null)
        {
            GameObject markersObj = new GameObject("Markers");
            markersObj.transform.SetParent(transform);
            markersObj.transform.localPosition = Vector3.zero;
            markersParent = markersObj.transform;
        }

        UpdateAreaMarker(markersParent);
        UpdateSubMarkers(markersParent);
    }

    private void UpdateAreaMarker(Transform markersParent)
    {
        Transform areaMarkerTransform = markersParent.Find("AreaMarker");

        if (areaMarkerTransform == null)
        {
            GameObject areaMarkerObj = new GameObject("AreaMarker");
            areaMarkerObj.transform.SetParent(markersParent);
            areaMarkerObj.transform.localPosition = Vector3.zero;
            areaMarkerObj.transform.localRotation = Quaternion.identity;

            MarkerComponent areaMarker = areaMarkerObj.AddComponent<MarkerComponent>();
            areaMarker.SetAsAreaMarker(true);
            areaMarker.SetMarkerSize(sampleSize);
        }
        else
        {
            MarkerComponent areaMarker = areaMarkerTransform.GetComponent<MarkerComponent>();
            if (areaMarker != null)
            {
                areaMarker.SetMarkerSize(sampleSize);
            }
        }
    }

    private void UpdateSubMarkers(Transform markersParent)
    {
        List<MarkerComponent> existingMarkers = new List<MarkerComponent>();
        foreach (Transform child in markersParent)
        {
            if (child.name == "AreaMarker") continue;

            MarkerComponent marker = child.GetComponent<MarkerComponent>();
            if (marker != null)
            {
                existingMarkers.Add(marker);
            }
        }

        foreach (BuildingTypeData requiredType in requiredBuildingTypes)
        {
            if (requiredType == null) continue;

            MarkerComponent existingMarker = existingMarkers.Find(m => m.BuildingType == requiredType);

            if (existingMarker == null)
            {
                GameObject markerObj = new GameObject($"Marker_{requiredType}");
                markerObj.transform.SetParent(markersParent);
                markerObj.transform.localPosition = Vector3.right * (markersParent.childCount - 1) * 5f;
                markerObj.transform.localRotation = Quaternion.identity;

                MarkerComponent marker = markerObj.AddComponent<MarkerComponent>();
                marker.SetBuildingType(requiredType);
                marker.SetMarkerSize(new Vector2(5f, 5f));
            }
            else
            {
                existingMarkers.Remove(existingMarker);
            }
        }

        foreach (MarkerComponent marker in existingMarkers)
        {
            if (Application.isEditor && !Application.isPlaying)
            {
                UnityEditor.EditorApplication.delayCall += () =>
                {
                    if (marker != null)
                    {
                        DestroyImmediate(marker.gameObject);
                    }
                };
            }
        }
    }    
#endif
    }
}
