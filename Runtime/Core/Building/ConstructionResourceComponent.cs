using System.Collections.Generic;
using UnityEngine;

namespace HanokBuildingSystem
{
    /// <summary>
    /// Building의 건설 관리 컴포넌트 (스테이지 + 자원)
    ///
    /// 사용 방법:
    /// - 단순 사용: 컴포넌트를 추가하지 않으면 건설/자원 시스템이 무시됨
    /// - 기본 사용: 이 컴포넌트를 Building에 추가하면 건설 진행 시스템 활성화
    /// - 고급 사용: 이 클래스를 상속받아 커스텀 건설 시스템 구현
    ///
    /// 예시:
    /// - 단순 비주얼만: Building만 사용 (건설 과정 없음)
    /// - 기본 건설 시스템: Building + ConstructionResourceComponent
    /// - 커스텀 건설: MyCustomConstructionComponent : ConstructionResourceComponent
    /// </summary>
    [RequireComponent(typeof(Building))]
    public class ConstructionResourceComponent : MonoBehaviour
    {
        [Header("Stage Management")]
        [SerializeField] private GameObject[] stageVisuals = new GameObject[0];
        [SerializeField] private int currentStageIndex = 0;

        [Header("Resource Management")]
        [SerializeField] private List<Cost> pendingResources = new List<Cost>(); // 대기 자원
        [SerializeField] private List<Cost> collectedResourcesForCurrentStage = new List<Cost>(); // 현재 스테이지에 할당된 자원

        private Building building;

        public GameObject[] StageVisuals => stageVisuals;
        public int CurrentStageIndex => currentStageIndex;
        public int TotalStages => building != null && building.StatusData != null ? building.StatusData.ConstructionStages.Count : 0;
        public bool IsCompleted => currentStageIndex >= TotalStages;
        public float ConstructionProgress => TotalStages > 0 ? (float)currentStageIndex / TotalStages : 1f;

        public List<Cost> PendingResources => new List<Cost>(pendingResources);
        public List<Cost> CollectedResources => new List<Cost>(collectedResourcesForCurrentStage);

        private void Awake()
        {
            building = GetComponent<Building>();
            if (building == null)
            {
                Debug.LogError("[ConstructionResourceComponent] Building component not found!");
                enabled = false;
                return;
            }
        }

        /// <summary>
        /// 대기 자원에 추가
        /// </summary>
        public void AddPendingResource(ResourceTypeData resourceType, int amount)
        {
            if (resourceType == null || amount <= 0) return;

            // 이미 대기 중인 자원 타입이면 합산
            for (int i = 0; i < pendingResources.Count; i++)
            {
                if (pendingResources[i].ResourceType == resourceType)
                {
                    Cost existingCost = pendingResources[i];
                    pendingResources[i] = new Cost(resourceType, existingCost.Amount + amount);
                    TryAllocateResourcesFromPending();
                    return;
                }
            }

            // 새로운 자원 타입이면 추가
            pendingResources.Add(new Cost(resourceType, amount));
            TryAllocateResourcesFromPending();
        }

        /// <summary>
        /// 대기 자원에서 현재 스테이지로 자원 할당 시도
        /// Building의 Setup()이나 AdvanceStage() 후 호출됨
        /// </summary>
        public void TryAllocateResourcesFromPending()
        {
            if (building == null || building.IsCompleted) return;

            // 스테이지 진행 중이면 할당하지 않음
            if (IsStageInProgress())
            {
                return;
            }

            Cost[] requiredResources = building.GetCurrentStageRequiredResources();
            if (requiredResources == null || requiredResources.Length == 0) return;

            // 각 필요 자원에 대해 대기 자원에서 할당
            foreach (var required in requiredResources)
            {
                if (required.ResourceType == null) continue;

                int currentCollected = GetCollectedAmount(required.ResourceType);
                int stillNeeded = required.Amount - currentCollected;

                if (stillNeeded <= 0) continue; // 이미 충분히 수집됨

                // 대기 자원에서 해당 타입 찾기
                for (int i = 0; i < pendingResources.Count; i++)
                {
                    if (pendingResources[i].ResourceType == required.ResourceType)
                    {
                        int availableAmount = pendingResources[i].Amount;
                        int toAllocate = Mathf.Min(availableAmount, stillNeeded);

                        if (toAllocate > 0)
                        {
                            // 대기 자원에서 차감
                            int remaining = availableAmount - toAllocate;
                            if (remaining > 0)
                            {
                                pendingResources[i] = new Cost(required.ResourceType, remaining);
                            }
                            else
                            {
                                pendingResources.RemoveAt(i);
                            }

                            // 현재 스테이지에 할당
                            AllocateToCurrentStage(required.ResourceType, toAllocate);

                            Debug.Log($"[ConstructionResourceComponent] {building.name}: Allocated {required.ResourceType.name} x{toAllocate} to Stage {building.CurrentStageIndex}");
                        }
                        break;
                    }
                }
            }

            // 모든 자원이 충족되었는지 확인
            if (AreAllResourcesCollected())
            {
                Debug.Log($"[ConstructionResourceComponent] {building.name}: Stage {building.CurrentStageIndex} resources fully collected!");
            }
        }

