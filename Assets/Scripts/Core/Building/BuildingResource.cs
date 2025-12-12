using System;
using UnityEngine;

namespace HanokBuildingSystem
{
    [Serializable]
    public struct Cost
    {
        [SerializeField] private ResourceTypeData resourceType;
        [SerializeField] private int amount;

        public ResourceTypeData ResourceType => resourceType;
        public int Amount => amount;

        public Cost(ResourceTypeData resourceType, int amount)
        {
            this.resourceType = resourceType;
            this.amount = amount;
        }

        /// <summary>
        /// 주어진 자원이 이 비용의 요구사항을 만족하는지 확인
        /// 예: Cost가 Wood를 요구할 때, SoftWood나 HardWood도 사용 가능
        /// </summary>
        public bool CanBeSatisfiedBy(ResourceTypeData availableResource)
        {
            if (resourceType == null || availableResource == null)
            {
                return false;
            }

            return availableResource.CanSatisfy(resourceType);
        }

        /// <summary>
        /// 두 비용이 같은 자원 타입인지 확인 (계층 구조 무시)
        /// </summary>
        public bool IsSameExactType(Cost other)
        {
            return this.resourceType == other.resourceType;
        }

        /// <summary>
        /// 두 비용이 호환되는 자원 타입인지 확인 (계층 구조 고려)
        /// </summary>
        public bool IsCompatibleWith(Cost other)
        {
            if (resourceType == null || other.resourceType == null)
            {
                return false;
            }

            return resourceType.CanSatisfy(other.resourceType) ||
                   other.resourceType.CanSatisfy(resourceType);
        }
    }

    [Serializable]
    public class ConstructionStage
    {
        [SerializeField] private Cost[] requiredResources;

        public Cost[] RequiredResources => requiredResources;

        public ConstructionStage()
        {
            this.requiredResources = new Cost[0];
        }

        public ConstructionStage(Cost[] requiredResources)
        {
            this.requiredResources = requiredResources;
        }
    }

    public enum EnvironmentState
    {
        Normal,
        Fire,
        Rain,
        Snow,
        Deteriorated
    }
}
