using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor.Animations;
using UnityEngine;

namespace HanokBuildingSystem
{
    public enum ConstructionMode
    {
        Instant,
        TimeBased,
        LaborBased
    }
    public class Building : MonoBehaviour
    {
        [Header("Building Configuration")]
        [SerializeField] private BuildingStatusData statusData;
        [SerializeField] private bool allowManualRotation = true;

        [Header("Stage Visuals")]
        [SerializeField] private GameObject[] stageVisuals = new GameObject[0];

        [Header("Construction")]
        [SerializeField] private int currentStageIndex = 0;
        [SerializeField] private ConstructionMode constructionMode = ConstructionMode.Instant;
        [SerializeField] private List<Cost> pendingResources = new List<Cost>(); // 대기 자원
        [SerializeField] private List<Cost> collectedResourcesForCurrentStage = new List<Cost>(); // 현재 스테이지에 할당된 자원
        
        [Header("Time-Based Settings")]
        [SerializeField] private float constructionDuration = 10f; // TimeBased: 각 단계당 소요 시간(초)
        [SerializeField] private float currentConstructionDuration = 0f; // 현재 단계의 진행 시간
        private Coroutine timeBasedCoroutine = null;

        [Header("Labor-Based Settings")]
        [SerializeField] private float requiredLaborPerStage = 100f; // LaborBased: 각 단계당 필요 노동량

        [Header("Building Members")]
        [SerializeField] private List<GameObject> buildingMembers = new List<GameObject>();

        [SerializeField] private GameObject body;
        [SerializeField] public GameObject Body => body;
        public Vector3 Size => statusData != null ? statusData.DefaultSize : Vector3.one;
        public BuildingStatusData StatusData => statusData;
        public bool AllowManualRotation => allowManualRotation;
        public GameObject[] StageVisuals => stageVisuals;
        public int CurrentStageIndex => currentStageIndex;
        public int TotalStages => statusData.ConstructionStages.Count;
        public bool IsCompleted => currentStageIndex >= statusData.ConstructionStages.Count;
        public float ConstructionProgress => statusData.ConstructionStages.Count > 0 ? (float)currentStageIndex / statusData.ConstructionStages.Count : 1f;
        public List<GameObject> BuildingMembers => buildingMembers;
        
        // Construction Mode Properties
        public ConstructionMode Mode => constructionMode;
        public float ConstructionDuration => constructionDuration;
        
        /// <summary>
        /// 현재 단계의 진행도 (0~1)
        /// </summary>
        public float StageProgress
        {
            get
            {
                if (IsCompleted) return 1f;

                switch (constructionMode)
                {
                    case ConstructionMode.Instant:
                        return 1f;
                    case ConstructionMode.TimeBased:
                        if (constructionDuration <= 0f) return 0f;
                        return Mathf.Clamp01(currentConstructionDuration / constructionDuration);
                    case ConstructionMode.LaborBased:
                        LaborComponent laborComp = GetComponent<LaborComponent>();
                        return laborComp != null ? laborComp.LaborProgress : 0f;
                    default:
                        return 0f;
                }
            }
        }

        private House parentHouse;

        private void Awake()
        {
            InitializeBuilding();
        }

        private void Start()
        {
            // 부모 House 찾기 및 이벤트 구독
            parentHouse = GetComponentInParent<House>();
            if (parentHouse != null)
            {
                parentHouse.Events.OnConstructionStarted += HandleConstructionStarted;
                parentHouse.Events.OnShowModelHouse += HandleShowModelHouse;
            }
        }

        private void InitializeBuilding()
        {
            if (buildingMembers == null)
            {
                buildingMembers = new List<GameObject>();
            }

            SetBody();
        }

        public void SetBody()
        {
            if(body == null)
            {
                Transform[] childs = GetComponentsInChildren<Transform>();
                foreach(var child in childs)
                {
                    if(child.name == "Body") {
                        body = child.gameObject;
                        break;
                    }
                }
            }

            if(body == null)
            {
                body = new GameObject("Body");
                body.transform.position = Vector3.zero;
                body.transform.SetParent(transform);
            }
        }

