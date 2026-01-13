using System.Collections.Generic;
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

        [Header("Construction")]
        [SerializeField] private ConstructionMode constructionMode = ConstructionMode.Instant;

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
        public List<GameObject> BuildingMembers => buildingMembers;

        // Stage 관련 속성들 - ConstructionResourceComponent에 위임
        public GameObject[] StageVisuals
        {
            get
            {
                var constructionComp = GetComponent<ConstructionResourceComponent>();
                return constructionComp != null ? constructionComp.StageVisuals : new GameObject[0];
            }
        }

        public int CurrentStageIndex
        {
            get
            {
                var constructionComp = GetComponent<ConstructionResourceComponent>();
                return constructionComp != null ? constructionComp.CurrentStageIndex : 0;
            }
        }

        public int TotalStages
        {
            get
            {
                var constructionComp = GetComponent<ConstructionResourceComponent>();
                return constructionComp != null ? constructionComp.TotalStages : (statusData?.ConstructionStages.Count ?? 0);
            }
        }

        public bool IsCompleted
        {
            get
            {
                var constructionComp = GetComponent<ConstructionResourceComponent>();
                return constructionComp != null ? constructionComp.IsCompleted : true;
            }
        }

        public float ConstructionProgress
        {
            get
            {
                var constructionComp = GetComponent<ConstructionResourceComponent>();
                return constructionComp != null ? constructionComp.ConstructionProgress : 1f;
            }
        }
        
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

            // ConstructionResourceComponent가 있으면 위임
            ConstructionResourceComponent constructionComp = GetComponent<ConstructionResourceComponent>();
            if (constructionComp != null)
            {
                constructionComp.Setup();
            }

            ResetStageProgress(); // 건설 진행도 초기화

            DurabilityComponent durability = GetComponent<DurabilityComponent>();
            if (durability != null)
            {
                durability.SetupFromStatusData(statusData);
            }

            if (constructionMode == ConstructionMode.TimeBased)
            {
                StartTimeBasedConstruction();
            }
        }

        #region Construction Progress Methods
        private System.Collections.IEnumerator TimeBasedConstructionCoroutine()
        {
            const float updateInterval = 0.1f; // 0.1초마다 업데이트
            ConstructionResourceComponent resourceComp = GetComponent<ConstructionResourceComponent>();

            while (!IsCompleted)
            {
                currentConstructionDuration = 0f;

                // 현재 단계 진행
                while (currentConstructionDuration < constructionDuration && !IsCompleted)
                {
                    yield return new WaitForSeconds(updateInterval);

                    // 현재 단계에 필요한 자원이 모두 마련되어야만 진행
                    // 자원 컴포넌트가 없으면 자원 체크 생략
                    bool resourcesReady = resourceComp == null || resourceComp.AreAllResourcesCollected();
                    if (resourcesReady)
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

            // ConstructionResourceComponent가 있으면 위임
            ConstructionResourceComponent constructionComp = GetComponent<ConstructionResourceComponent>();
            if (constructionComp != null)
            {
                bool result = constructionComp.AdvanceStage();
                ResetStageProgress(); // 다음 단계를 위해 진행도 초기화
                return result;
            }

            // 컴포넌트가 없으면 기본 동작 (호환성)
            Debug.LogWarning($"[Building] {name}: AdvanceStage called but no ConstructionResourceComponent found.");
            return false;
        }

        public bool CanAdvanceStage()
        {
            return !IsCompleted;
        }

        public ConstructionStage GetCurrentStage()
        {
            if (CurrentStageIndex >= 0 && CurrentStageIndex < statusData.ConstructionStages.Count)
            {
                return statusData.ConstructionStages[CurrentStageIndex];
            }
            return null;
        }

        public Cost[] GetCurrentStageRequiredResources()
        {
            var stage = GetCurrentStage();
            return stage?.RequiredResources ?? new Cost[0];
        }

        public void SetStageIndex(int index)
        {
            // ConstructionResourceComponent가 있으면 위임
            ConstructionResourceComponent constructionComp = GetComponent<ConstructionResourceComponent>();
            if (constructionComp != null)
            {
                constructionComp.SetStageIndex(index);
                return;
            }

            // 컴포넌트가 없으면 기본 동작 (호환성)
            Debug.LogWarning($"[Building] {name}: SetStageIndex called but no ConstructionResourceComponent found.");
        }

        public void CompleteConstruction()
        {
            // ConstructionResourceComponent가 있으면 위임
            ConstructionResourceComponent constructionComp = GetComponent<ConstructionResourceComponent>();
            if (constructionComp != null)
            {
                constructionComp.CompleteConstruction();
                ResetStageProgress();
                return;
            }

            // 컴포넌트가 없으면 기본 동작 (호환성)
            Debug.LogWarning($"[Building] {name}: CompleteConstruction called but no ConstructionResourceComponent found.");
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
            // ConstructionResourceComponent가 있으면 위임
            ConstructionResourceComponent constructionComp = GetComponent<ConstructionResourceComponent>();
            if (constructionComp != null)
            {
                constructionComp.ShowModelBuilding();
                return;
            }

            // 컴포넌트가 없으면 BuildingMember들만 모든 스테이지 표시 (호환성)
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
        /// [Editor] BuildingStatusData의 StageName을 기반으로 Body 아래의 GameObject를 자동으로 찾아 stageVisuals 배열에 할당
        /// ConstructionResourceComponent에 위임
        /// </summary>
        public void AutoAssignStageVisuals4Editor()
        {
            // ConstructionResourceComponent가 있으면 위임
            ConstructionResourceComponent constructionComp = GetComponent<ConstructionResourceComponent>();
            if (constructionComp != null)
            {
                constructionComp.AutoAssignStageVisuals();
                return;
            }

            // 컴포넌트가 없으면 경고
            Debug.LogWarning($"[Building] {name}: AutoAssignStageVisuals called but no ConstructionResourceComponent found. Please add ConstructionResourceComponent to use stage visuals.");
        }
#endif
    }
}
