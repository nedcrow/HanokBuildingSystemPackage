using UnityEngine;

namespace HanokBuildingSystem
{
    /// <summary>
    /// Building의 구성 부품 (벽 조각, 기둥 등)
    /// Building보다 경량화된 컴포넌트로 시각적 표현만 담당
    /// </summary>
    public class BuildingMember : MonoBehaviour
    {
        [Header("Member Configuration")]
        [SerializeField] private string memberName;

        [Header("Stage Visuals")]
        [SerializeField] private GameObject[] stageVisuals = new GameObject[0];

        [Header("Current State")]
        [SerializeField] private int currentStage = 0;

        private Building parentBuilding;

        public string MemberName => memberName;
        public GameObject[] StageVisuals => stageVisuals;
        public int CurrentStage => currentStage;
        public Building ParentBuilding => parentBuilding;

        private void Start()
        {
            // 부모 Building 찾기
            parentBuilding = GetComponentInParent<Building>();

            if (parentBuilding != null)
            {
                // Building이 이미 특정 스테이지에 있으면 동기화
                SetStage(parentBuilding.CurrentStageIndex);
            }
            else
            {
                // 부모 Building이 없으면 초기 비주얼 설정
                UpdateVisual();
            }
        }

        /// <summary>
        /// 스테이지 설정 및 비주얼 업데이트
        /// </summary>
        public void SetStage(int stage)
        {
            currentStage = Mathf.Clamp(stage, 0, stageVisuals.Length - 1);
            UpdateVisual();
        }

        /// <summary>
        /// 현재 스테이지에 맞는 비주얼까지 활성화
        /// </summary>
        private void UpdateVisual()
        {
            if (stageVisuals == null || stageVisuals.Length == 0) return;

            for (int i = 0; i < stageVisuals.Length; i++)
            {
                if (stageVisuals[i] != null)
                {
                    stageVisuals[i].SetActive(i <= currentStage);
                }
            }
        }

        /// <summary>
        /// 모든 스테이지 비주얼 표시 (모델 하우스용)
        /// </summary>
        public void ShowAllStages()
        {
            if (stageVisuals == null || stageVisuals.Length == 0) return;

            foreach (var visual in stageVisuals)
            {
                if (visual != null)
                {
                    visual.SetActive(true);
                }
            }
        }

        /// <summary>
        /// 모든 스테이지 비주얼 숨기기
        /// </summary>
        public void HideAllStages()
        {
            if (stageVisuals == null || stageVisuals.Length == 0) return;

            foreach (var visual in stageVisuals)
            {
                if (visual != null)
                {
                    visual.SetActive(false);
                }
            }
        }

#if UNITY_EDITOR
        /// <summary>
        /// 에디터에서 직계 자식 GameObject들을 자동으로 찾아 stageVisuals 배열에 할당
        /// </summary>
        public void AutoAssignStageVisuals()
        {
            int childCount = transform.childCount;

            if (childCount == 0)
            {
                Debug.LogWarning($"[BuildingMember] {name}: No child objects found.");
                return;
            }

            stageVisuals = new GameObject[childCount];

            for (int i = 0; i < childCount; i++)
            {
                Transform child = transform.GetChild(i);
                stageVisuals[i] = child.gameObject;
                Debug.Log($"[BuildingMember] {name}: Stage {i} assigned to '{child.name}'");
            }

            if (stageVisuals == null || stageVisuals.Length == 0) return;

            for (int i = 0; i < stageVisuals.Length; i++)
            {
                if (stageVisuals[i] != null)
                {
                    stageVisuals[i].SetActive(true);
                }
            }
        }
#endif
    }
}
