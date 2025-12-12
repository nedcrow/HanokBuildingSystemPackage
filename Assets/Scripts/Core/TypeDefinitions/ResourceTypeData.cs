using UnityEngine;

namespace HanokBuildingSystem
{
    /// <summary>
    /// ScriptableObject-based resource type definition
    /// 사용자가 커스텀 리소스 타입을 추가할 수 있도록 확장 가능한 구조
    /// </summary>
    [CreateAssetMenu(fileName = "New Resource Type", menuName = "HanokBuildingSystem/TypeDefinitions/ResourceType", order = 1)]
    public class ResourceTypeData : ScriptableObject
    {
        [Header("Resource Information")]
        [SerializeField] private string resourceTypeID; // 고유 식별자
        [SerializeField] private string displayTypeName; // 표시 이름
        [SerializeField] private Sprite icon; // 리소스 아이콘

        [Header("Description")]
        [TextArea(3, 5)]
        [SerializeField] private string description; // 리소스 설명

        [Header("Visual")]
        [SerializeField] private Color resourceColor = Color.white; // 리소스 대표 색상

        [Header("Category Hierarchy")]
        [Tooltip("상위 카테고리 자원. 예: SoftWood의 parentCategory는 Wood")]
        [SerializeField] private ResourceTypeData parentCategory; // 부모 카테고리

        public string ResourceTypeID => resourceTypeID;
        public string DisplayTypeName => displayTypeName;
        public Sprite Icon => icon;
        public string Description => description;
        public Color ResourceColor => resourceColor;
        public ResourceTypeData ParentCategory => parentCategory;

        private void OnValidate()
        {
            // resourceID가 비어있으면 파일 이름을 사용
            if (string.IsNullOrEmpty(resourceTypeID))
            {
                resourceTypeID = name;
            }
        }

        public override string ToString()
        {
            return displayTypeName;
        }

        /// <summary>
        /// 이 자원이 요구되는 자원 타입을 만족하는지 확인
        /// 예: SoftWood가 Wood 요구사항을 만족하는가? → true
        /// </summary>
        public bool CanSatisfy(ResourceTypeData requiredType)
        {
            if (requiredType == null) return false;
            if (this == requiredType) return true;

            // 부모 계층을 따라 올라가며 확인
            ResourceTypeData current = this;
            while (current.parentCategory != null)
            {
                current = current.parentCategory;
                if (current == requiredType) return true;
            }

            return false;
        }

        /// <summary>
        /// 이 자원이 최상위 카테고리인지 확인
        /// </summary>
        public bool IsRootCategory()
        {
            return parentCategory == null;
        }

        /// <summary>
        /// 최상위 카테고리를 반환
        /// </summary>
        public ResourceTypeData GetRootCategory()
        {
            ResourceTypeData current = this;
            while (current.parentCategory != null)
            {
                current = current.parentCategory;
            }
            return current;
        }
    }
}
