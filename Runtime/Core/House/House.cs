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

    public class House : MonoBehaviour
    {
    [Header("Sample Size")]
    [Tooltip("Sample boundary size for editor visualization")]
    [SerializeField] private Vector2 sampleSize = new Vector2(10f, 10f);

    [Header("House Configuration")]
    [SerializeField] private List<BuildingType> requiredBuildingTypes = new List<BuildingType>();

    [SerializeField] private HouseType houseType = HouseType.None;

    [Header("Area Information")]
    [Tooltip("2D outline vertices defining the house area")]
    [SerializeField] private List<List<Vector3>> outlineVertices = new List<List<Vector3>>();

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

    public Vector2 SampleSize => sampleSize;
    public HouseType HouseType => houseType;
    public List<BuildingType> RequiredBuildingTypes => requiredBuildingTypes;
    public List<List<Vector3>> OutlineVertices => outlineVertices;
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

        if (outlineVertices == null)
        {
            outlineVertices = new List<List<Vector3>>();
        }

        if (requiredBuildingTypes == null)
        {
            requiredBuildingTypes = new List<BuildingType>();
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
        foreach (BuildingType requiredType in requiredBuildingTypes)
        {
            bool hasType = false;
            foreach (Building building in buildings)
            {
                if (building.Type == requiredType)
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

    public void SetOutlineVertices(List<List<Vector3>> vertices)
    {
        outlineVertices = vertices;
    }

    public void AddOutlineLoop(List<Vector3> vertexLoop)
    {
        if (vertexLoop != null && vertexLoop.Count > 0)
        {
            outlineVertices.Add(vertexLoop);
        }
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

    public void ShowModelHouse(Plot plot)
    {
        if (plot == null)
        {
            Debug.LogWarning("[House] Cannot show model house: plot is null");
            return;
        }

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

        foreach (Transform child in markersParent)
        {
            MarkerComponent marker = child.GetComponent<MarkerComponent>();
            if (marker == null || marker.IsMainMarker || marker.BuildingType == BuildingType.None)
                continue;

            // 이미 빌딩이 있으면 스킵
            if (marker.CurrentBuilding != null)
                continue;

            Building building = buildingCatalog.GetBuildingByType(
                marker.BuildingType,
                child.position,
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

        SetUsageState(HouseOccupancyState.UnderConstruction);
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

        UpdateMainMarker(markersParent);
        UpdateSubMarkers(markersParent);
    }

    private void UpdateMainMarker(Transform markersParent)
    {
        Transform mainMarkerTransform = markersParent.Find("MainMarker");

        if (mainMarkerTransform == null)
        {
            GameObject mainMarkerObj = new GameObject("MainMarker");
            mainMarkerObj.transform.SetParent(markersParent);
            mainMarkerObj.transform.localPosition = Vector3.zero;
            mainMarkerObj.transform.localRotation = Quaternion.identity;

            MarkerComponent mainMarker = mainMarkerObj.AddComponent<MarkerComponent>();
            mainMarker.SetAsMainMarker(true);
            mainMarker.SetMarkerSize(sampleSize);
        }
        else
        {
            MarkerComponent mainMarker = mainMarkerTransform.GetComponent<MarkerComponent>();
            if (mainMarker != null)
            {
                mainMarker.SetMarkerSize(sampleSize);
            }
        }
    }

    private void UpdateSubMarkers(Transform markersParent)
    {
        List<MarkerComponent> existingMarkers = new List<MarkerComponent>();
        foreach (Transform child in markersParent)
        {
            if (child.name == "MainMarker") continue;

            MarkerComponent marker = child.GetComponent<MarkerComponent>();
            if (marker != null)
            {
                existingMarkers.Add(marker);
            }
        }

        foreach (BuildingType requiredType in requiredBuildingTypes)
        {
            if (requiredType == BuildingType.None) continue;

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

    private void OnDrawGizmosSelected()
    {
        DrawOutlineVertices();
    }

    private void DrawOutlineVertices()
    {
        if (outlineVertices != null)
        {
            Gizmos.color = Color.green;
            foreach (List<Vector3> loop in outlineVertices)
            {
                if (loop == null || loop.Count < 2) continue;

                for (int i = 0; i < loop.Count; i++)
                {
                    Vector3 current = loop[i];
                    Vector3 next = loop[(i + 1) % loop.Count];
                    Gizmos.DrawLine(current, next);
                }
            }
        }
    }
#endif
    }
}