        public void Setup()
        {
            if (statusData == null) return;

            currentStageIndex = 0;
            collectedResourcesForCurrentStage.Clear(); // 현재 스테이지 자원만 초기화 (대기 자원은 유지)
            ResetStageProgress(); // 건설 진행도 초기화

            UpdateStageVisual(); // 현재 단계의 비주얼 활성화

            DurabilityComponent durability = GetComponent<DurabilityComponent>();
            if (durability != null)
            {
                durability.SetupFromStatusData(statusData);
            }

            if (constructionMode == ConstructionMode.TimeBased)
            {
                StartTimeBasedConstruction();
            }

            // Setup 후 대기 자원이 있으면 할당 시도
            TryAllocateResourcesFromPending();
        }

        #region Construction Progress Methods
        private System.Collections.IEnumerator TimeBasedConstructionCoroutine()
        {
            const float updateInterval = 0.1f; // 0.1초마다 업데이트

            while (!IsCompleted)
            {
                currentConstructionDuration = 0f;

                // 현재 단계 진행
                while (currentConstructionDuration < constructionDuration && !IsCompleted)
                {
                    yield return new WaitForSeconds(updateInterval);

                    // 현재 단계에 필요한 자원이 모두 마련되어야만 진행
                    if (AreAllResourcesCollected())
                    {
                        currentConstructionDuration += updateInterval;
                    }
                }

                if (!IsCompleted)
                {
                    AdvanceStage();
                    Debug.Log($"[Building] {name} TimeBased construction advanced to stage {CurrentStageIndex}/{TotalStages}");
                }
            }

            timeBasedCoroutine = null;
        }

        private void StartTimeBasedConstruction()
        {
            StopTimeBasedConstruction(); // 기존 코루틴 중지
            
            if (!IsCompleted && constructionMode == ConstructionMode.TimeBased)
            {
                timeBasedCoroutine = StartCoroutine(TimeBasedConstructionCoroutine());
            }
        }

        private void StopTimeBasedConstruction()
        {
            if (timeBasedCoroutine != null)
            {
                StopCoroutine(timeBasedCoroutine);
                timeBasedCoroutine = null;
            }
        }

        /// <summary>
        /// 건설 모드 변경
        /// TimeBased로 변경 시 코루틴을 자동으로 시작합니다.
        /// LaborBased로 변경 시 LaborComponent를 자동으로 추가합니다.
        /// </summary>
        public void SetConstructionMode(ConstructionMode mode)
        {
            ConstructionMode oldMode = constructionMode;
            constructionMode = mode;

            // TimeBased 모드 처리
            if (mode == ConstructionMode.TimeBased)
            {
                StartTimeBasedConstruction();
            }
            else
            {
                StopTimeBasedConstruction();
            }

            // LaborBased로 변경 시 LaborComponent 추가
            if (mode == ConstructionMode.LaborBased && oldMode != ConstructionMode.LaborBased)
            {
                LaborComponent laborComp = GetComponent<LaborComponent>();
                if (laborComp == null)
                {
                    laborComp = gameObject.AddComponent<LaborComponent>();
                    Debug.Log($"[Building] Added LaborComponent to {name} (mode changed to LaborBased)");
                }
                
                // Building의 설정값을 LaborComponent에 전달
                laborComp.SetRequiredLaborPerStage(requiredLaborPerStage);
            }
            // LaborBased에서 다른 모드로 변경 시 LaborComponent 제거 (선택사항)
            else if (mode != ConstructionMode.LaborBased && oldMode == ConstructionMode.LaborBased)
            {
                LaborComponent laborComp = GetComponent<LaborComponent>();
                if (laborComp != null)
                {
                    Destroy(laborComp);
                    Debug.Log($"[Building] Removed LaborComponent from {name} (mode changed from LaborBased)");
                }
            }

            ResetStageProgress();
        }

        private void ResetStageProgress()
        {
            currentConstructionDuration = 0f;

            if (constructionMode == ConstructionMode.TimeBased)
            {
                StartTimeBasedConstruction();
            }

            LaborComponent laborComp = GetComponent<LaborComponent>();
            if (laborComp != null)
            {
                laborComp.ResetLabor();
            }
        }
        #endregion

