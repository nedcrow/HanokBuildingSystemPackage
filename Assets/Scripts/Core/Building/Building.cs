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
        [SerializeField] private Vector3 size = Vector3.one;
        [SerializeField] private BuildingStatusData statusData;
        [SerializeField] private bool allowManualRotation = true;

        [Header("Construction")]
        [SerializeField] private int currentStageIndex = 0;
        [SerializeField] private ConstructionMode constructionMode = ConstructionMode.Instant;
        
        [Header("Time-Based Settings")]
        [SerializeField] private float constructionDuration = 10f; // TimeBased: 각 단계당 소요 시간(초)
        private Coroutine timeBasedCoroutine = null;

        [Header("Labor-Based Settings")]
        [SerializeField] private float requiredLaborPerStage = 100f; // LaborBased: 각 단계당 필요 노동량

        [Header("Building Members")]
        [SerializeField] private List<GameObject> buildingMembers = new List<GameObject>();

        protected GameObject body;
        public Vector3 Size => size;
        public BuildingStatusData StatusData => statusData;
        public bool AllowManualRotation => allowManualRotation;
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
                        // 코루틴 진행 중이면 진행 중으로 표시
                        return timeBasedCoroutine != null ? 0.5f : 0f;
                    case ConstructionMode.LaborBased:
                        LaborComponent laborComp = GetComponent<LaborComponent>();
                        return laborComp != null ? laborComp.LaborProgress : 0f;
                    default:
                        return 0f;
                }
            }
        }

        private void Awake()
        {
            InitializeBuilding();
        }



        private void InitializeBuilding()
        {
            if (buildingMembers == null)
            {
                buildingMembers = new List<GameObject>();
            }

            SetBody();
        }

        private void SetBody()
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

            size = statusData.DefaultSize;
            currentStageIndex = 0;
            ResetStageProgress(); // 건설 진행도 초기화

            UpdateBuildingScale();

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

        public void SetSize(Vector3 newSize)
        {
            size = newSize;
            UpdateBuildingScale();
        }

        #region Construction Progress Methods
        private System.Collections.IEnumerator TimeBasedConstructionCoroutine()
        {
            while (!IsCompleted)
            {
                yield return new WaitForSeconds(constructionDuration);
                
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
                Debug.Log($"[Building] {name} started TimeBased construction (duration: {constructionDuration}s per stage)");
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
            ResetStageProgress(); // 다음 단계를 위해 진행도 초기화

            if (IsCompleted)
            {
                OnConstructionCompleted();
            }
            else
            {
                Debug.Log($"[Building] {name} advanced to stage {currentStageIndex}/{TotalStages} (Progress: {StageProgress:P0})");
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

        public void SetStageIndex(int index)
        {
            currentStageIndex = Mathf.Clamp(index, 0, statusData.ConstructionStages.Count);

            if (IsCompleted)
            {
                OnConstructionCompleted();
            }
        }

        public void CompleteConstruction()
        {
            currentStageIndex = statusData.ConstructionStages.Count;
            ResetStageProgress();
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

        private void UpdateBuildingScale()
        {
            transform.localScale = size;
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
        }

        private void OnDestroy()
        {
            StopTimeBasedConstruction();
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
#endif
    }
}
