using System.Collections.Generic;
using UnityEngine;

namespace HanokBuildingSystem
{
    [CreateAssetMenu(fileName = "BuildingStatusData", menuName = "HanokBuildingSystem/Building Status Data", order = 1)]
    public class BuildingStatusData : ScriptableObject
    {
        [Header("Building Information")]
        [SerializeField] private string buildingName;
        [SerializeField] private BuildingType buildingType;
        [SerializeField] private string description;

        [Header("Construction Stage")]
        [SerializeField] private List<ConstructionStage> constructionStages = new List<ConstructionStage>();

        [Header("Configuration")]
        [SerializeField] private float maxDurability = 100f;
        [SerializeField] private Vector3 defaultSize = Vector3.one;

        [Header("Environment Effects")]
        [SerializeField] private float fireResistance = 0.5f;
        [SerializeField] private float waterResistance = 0.7f;
        [SerializeField] private float quakeResistance = 0.8f;

        [Header("Visual Prefabs")]
        [SerializeField] private GameObject completedPrefab;
        [SerializeField] private Material normalMaterial;
        [SerializeField] private Material fireMaterial;
        [SerializeField] private Material rainMaterial;
        [SerializeField] private Material snowMaterial;
        [SerializeField] private Material deterioratedMaterial;

        public string BuildingName => buildingName;
        public BuildingType BuildingType => buildingType;
        public string Description => description;
        public List<ConstructionStage> ConstructionStages => constructionStages;
        public float MaxDurability => maxDurability;
        public Vector3 DefaultSize => defaultSize;
        public float FireResistance => fireResistance;
        public float WaterResistance => waterResistance;
        public float SnowResistance => quakeResistance;
        public GameObject CompletedPrefab => completedPrefab;
        public Material NormalMaterial => normalMaterial;
        public Material FireMaterial => fireMaterial;
        public Material RainMaterial => rainMaterial;
        public Material SnowMaterial => snowMaterial;
        public Material DeterioratedMaterial => deterioratedMaterial;

        public Cost[] GetStageRequiredResources(int stageIndex)
        {
            if (stageIndex >= 0 && stageIndex < constructionStages.Count)
            {
                return constructionStages[stageIndex].RequiredResources;
            }
            return new Cost[0];
        }

        public float GetEnvironmentResistance(EnvironmentState state)
        {
            switch (state)
            {
                case EnvironmentState.Fire:
                    return fireResistance;
                case EnvironmentState.Rain:
                    return waterResistance;
                case EnvironmentState.Snow:
                    return quakeResistance;
                default:
                    return 1f;
            }
        }

        public Material GetMaterialForState(EnvironmentState state)
        {
            switch (state)
            {
                case EnvironmentState.Fire:
                    return fireMaterial;
                case EnvironmentState.Rain:
                    return rainMaterial;
                case EnvironmentState.Snow:
                    return snowMaterial;
                case EnvironmentState.Deteriorated:
                    return deterioratedMaterial;
                default:
                    return normalMaterial;
            }
        }

        private void OnValidate()
        {
            maxDurability = Mathf.Max(1f, maxDurability);
            fireResistance = Mathf.Clamp01(fireResistance);
            waterResistance = Mathf.Clamp01(waterResistance);
            quakeResistance = Mathf.Clamp01(quakeResistance);
        }
    }
}
