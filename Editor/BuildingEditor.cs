using UnityEditor;
using UnityEngine;

namespace HanokBuildingSystem.Editor
{
    [CustomEditor(typeof(Building))]
    public class BuildingEditor : UnityEditor.Editor
    {
        private SerializedProperty typeProp;
        private SerializedProperty sizeProp;
        private SerializedProperty statusDataProp;
        private SerializedProperty constructionStagesProp;
        private SerializedProperty currentStageIndexProp;
        private SerializedProperty constructionModeProp;
        private SerializedProperty constructionDurationProp;
        private SerializedProperty requiredLaborPerStageProp;
        private SerializedProperty buildingMembersProp;

        private void OnEnable()
        {
            typeProp = serializedObject.FindProperty("type");
            sizeProp = serializedObject.FindProperty("size");
            statusDataProp = serializedObject.FindProperty("statusData");
            constructionStagesProp = serializedObject.FindProperty("constructionStages");
            currentStageIndexProp = serializedObject.FindProperty("currentStageIndex");
            constructionModeProp = serializedObject.FindProperty("constructionMode");
            constructionDurationProp = serializedObject.FindProperty("constructionDuration");
            requiredLaborPerStageProp = serializedObject.FindProperty("requiredLaborPerStage");
            buildingMembersProp = serializedObject.FindProperty("buildingMembers");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // Building Configuration
            EditorGUILayout.LabelField("Building Configuration", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(typeProp);
            EditorGUILayout.PropertyField(sizeProp);
            EditorGUILayout.PropertyField(statusDataProp);

            EditorGUILayout.Space();

            // Construction
            EditorGUILayout.LabelField("Construction", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(constructionStagesProp, true);
            EditorGUILayout.PropertyField(currentStageIndexProp);

            EditorGUILayout.Space();

            // Construction Progress
            EditorGUILayout.LabelField("Construction Progress", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(constructionModeProp);

            ConstructionMode mode = (ConstructionMode)constructionModeProp.enumValueIndex;

            // Time-Based Settings (항상 표시, TimeBased가 아니면 비활성화)
            EditorGUILayout.Space();
            
            bool isTimeBased = mode == ConstructionMode.TimeBased;
            EditorGUI.BeginDisabledGroup(!isTimeBased);
            EditorGUILayout.PropertyField(constructionDurationProp);
            EditorGUI.EndDisabledGroup();

            // Labor-Based Settings (LaborBased가 아니면 비활성화)
            EditorGUILayout.Space();
            
            bool isLaborBased = mode == ConstructionMode.LaborBased;
            EditorGUI.BeginDisabledGroup(!isLaborBased);
            EditorGUILayout.PropertyField(requiredLaborPerStageProp);
            EditorGUI.EndDisabledGroup();

            if (!isLaborBased)
            {
                EditorGUILayout.HelpBox("LaborBased 모드로 변경하면 이 설정을 수정할 수 있습니다.", MessageType.Info);
            }

            EditorGUILayout.Space();

            // Building Members
            EditorGUILayout.LabelField("Building Members", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(buildingMembersProp, true);

            serializedObject.ApplyModifiedProperties();
        }
    }
}
