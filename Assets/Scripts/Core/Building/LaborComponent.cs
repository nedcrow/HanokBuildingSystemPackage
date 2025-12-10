using UnityEngine;

namespace HanokBuildingSystem
{
    /// <summary>
    /// 노동력 기반 건설 진행을 담당하는 컴포넌트
    /// ConstructionMode가 LaborBased일 때 사용됩니다.
    /// </summary>
    [RequireComponent(typeof(Building))]
    public class LaborComponent : MonoBehaviour
    {
        [Header("Labor Settings (Building에서 설정)")]
        [SerializeField] private float requiredLaborPerStage = 100f; // 각 단계당 필요 노동량
        [SerializeField] private float laborPerHit = 1f; // 망치질 1회당 노동량
        
        private float currentLabor = 0f;
        private Building building;

        public float RequiredLaborPerStage => requiredLaborPerStage;
        public float LaborPerHit => laborPerHit;
        public float CurrentLabor => currentLabor;
        
        /// <summary>
        /// 현재 단계의 노동 진행도 (0~1)
        /// </summary>
        public float LaborProgress => Mathf.Clamp01(currentLabor / requiredLaborPerStage);

        private void Awake()
        {
            building = GetComponent<Building>();
            if (building == null)
            {
                Debug.LogError("[LaborComponent] requires a Building component on the same GameObject!");
                enabled = false;
                return;
            }

            InitializeLabor();
        }

        private void InitializeLabor()
        {
            currentLabor = 0f;
        }

        /// <summary>
        /// 일꾼이 망치질할 때 호출
        /// </summary>
        /// <param name="amount">추가할 노동량 (기본값: laborPerHit)</param>
        /// <returns>단계가 진행되었는지 여부</returns>
        public bool AddLabor(float amount = -1f)
        {
            if (building == null || building.IsCompleted)
            {
                return false; // Where is house?
            }

            if(amount <= 0)
            {
                return false; // Where is your hammer?
            }

            currentLabor += amount;

            // 필요 노동량을 채우면 다음 단계로
            if (currentLabor >= requiredLaborPerStage)
            {
                currentLabor -= requiredLaborPerStage; // 초과분 이월
                bool advanced = building.AdvanceStage();
                
                if (advanced)
                {
                    Debug.Log($"[LaborComponent] {building.name} advanced to stage {building.CurrentStageIndex}/{building.TotalStages}");
                }
                
                return advanced;
            }

            return false;
        }

        public void ResetLabor()
        {
            currentLabor = 0f;
        }

        /// <summary>
        /// 필요 노동량 설정
        /// </summary>
        public void SetRequiredLaborPerStage(float value)
        {
            requiredLaborPerStage = Mathf.Max(1f, value);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            requiredLaborPerStage = Mathf.Max(1f, requiredLaborPerStage);
            laborPerHit = Mathf.Max(0.1f, laborPerHit);
        }
#endif
    }
}
