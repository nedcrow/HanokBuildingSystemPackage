using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace HanokBuildingSystem
{
    public class TutorialNotice : MonoBehaviour
    {
        [Header("Tutorial Panels")]
        private List<GameObject> tutorialPanels = new List<GameObject>();
        private int currentPanelIndex = 0;

        [Header("Stage 5 Settings")]
        [SerializeField] private TextMeshProUGUI countdownText;

        private House selectedHouse;
        private Coroutine stage5Coroutine;

        void Awake()
        {
            RectTransform panel = GetComponent<RectTransform>();
            tutorialPanels.Clear();

            for (int i = 0; i < panel.childCount; i++)
            {
                if (panel.GetChild(i).gameObject.name.Contains("Panel"))
                {
                    tutorialPanels.Add(panel.GetChild(i).gameObject);
                }
            }

            if(tutorialPanels.Count > 0) tutorialPanels[currentPanelIndex].SetActive(true);
        }

        void Start()
        {
            HanokBuildingSystem hanokBuildingSystem = HanokBuildingSystem.Instance;
            if (hanokBuildingSystem != null)
            {
                hanokBuildingSystem.Events.OnStateChanged += OnSystemStateChanged;
                hanokBuildingSystem.Events.OnConstructionStarted += OnStartConstruction;
                hanokBuildingSystem.Events.OnHouseSelected += OnHouseSelected;
                hanokBuildingSystem.Events.OnBuildingModified += OnBuildingModified;
            }
        }

        private void OnSystemStateChanged(SystemState oldState, SystemState newState)
        {
            if (newState == SystemState.NewBuilding && currentPanelIndex == 1)
            {
                UpdatePanelWithIndex();
            }

            if (newState == SystemState.Remodeling)
            {
                UpdatePanelWithIndex();
            }
        }

        private void OnStartConstruction(House house)
        {
            UpdatePanelWithIndex();
        }

        private void OnHouseSelected(House house)
        {
            selectedHouse = house;

            UpdatePanelWithIndex();
        }

        private void OnBuildingModified(House house, Building building)
        {
            UpdatePanelWithIndex();
        }

        public void OnClickedConfirmButton()
        {
            UpdatePanelWithIndex();
        }

        public void OnClickedTestButton()
        {
            UpdatePanelWithIndex();

            if (currentPanelIndex == 5 && stage5Coroutine == null)
            {
                stage5Coroutine = StartCoroutine(Stage5Coroutine());
            }
        }

        public void UpdatePanelWithIndex()
        {
            if(tutorialPanels.Count > 0)
            {
                tutorialPanels[currentPanelIndex].SetActive(false);

                currentPanelIndex ++;
                if(currentPanelIndex < tutorialPanels.Count)
                {                    
                    tutorialPanels[currentPanelIndex].SetActive(true);
                }
                else
                {
                    Debug.Log("End tutorial");
                }
            }
        }

        /// <summary>
        /// 0.1초마다 건설 카운트다운 업데이트
        /// </summary>
        private IEnumerator Stage5Coroutine()
        {
            if (countdownText == null)
            {
                Debug.LogWarning("[TutorialNotice] CountdownText is not assigned.");
                yield break;
            }

            WaitForSeconds wait = new WaitForSeconds(1f);

            while (currentPanelIndex == 5)
            {
                // 건설 카운트다운 업데이트
                UpdateConstructionCountdown();

                yield return wait;
            }

            stage5Coroutine = null;
        }

        private void UpdateConstructionCountdown()
        {
            if (selectedHouse == null || countdownText == null) return;

            // 선택된 House의 Buildings 중 TimeBased이고 진행 중인 것 찾기
            Building timeBasedBuilding = null;
            foreach (var building in selectedHouse.Buildings)
            {
                if (building != null &&
                    building.Mode == ConstructionMode.TimeBased &&
                    !building.IsCompleted)
                {
                    timeBasedBuilding = building;
                    break;
                }
            }

            if (timeBasedBuilding != null)
            {
                // 남은 시간 계산
                float stageProgress = timeBasedBuilding.StageProgress;
                float totalDuration = timeBasedBuilding.ConstructionDuration;
                float elapsedTime = stageProgress * totalDuration;
                float remainingTime = totalDuration - elapsedTime;

                int remainingSeconds = Mathf.CeilToInt(remainingTime);

                // 텍스트 업데이트
                countdownText.text = $"건설 중... {remainingSeconds-1}초 남음";

                if(remainingSeconds <= 1) UpdatePanelWithIndex();
            }
            else
            {
                // TimeBased 건설 중인 Building이 없을 때
                countdownText.text = "건설 진행 중인 건물이 없습니다.";
            }
        }
    }
}
