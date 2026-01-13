using System.ComponentModel;
using UnityEngine;

namespace HanokBuildingSystem
{
    [RequireComponent(typeof(Building))]
    public class DurabilityComponent : MonoBehaviour
    {
        [Header("Durability Settings")]
        [SerializeField][Range(0,1)] private float durabilityPercent = 1;
        [SerializeField] private float durabilityDecayRate = 1f;
        [SerializeField] [ReadOnly] private float currentDurability = 100f;
        [SerializeField] [ReadOnly] private float maxDurability = 100f;

        [Header("State")]
        [SerializeField] private EnvironmentState currentEnvironmentState = EnvironmentState.Normal;
        [SerializeField] private bool isDeteriorated = false;

        private Building building;
        private BuildingStatusData statusData;

        public float CurrentDurability => currentDurability;
        public float MaxDurability => maxDurability;
        public float DurabilityDecayRate => durabilityDecayRate;
        public float DurabilityPercent => durabilityPercent;
        public bool IsDeteriorated => isDeteriorated;
        public EnvironmentState CurrentEnvironmentState => currentEnvironmentState;

        private void Awake()
        {
            building = GetComponent<Building>();
            if (building == null)
            {
                Debug.LogError("DurabilityComponent requires a Building component on the same GameObject!");
                enabled = false;
                return;
            }
        }

        public void SetupFromStatusData(BuildingStatusData data)
        {
            if (data == null) return;

            statusData = data;
            maxDurability = data.MaxDurability;
            currentDurability = maxDurability * durabilityPercent;
        }

        public void SetEnvironmentState(EnvironmentState newState)
        {
            currentEnvironmentState = newState;
            UpdateDeterioratedState();
        }

        public void TakeDamage(float damage)
        {
            if (damage <= 0) return;

            float resistance = GetEnvironmentResistance();
            float actualDamage = damage * (1f - resistance);

            currentDurability = Mathf.Max(0, currentDurability - actualDamage);
            UpdateDeterioratedState();

            if (currentDurability <= 0)
            {
                OnDurabilityDepleted();
            }
        }

        public void Repair(float amount)
        {
            if (amount <= 0) return;

            currentDurability = Mathf.Min(maxDurability, currentDurability + amount);
            UpdateDeterioratedState();
        }

        public void RepairToFull()
        {
            currentDurability = maxDurability;
            UpdateDeterioratedState();
        }

        public void ApplyEnvironmentalDecay(float deltaTime)
        {
            if (!building.IsCompleted) return;
            if (durabilityDecayRate <= 0) return;

            float resistance = GetEnvironmentResistance();
            float decayAmount = durabilityDecayRate * (1f - resistance) * deltaTime;

            currentDurability = Mathf.Max(0, currentDurability - decayAmount);
            UpdateDeterioratedState();

            if (currentDurability <= 0)
            {
                OnDurabilityDepleted();
            }
        }

        public float GetEnvironmentResistance()
        {
            if (statusData != null)
            {
                return statusData.GetEnvironmentResistance(currentEnvironmentState);
            }
            return 0f;
        }

        private void UpdateDeterioratedState()
        {
            bool wasDeteriorated = isDeteriorated;
            isDeteriorated = DurabilityPercent <= 0.3f;

            if (!wasDeteriorated && isDeteriorated)
            {
                OnBecameDeteriorated();
            }
            else if (wasDeteriorated && !isDeteriorated)
            {
                OnRecoveredFromDeterioration();
            }
        }

        private void OnDurabilityDepleted()
        {
            Debug.Log($"Building {building.name} durability depleted!");
        }

        private void OnBecameDeteriorated()
        {
            Debug.Log($"Building {building.name} became deteriorated (durability: {DurabilityPercent:P0})");
        }

        private void OnRecoveredFromDeterioration()
        {
            Debug.Log($"Building {building.name} recovered from deterioration (durability: {DurabilityPercent:P0})");
        }

        public void SetMaxDurability(float value)
        {
            maxDurability = Mathf.Max(1f, value);
            currentDurability = maxDurability * durabilityPercent;
        }

        public void SetDurabilityDecayRate(float value)
        {
            durabilityDecayRate = Mathf.Max(0f, value);
        }

        private void Update()
        {
            if (building != null && building.IsCompleted)
            {
                ApplyEnvironmentalDecay(Time.deltaTime);
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            maxDurability = Mathf.Max(1f, maxDurability);
            currentDurability = maxDurability * durabilityPercent;
            durabilityDecayRate = Mathf.Max(0f, durabilityDecayRate);
        }
#endif
    }
}
