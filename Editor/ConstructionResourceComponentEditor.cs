using UnityEditor;
using UnityEngine;

namespace HanokBuildingSystem.Editor
{
    [CustomEditor(typeof(ConstructionResourceComponent), true)]
    [CanEditMultipleObjects]
    public class ConstructionResourceComponentEditor : UnityEditor.Editor
    {
        private SerializedProperty stageVisualsProp;
        private SerializedProperty currentStageIndexProp;
        private SerializedProperty pendingResourcesProp;
        private SerializedProperty collectedResourcesProp;

        private void OnEnable()
        {
            stageVisualsProp = serializedObject.FindProperty("stageVisuals");
            currentStageIndexProp = serializedObject.FindProperty("currentStageIndex");
            pendingResourcesProp = serializedObject.FindProperty("pendingResources");
            collectedResourcesProp = serializedObject.FindProperty("collectedResourcesForCurrentStage");
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

            ConstructionResourceComponent component = target as ConstructionResourceComponent;
            Building building = component?.GetComponent<Building>();

            if (building == null || building.StatusData == null)
            {
                EditorGUILayout.HelpBox("Building 또는 BuildingStatusData가 없습니다.", MessageType.Warning);
                serializedObject.ApplyModifiedProperties();
                return;
            }

            // ===== Stage Visuals =====
            EditorGUILayout.LabelField("Stage Management", EditorStyles.boldLabel);

            int stageCount = building.StatusData.ConstructionStages.Count;
            EditorGUILayout.HelpBox($"Total Stages: {stageCount} (from BuildingStatusData)", MessageType.Info);

            if (stageVisualsProp.arraySize != stageCount)
            {
                EditorGUILayout.HelpBox($"Array size mismatch! Expected {stageCount}, but found {stageVisualsProp.arraySize}.", MessageType.Warning);
            }

            // Auto Assign 버튼
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Auto Assign Stage Visuals", GUILayout.Height(25)))
            {
                component.AutoAssignStageVisuals();
                serializedObject.Update();
                EditorUtility.SetDirty(component);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(stageVisualsProp, true);

            // 각 스테이지의 이름 표시
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Expected Stage Names:", EditorStyles.miniBoldLabel);
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

            EditorGUILayout.Space();

            // ===== Current Stage =====
            EditorGUILayout.PropertyField(currentStageIndexProp);

            // 현재 단계의 자원 정보 표시
            DrawCurrentStageResourceInfo(component);

            EditorGUILayout.Space();

            // ===== Resources =====
            EditorGUILayout.LabelField("Resource Management", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(pendingResourcesProp, true);
            EditorGUILayout.PropertyField(collectedResourcesProp, true);

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawCurrentStageResourceInfo(ConstructionResourceComponent component)
        {
            if (component == null) return;

            Building building = component.GetComponent<Building>();
            if (building == null || building.StatusData == null) return;

            // 완성된 경우
            if (component.IsCompleted)
            {
                EditorGUILayout.HelpBox("건설 완료", MessageType.Info);
                return;
            }

            // 현재 단계의 필요 자원
            Cost[] requiredResources = building.GetCurrentStageRequiredResources();
            if (requiredResources == null || requiredResources.Length == 0)
            {
                EditorGUILayout.HelpBox($"Stage {component.CurrentStageIndex}: 필요 자원 없음", MessageType.Info);
                return;
            }

            // 자원 정보 박스
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField($"Stage {component.CurrentStageIndex} 자원 현황", EditorStyles.boldLabel);

            foreach (var required in requiredResources)
            {
                if (required.ResourceType == null) continue;

                int collected = component.GetCollectedAmount(required.ResourceType);
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
            bool allSatisfied = component.AreAllResourcesCollected();
            if (allSatisfied)
            {
                EditorGUILayout.HelpBox("✓ 모든 자원 충족됨", MessageType.Info);
            }

            EditorGUILayout.EndVertical();

            // 대기 자원 정보
            DrawPendingResourcesInfo(component);
        }

        private void DrawPendingResourcesInfo(ConstructionResourceComponent component)
        {
            if (component == null) return;

            var pendingResources = component.PendingResources;
            if (pendingResources == null || pendingResources.Count == 0)
            {
                return; // 대기 자원이 없으면 표시하지 않음
            }

            EditorGUILayout.Space();

            // 대기 자원 박스
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("대기 자원 (할당되었으나 아직 사용되지 않음)", EditorStyles.boldLabel);

            Color originalColor = GUI.color;
            GUI.color = new Color(0.8f, 0.8f, 1f); // 연한 파란색

            foreach (var pendingCost in pendingResources)
            {
                if (pendingCost.ResourceType == null) continue;

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(pendingCost.ResourceType.name, GUILayout.Width(150));
                EditorGUILayout.LabelField($"{pendingCost.Amount}", GUILayout.Width(100));
                EditorGUILayout.EndHorizontal();
            }

            GUI.color = originalColor;

            EditorGUILayout.EndVertical();
        }
    }
}
