using UnityEditor;
using UnityEngine;

namespace HanokBuildingSystem.Editor
{
    [CustomEditor(typeof(Building), true)]          // ← 자식 클래스에도 적용
    [CanEditMultipleObjects]
    public class BuildingEditor : UnityEditor.Editor
    {
        private SerializedProperty statusDataProp;
        private SerializedProperty stageVisualsProp;
        private SerializedProperty currentStageIndexProp;
        private SerializedProperty constructionModeProp;
        private SerializedProperty constructionDurationProp;
        private SerializedProperty currentConstructionDuration;
        private SerializedProperty requiredLaborPerStageProp;
        private SerializedProperty buildingMembersProp;

        private void OnEnable()
        {
            statusDataProp              = serializedObject.FindProperty("statusData");
            stageVisualsProp            = serializedObject.FindProperty("stageVisuals");
            currentStageIndexProp       = serializedObject.FindProperty("currentStageIndex");
            constructionModeProp        = serializedObject.FindProperty("constructionMode");
            constructionDurationProp    = serializedObject.FindProperty("constructionDuration");
            currentConstructionDuration = serializedObject.FindProperty("currentConstructionDuration");
            requiredLaborPerStageProp   = serializedObject.FindProperty("requiredLaborPerStage");
            buildingMembersProp         = serializedObject.FindProperty("buildingMembers");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // m_Script는 항상 비활성 상태로 맨 위에
            using (new EditorGUI.DisabledScope(true))
            {
                var scriptProp = serializedObject.FindProperty("m_Script");
                if (scriptProp != null)
                    EditorGUILayout.PropertyField(scriptProp);
            }

            EditorGUILayout.Space();

            // ▶ Building Configuration
            EditorGUILayout.PropertyField(statusDataProp);

            // StatusData 변경 시 stageVisuals 배열 크기 조정
            ValidateStageVisualsArraySize();

            // Stage Visuals
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Stage Visuals", EditorStyles.boldLabel);

            Building building = target as Building;
            if (building != null && building.StatusData != null)
            {
                int stageCount = building.StatusData.ConstructionStages.Count;
                EditorGUILayout.HelpBox($"Total Stages: {stageCount} (based on BuildingStatusData)", MessageType.Info);

                if (stageVisualsProp.arraySize != stageCount)
                {
                    EditorGUILayout.HelpBox($"Array size mismatch! Expected {stageCount}, but found {stageVisualsProp.arraySize}.", MessageType.Warning);
                }

                // Auto Assign 버튼
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Auto Assign Stage Visuals", GUILayout.Height(25)))
                {
                    building.AutoAssignStageVisuals4Editor();
                    serializedObject.Update();
                    EditorUtility.SetDirty(building);
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.Space();

                EditorGUILayout.PropertyField(stageVisualsProp, true);

                // 각 스테이지의 이름 표시
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Expected Stage Names (from StatusData):", EditorStyles.miniBoldLabel);
                for (int i = 0; i < stageCount; i++)
                {
                    string stageName = building.StatusData.ConstructionStages[i].StageName;
                    bool isAssigned = i < stageVisualsProp.arraySize && stageVisualsProp.GetArrayElementAtIndex(i).objectReferenceValue != null;

                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField($"  Stage {i}:", GUILayout.Width(60));
                    EditorGUILayout.LabelField(string.IsNullOrEmpty(stageName) ? "<No Name>" : stageName, GUILayout.Width(150));
                    EditorGUILayout.LabelField(isAssigned ? "✓ Assigned" : "✗ Not Assigned", isAssigned ? EditorStyles.boldLabel : EditorStyles.label);
                    EditorGUILayout.EndHorizontal();
                }
            }
            else
            {
                EditorGUILayout.HelpBox("Assign BuildingStatusData to configure stage visuals.", MessageType.Warning);
            }

            EditorGUILayout.Space();

            // ▶ Construction
            EditorGUILayout.PropertyField(currentStageIndexProp);

            // 현재 단계의 자원 정보 표시
            DrawCurrentStageResourceInfo();

            EditorGUILayout.Space();

            // ▶ Construction Mode
            EditorGUILayout.PropertyField(constructionModeProp);
            var mode = (ConstructionMode)constructionModeProp.enumValueIndex;

            // ▶ Time-Based Settings
            EditorGUILayout.Space();
            bool isTimeBased = mode == ConstructionMode.TimeBased;
            using (new EditorGUI.DisabledScope(!isTimeBased))
            {
                EditorGUILayout.PropertyField(constructionDurationProp);
                EditorGUILayout.PropertyField(currentConstructionDuration);
            }

            // ▶ Labor-Based Settings
            EditorGUILayout.Space();
            bool isLaborBased = mode == ConstructionMode.LaborBased;
            using (new EditorGUI.DisabledScope(!isLaborBased))
            {
                EditorGUILayout.PropertyField(requiredLaborPerStageProp);
            }

            if (!isLaborBased)
            {
                EditorGUILayout.HelpBox(
                    "LaborBased 모드로 변경하면 이 설정을 수정할 수 있습니다.",
                    MessageType.Info
                );
            }

            EditorGUILayout.Space();

            // ▶ Building Members
            EditorGUILayout.PropertyField(buildingMembersProp, true);

            EditorGUILayout.Space();

            // 🔻 여기서부터는 "자식 클래스(WallBuilding 등)만 가진 추가 필드" 자동 출력
            //    Building에서 이미 처리한 필드는 제외
            DrawPropertiesExcluding(
                serializedObject,
                "m_Script",
                "type",
                "statusData",
                "stageVisuals",
                "constructionStages",
                "currentStageIndex",
                "pendingResources",
                "collectedResourcesForCurrentStage",
                "constructionMode",
                "constructionDuration",
                "currentConstructionDuration",
                "requiredLaborPerStage",
                "buildingMembers"
            );

            serializedObject.ApplyModifiedProperties();
        }

        private void ValidateStageVisualsArraySize()
        {
            Building building = target as Building;
            if (building == null || building.StatusData == null) return;

            int expectedSize = building.StatusData.ConstructionStages.Count;
            if (stageVisualsProp.arraySize != expectedSize)
            {
                stageVisualsProp.arraySize = expectedSize;
                serializedObject.ApplyModifiedProperties();
            }
        }

        private void DrawCurrentStageResourceInfo()
        {
            Building building = target as Building;
            if (building == null || building.StatusData == null) return;

            // 완성된 경우
            if (building.IsCompleted)
            {
                EditorGUILayout.HelpBox("건설 완료", MessageType.Info);
                return;
            }

            // 현재 단계의 필요 자원
            Cost[] requiredResources = building.GetCurrentStageRequiredResources();
            if (requiredResources == null || requiredResources.Length == 0)
            {
                EditorGUILayout.HelpBox($"Stage {building.CurrentStageIndex}: 필요 자원 없음", MessageType.Info);
                return;
            }

            // 자원 정보 박스
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField($"Stage {building.CurrentStageIndex} 자원 현황", EditorStyles.boldLabel);

            foreach (var required in requiredResources)
            {
                if (required.ResourceType == null) continue;

                int collected = building.GetCollectedAmount(required.ResourceType);
                int needed = required.Amount;
                bool isSatisfied = collected >= needed;

                // 색상 설정
                Color originalColor = GUI.color;
                GUI.color = isSatisfied ? Color.green : Color.yellow;

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(required.ResourceType.name, GUILayout.Width(150));
                EditorGUILayout.LabelField($"{collected} / {needed}", GUILayout.Width(100));

                // 진행도 바
                Rect progressRect = GUILayoutUtility.GetRect(100, 18);
                float progress = needed > 0 ? Mathf.Clamp01((float)collected / needed) : 1f;
                EditorGUI.ProgressBar(progressRect, progress, $"{progress:P0}");

                EditorGUILayout.EndHorizontal();

                GUI.color = originalColor;
            }

            // 전체 충족 여부
            bool allSatisfied = building.AreAllResourcesCollected();
            if (allSatisfied)
            {
                EditorGUILayout.HelpBox("✓ 모든 자원 충족됨", MessageType.Info);
            }

            EditorGUILayout.EndVertical();
        }
    }
}