        public bool AdvanceStage()
        {
            if (IsCompleted) return false;

            currentStageIndex++;
            collectedResourcesForCurrentStage.Clear(); // 다음 단계로 넘어갈 때 현재 스테이지 자원만 초기화 (대기 자원은 유지)
            ResetStageProgress(); // 다음 단계를 위해 진행도 초기화
            UpdateStageVisual(); // 단계 변경 시 비주얼 업데이트

            if (IsCompleted)
            {
                OnConstructionCompleted();
                // 건설 완료 시에도 대기 자원은 유지
            }
            else
            {
                Debug.Log($"[Building] {name} advanced to stage {currentStageIndex}/{TotalStages} (Progress: {StageProgress:P0})");

                // 다음 스테이지로 이동했으니 대기 자원에서 할당 시도
                TryAllocateResourcesFromPending();
            }

            return true;
        }

        public bool CanAdvanceStage()
        {
            return !IsCompleted;
        }

        public ConstructionStage GetCurrentStage()
        {
            if (currentStageIndex >= 0 && currentStageIndex < statusData.ConstructionStages.Count)
            {
                return statusData.ConstructionStages[currentStageIndex];
            }
            return null;
        }

        public Cost[] GetCurrentStageRequiredResources()
        {
            var stage = GetCurrentStage();
            return stage?.RequiredResources ?? new Cost[0];
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
                    TryAllocateResourcesFromPending(); // 할당 시도
                    return;
                }
            }

