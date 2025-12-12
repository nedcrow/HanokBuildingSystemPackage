using UnityEngine;

namespace HanokBuildingSystem
{
    public class MarkerComponent : MonoBehaviour
    {
    [Header("Marker Configuration")]
    [SerializeField] private BuildingTypeData buildingType;
    [SerializeField] private Vector2 markerSize = new Vector2(5f, 5f);
    [SerializeField] private bool isMainMarker = false;

    [Header("Runtime")]
    [SerializeField] private Building currentBuilding;

    public BuildingTypeData BuildingType => buildingType;
    public Vector2 MarkerSize => markerSize;
    public bool IsMainMarker => isMainMarker;
    public Building CurrentBuilding => currentBuilding;

    public void SetBuildingType(BuildingTypeData type)
    {
        buildingType = type;
        gameObject.name = isMainMarker ? "MainMarker" : $"Marker_{type}";
    }

    public void SetMarkerSize(Vector2 size)
    {
        markerSize = size;
    }

    public void SetAsMainMarker(bool isMain)
    {
        isMainMarker = isMain;
        gameObject.name = isMain ? "MainMarker" : $"Marker_{buildingType}";
    }

    public void SetCurrentBuilding(Building building)
    {
        currentBuilding = building;
    }

    public void ClearCurrentBuilding()
    {
        currentBuilding = null;
    }

#if UNITY_EDITOR
    private Vector2 previousSize;

    private void OnValidate()
    {
        if (!Application.isPlaying && previousSize != markerSize)
        {
            previousSize = markerSize;
            UnityEditor.SceneView.RepaintAll();
        }
    }

    private void OnDrawGizmos()
    {
        if (markerSize.x <= 0 || markerSize.y <= 0) return;

        Vector3 center = transform.position;
        Vector3 halfExtents = new Vector3(markerSize.x * 0.5f, 0f, markerSize.y * 0.5f);

        Vector3 corner1 = center + new Vector3(-halfExtents.x, 0, -halfExtents.z);
        Vector3 corner2 = center + new Vector3(halfExtents.x, 0, -halfExtents.z);
        Vector3 corner3 = center + new Vector3(halfExtents.x, 0, halfExtents.z);
        Vector3 corner4 = center + new Vector3(-halfExtents.x, 0, halfExtents.z);

        Color borderColor = isMainMarker ? Color.yellow : new Color(1f, 0.5f, 0f);
        Gizmos.color = borderColor;
        Gizmos.DrawLine(corner1, corner2);
        Gizmos.DrawLine(corner2, corner3);
        Gizmos.DrawLine(corner3, corner4);
        Gizmos.DrawLine(corner4, corner1);

        UnityEditor.Handles.color = new Color(borderColor.r, borderColor.g, borderColor.b, 0.1f);
        UnityEditor.Handles.DrawSolidRectangleWithOutline(
            new Vector3[] { corner1, corner2, corner3, corner4 },
            new Color(borderColor.r, borderColor.g, borderColor.b, 0.1f),
            borderColor
        );

        if (!isMainMarker && buildingType != null)
        {
            UnityEditor.Handles.Label(center + Vector3.up * 0.5f, buildingType.ToString());
        }
    }
#endif
    }
}
