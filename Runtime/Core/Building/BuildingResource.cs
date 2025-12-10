using System;
using UnityEngine;

namespace HanokBuildingSystem
{
    [Serializable]
    public struct Cost
    {
        [SerializeField] private ResourceType resourceName;
        [SerializeField] private int amount;

        public ResourceType ResourceName => resourceName;
        public int Amount => amount;

        public Cost(ResourceType resourceName, int amount)
        {
            this.resourceName = resourceName;
            this.amount = amount;
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