            // 새로운 자원 타입이면 추가
            pendingResources.Add(new Cost(resourceType, amount));
            TryAllocateResourcesFromPending(); // 할당 시도
        }

        /// <summary>
        /// 대기 자원에서 현재 스테이지로 자원 할당 시도
        /// 스테이지 진행 중이 아닐 때만 할당
        /// </summary>
        private void TryAllocateResourcesFromPending()
        {
            if (IsCompleted) return;

            // 스테이지 진행 중이면 할당하지 않음
            if (IsStageInProgress())
            {
                return;
            }

            Cost[] requiredResources = GetCurrentStageRequiredResources();
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

                            Debug.Log($"[Building] {name}: Allocated {required.ResourceType.name} x{toAllocate} to Stage {currentStageIndex}");
                        }
                        break;
                    }
                }
            }

            // 모든 자원이 충족되었는지 확인
            if (AreAllResourcesCollected())
            {
                Debug.Log($"[Building] {name}: Stage {currentStageIndex} resources fully collected!");
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
            // 자원이 모두 모였고, 건설 진행도가 100% 미만이면 진행 중
            if (AreAllResourcesCollected() && StageProgress < 1.0f)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// 현재 단계에 수집된 자원 목록
        /// </summary>
        public List<Cost> GetCollectedResources()
        {
            return new List<Cost>(collectedResourcesForCurrentStage);
        }

        /// <summary>
        /// 대기 자원 목록
        /// </summary>
        public List<Cost> GetPendingResources()
        {
            return new List<Cost>(pendingResources);
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
            Cost[] required = GetCurrentStageRequiredResources();

            foreach (var requiredCost in required)
            {
                int collected = GetCollectedAmount(requiredCost.ResourceType);
                if (collected < requiredCost.Amount)
                    return false;
            }

            return true;
        }

        public void SetStageIndex(int index)
        {
            currentStageIndex = Mathf.Clamp(index, 0, statusData.ConstructionStages.Count);
            UpdateStageVisual();

            if (IsCompleted)
            {
                OnConstructionCompleted();
            }
        }

        public void CompleteConstruction()
        {
            currentStageIndex = statusData.ConstructionStages.Count;
            ResetStageProgress();
            UpdateStageVisual();
            OnConstructionCompleted();
        }

        private void OnConstructionCompleted()
        {
            Debug.Log($"Building {name} construction completed!");
        }

        public void AddBuildingMember(GameObject member)
        {
            if (member == null)
            {
                Debug.LogWarning("Cannot add null building member");
                return;
            }

            if (!buildingMembers.Contains(member))
            {
                buildingMembers.Add(member);
                member.transform.SetParent(transform);
            }
        }

        public void RemoveBuildingMember(GameObject member)
        {
            if (member != null && buildingMembers.Contains(member))
            {
                buildingMembers.Remove(member);
            }
        }

        /// <summary>
        /// 현재 단계에 해당하는 비주얼만 활성화
        /// </summary>
        private void UpdateStageVisual()
        {
            if (stageVisuals == null || stageVisuals.Length == 0) return;

            // 모든 단계 비주얼 비활성화
            for (int i = 0; i < stageVisuals.Length; i++)
            {
                if (stageVisuals[i] != null)
                {
                    stageVisuals[i].SetActive(i <= currentStageIndex ? true : false);
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
            foreach (var memberObj in buildingMembers)
            {
                if (memberObj == null) continue;

                BuildingMember member = memberObj.GetComponent<BuildingMember>();
                if (member != null)
                {
                    member.SetStage(currentStageIndex);
                }
            }
        }

        public void Rotate(float angle)
        {
            transform.Rotate(Vector3.up, angle);
        }

        public void MoveTo(Vector3 position)
        {
            transform.position = position;
        }

        /// <summary>
        /// 완성 단계의 모습
        /// </summary>
        public virtual void ShowModelBuilding(Plot plot, Transform parent = null)
        {
            // 완성 단계의 모습
            foreach(GameObject stage in StageVisuals)
            {
                stage.SetActive(true);
            }

            // BuildingMember들도 모든 스테이지 표시
            foreach (var memberObj in buildingMembers)
            {
                if (memberObj == null) continue;

                BuildingMember member = memberObj.GetComponent<BuildingMember>();
                if (member != null)
                {
                    member.ShowAllStages();
                }
            }
        }

        #region House Event Handlers
        /// <summary>
        /// House의 건설 시작 이벤트 핸들러
        /// </summary>
        private void HandleConstructionStarted(House house, Plot plot)
        {
            if (house != parentHouse) return;

            // 빌딩을 Stage 0으로 초기화
            Setup();
            SetStageIndex(0);

            Debug.Log($"[Building] {name}: Initialized to Stage 0 for construction");
        }

        /// <summary>
        /// House의 모델 하우스 표시 이벤트 핸들러
        /// </summary>
        private void HandleShowModelHouse(House house, Plot plot)
        {
            if (house != parentHouse) return;

            // 완성 단계의 모습 표시
            ShowModelBuilding(plot, house.transform);
        }
        #endregion

        private void OnDestroy()
        {
            StopTimeBasedConstruction();

            // 이벤트 구독 해제
            if (parentHouse != null)
            {
                parentHouse.Events.OnConstructionStarted -= HandleConstructionStarted;
                parentHouse.Events.OnShowModelHouse -= HandleShowModelHouse;
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            // Body
            SetBody();

            // LaborBased 모드일 때 LaborComponent 설정 동기화
            if (constructionMode == ConstructionMode.LaborBased)
            {
                LaborComponent laborComp = GetComponent<LaborComponent>();
                if (laborComp != null)
                {
                    laborComp.SetRequiredLaborPerStage(requiredLaborPerStage);
                }
            }

            // 값 검증
            constructionDuration = Mathf.Max(0.1f, constructionDuration);
            requiredLaborPerStage = Mathf.Max(1f, requiredLaborPerStage);
        }

        /// <summary>
        /// BuildingStatusData의 StageName을 기반으로 Body 아래의 GameObject를 자동으로 찾아 stageVisuals 배열에 할당
        /// </summary>
        public void AutoAssignStageVisuals4Editor()
        {
            if (statusData == null || body == null)
            {
                Debug.LogWarning($"[Building] {name}: Cannot auto-assign stage visuals. StatusData or Body is null.");
                return;
            }

            SetBody();

            int stageCount = statusData.ConstructionStages.Count;
            stageVisuals = new GameObject[stageCount];

            for (int i = 0; i < stageCount; i++)
            {
                ConstructionStage stage = statusData.ConstructionStages[i];
                string stageName = stage.StageName;

                if (string.IsNullOrEmpty(stageName))
                {
                    Debug.LogWarning($"[Building] {name}: Stage {i} has no name. Skipping.");
                    continue;
                }

                // Body 아래에서 stageName과 일치하는 GameObject 찾기
                Transform stageTransform = body.transform.Find(stageName);

                if (stageTransform == null)
                {                    
                    Debug.LogWarning($"[Building] {name}: Stage {i} '{stageName}' not found under Body.");
                }
                else
                {
                    stageTransform.gameObject.SetActive(true);
                    stageVisuals[i] = stageTransform.gameObject;
                }
            }
        }
#endif
    }
}
