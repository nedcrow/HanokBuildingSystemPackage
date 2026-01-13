using UnityEngine;

namespace HanokBuildingSystem
{
    /// <summary>
    /// ScriptableObject-based building type definition
    /// 사용자가 커스텀 건물 타입을 추가할 수 있도록 확장 가능한 구조
    /// </summary>
    [CreateAssetMenu(fileName = "New Building Type", menuName = "HanokBuildingSystem/TypeDefinitions/BuildingType", order = 2)]
    public class BuildingTypeData : ScriptableObject
    {
        [Header("Building Information")]
        [SerializeField] private string buildingTypeID; // 고유 식별자
        [SerializeField] private string displayTypeName; // 표시 이름 (예: "안채", "사랑채")
        [SerializeField] private Sprite icon; // 건물 아이콘

        [Header("Description")]
        [TextArea(3, 5)]
        [SerializeField] private string description; // 건물 설명

        [Header("Visual")]
        [SerializeField] private Color buildingColor = Color.white; // 건물 대표 색상

        public string BuildingTypeID => buildingTypeID;
        public string DisplayTypeName => displayTypeName;
        public Sprite Icon => icon;
        public string Description => description;
        public Color BuildingColor => buildingColor;

        private void OnValidate()
        {
            // buildingID가 비어있으면 파일 이름을 사용
            if (string.IsNullOrEmpty(buildingTypeID))
            {
                buildingTypeID = name;
            }
        }

        public override string ToString()
        {
            return displayTypeName;
        }
    }
}