        /// <summary>
        /// 현재 스테이지에 자원 직접 할당 (내부 사용)
        /// </summary>
        private void AllocateToCurrentStage(ResourceTypeData resourceType, int amount)
        {
            if (resourceType == null || amount <= 0) return;

            // 이미 할당된 자원 타입이면 합산
            for (int i = 0; i < collectedResourcesForCurrentStage.Count; i++)
            {
                if (collectedResourcesForCurrentStage[i].ResourceType == resourceType)
                {
                    Cost existingCost = collectedResourcesForCurrentStage[i];
                    collectedResourcesForCurrentStage[i] = new Cost(resourceType, existingCost.Amount + amount);
                    return;
                }
            }

            // 새로운 자원 타입이면 추가
            collectedResourcesForCurrentStage.Add(new Cost(resourceType, amount));
        }

        /// <summary>
        /// 현재 스테이지가 진행 중인지 확인
        /// </summary>
        private bool IsStageInProgress()
        {
            if (building == null) return false;

            // 자원이 모두 모였고, 건설 진행도가 100% 미만이면 진행 중
            if (AreAllResourcesCollected() && building.StageProgress < 1.0f)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// 현재 단계에서 특정 자원이 얼마나 수집되었는지 확인
        /// </summary>
        public int GetCollectedAmount(ResourceTypeData resourceType)
        {
            if (resourceType == null) return 0;

            foreach (var cost in collectedResourcesForCurrentStage)
            {
                if (cost.ResourceType == resourceType)
                    return cost.Amount;
            }

            return 0;
        }

        /// <summary>
        /// 현재 단계의 모든 필요 자원이 충족되었는지 확인
        /// </summary>
        public bool AreAllResourcesCollected()
        {
            if (building == null) return true;

            Cost[] required = building.GetCurrentStageRequiredResources();

            foreach (var requiredCost in required)
            {
                int collected = GetCollectedAmount(requiredCost.ResourceType);
                if (collected < requiredCost.Amount)
                    return false;
            }

            return true;
        }

        /// <summary>
        /// 현재 스테이지의 자원 수집 진행도 (0~1)
        /// UI 표시용
        /// </summary>
        public float GetResourceProgress()
        {
            if (building == null) return 1f;

            Cost[] required = building.GetCurrentStageRequiredResources();
            if (required == null || required.Length == 0) return 1f;

            float totalRequired = 0;
            float totalCollected = 0;

            foreach (var requiredCost in required)
            {
                totalRequired += requiredCost.Amount;
                totalCollected += GetCollectedAmount(requiredCost.ResourceType);
            }

            if (totalRequired <= 0) return 1f;
            return Mathf.Clamp01(totalCollected / totalRequired);
        }

        /// <summary>
        /// 현재 스테이지 자원 초기화
        /// Building의 AdvanceStage()나 Setup()에서 호출됨
        /// </summary>
        public void ClearCurrentStageResources()
        {
            collectedResourcesForCurrentStage.Clear();
        }

        /// <summary>
        /// 모든 자원 초기화 (대기 자원 포함)
        /// </summary>
        public void ClearAllResources()
        {
            pendingResources.Clear();
            collectedResourcesForCurrentStage.Clear();
        }

        #region Stage Management
        /// <summary>
        /// 스테이지 인덱스 설정 및 비주얼 업데이트
        /// </summary>
        public void SetStageIndex(int index)
        {
            if (building == null || building.StatusData == null) return;

            currentStageIndex = Mathf.Clamp(index, 0, TotalStages);
            UpdateStageVisual();

            if (IsCompleted)
            {
                OnConstructionCompleted();
            }
        }

        /// <summary>
        /// 다음 스테이지로 진행
        /// </summary>
        public bool AdvanceStage()
        {
            if (IsCompleted) return false;

            currentStageIndex++;
            ClearCurrentStageResources();
            UpdateStageVisual();

            if (IsCompleted)
            {
                OnConstructionCompleted();
            }
            else
            {
                Debug.Log($"[ConstructionResourceComponent] {building.name} advanced to stage {currentStageIndex}/{TotalStages}");
                TryAllocateResourcesFromPending();
            }

            return true;
        }

        /// <summary>
        /// 건설 완료 처리
        /// </summary>
        public void CompleteConstruction()
        {
            if (building == null || building.StatusData == null) return;

            currentStageIndex = TotalStages;
            UpdateStageVisual();
            OnConstructionCompleted();
        }

        private void OnConstructionCompleted()
        {
            if (building != null)
            {
                Debug.Log($"[ConstructionResourceComponent] Building {building.name} construction completed!");
            }
        }

        /// <summary>
        /// 현재 단계에 해당하는 비주얼 활성화 (누적 방식: 0~currentStageIndex)
        /// </summary>
        private void UpdateStageVisual()
        {
            if (stageVisuals == null || stageVisuals.Length == 0) return;

            // 모든 단계 비주얼 설정
            for (int i = 0; i < stageVisuals.Length; i++)
            {
                if (stageVisuals[i] != null)
                {
                    stageVisuals[i].SetActive(i <= currentStageIndex);
                }
            }

            // BuildingMember들도 동기화
            UpdateBuildingMembersStage();
        }

        /// <summary>
        /// 모든 BuildingMember를 현재 스테이지로 동기화
        /// </summary>
        private void UpdateBuildingMembersStage()
        {
            if (building == null) return;

            foreach (var memberObj in building.BuildingMembers)
            {
                if (memberObj == null) continue;

                BuildingMember member = memberObj.GetComponent<BuildingMember>();
                if (member != null)
                {
                    member.SetStage(currentStageIndex);
                }
            }
        }

        /// <summary>
        /// 완성 단계의 모습 표시
        /// </summary>
        public void ShowModelBuilding()
        {
            // 완성 단계의 모습
            foreach (GameObject stage in stageVisuals)
            {
                if (stage != null)
                {
                    stage.SetActive(true);
                }
            }

            // BuildingMember들도 모든 스테이지 표시
            if (building != null)
            {
                foreach (var memberObj in building.BuildingMembers)
                {
                    if (memberObj == null) continue;

                    BuildingMember member = memberObj.GetComponent<BuildingMember>();
                    if (member != null)
                    {
                        member.ShowAllStages();
                    }
                }
            }
        }

        /// <summary>
        /// 건설 초기화
        /// </summary>
        public void Setup()
        {
            if (building == null || building.StatusData == null) return;

            currentStageIndex = 0;
            ClearCurrentStageResources();
            UpdateStageVisual();

            // Setup 후 대기 자원이 있으면 할당 시도
            TryAllocateResourcesFromPending();
        }

#if UNITY_EDITOR
        /// <summary>
        /// [Editor] BuildingStatusData의 StageName을 기반으로 Body 아래의 GameObject를 자동으로 찾아 stageVisuals 배열에 할당
        /// </summary>
        public void AutoAssignStageVisuals()
        {
            building = GetComponent<Building>();
            if (building == null || building.StatusData == null || building.Body == null)
            {
                Debug.LogWarning($"[ConstructionResourceComponent] Cannot auto-assign: Building, StatusData, or Body is null.");
                return;
            }

            int stageCount = building.StatusData.ConstructionStages.Count;
            stageVisuals = new GameObject[stageCount];

            for (int i = 0; i < stageCount; i++)
            {
                ConstructionStage stage = building.StatusData.ConstructionStages[i];
                string stageName = stage.StageName;

                if (string.IsNullOrEmpty(stageName))
                {
                    Debug.LogWarning($"[ConstructionResourceComponent] Stage {i} has no name. Skipping.");
                    continue;
                }

                // Body 아래에서 stageName과 일치하는 GameObject 찾기
                Transform stageTransform = building.Body.transform.Find(stageName);

                if (stageTransform == null)
                {
                    Debug.LogWarning($"[ConstructionResourceComponent] Stage {i} '{stageName}' not found under Body.");
                }
                else
                {
                    stageTransform.gameObject.SetActive(true);
                    stageVisuals[i] = stageTransform.gameObject;
                }
            }
        }
#endif
        #endregion
    }
}
