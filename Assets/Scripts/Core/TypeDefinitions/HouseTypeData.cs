using UnityEngine;

namespace HanokBuildingSystem
{
    /// <summary>
    /// ScriptableObject-based house type definition
    /// 사용자가 커스텀 주택 타입을 추가할 수 있도록 확장 가능한 구조
    /// </summary>
    [CreateAssetMenu(fileName = "New House Type", menuName = "HanokBuildingSystem/TypeDefinitions/HouseType", order = 3)]
    public class HouseTypeData : ScriptableObject
    {
        [Header("House Information")]
        [SerializeField] private string houseID; // 고유 식별자
        [SerializeField] private string displayName; // 표시 이름 (예: "Housing", "Resource Production")
        [SerializeField] private Sprite icon; // 주택 타입 아이콘

        [Header("Description")]
        [TextArea(3, 5)]
        [SerializeField] private string description; // 주택 타입 설명

        [Header("Visual")]
        [SerializeField] private Color houseColor = Color.white; // 주택 타입 대표 색상

        public string HouseID => houseID;
        public string DisplayName => displayName;
        public Sprite Icon => icon;
        public string Description => description;
        public Color HouseColor => houseColor;

        private void OnValidate()
        {
            // houseID가 비어있으면 파일 이름을 사용
            if (string.IsNullOrEmpty(houseID))
            {
                houseID = name;
            }
        }

        public override string ToString()
        {
            return displayName;
        }
    }
}
