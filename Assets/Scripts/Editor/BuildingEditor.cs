using UnityEditor;
using UnityEngine;

namespace HanokBuildingSystem.Editor
{
    [CustomEditor(typeof(Building), true)]          // ← 자식 클래스에도 적용
    [CanEditMultipleObjects]
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
            typeProp                    = serializedObject.FindProperty("type");
            sizeProp                    = serializedObject.FindProperty("size");
            statusDataProp              = serializedObject.FindProperty("statusData");
            constructionStagesProp      = serializedObject.FindProperty("constructionStages");
            currentStageIndexProp       = serializedObject.FindProperty("currentStageIndex");
            constructionModeProp        = serializedObject.FindProperty("constructionMode");
            constructionDurationProp    = serializedObject.FindProperty("constructionDuration");
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
            EditorGUILayout.PropertyField(typeProp);
            EditorGUILayout.PropertyField(sizeProp);
            EditorGUILayout.PropertyField(statusDataProp);

            EditorGUILayout.Space();

            // ▶ Construction
            EditorGUILayout.PropertyField(constructionStagesProp, true);
            EditorGUILayout.PropertyField(currentStageIndexProp);

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
                "size",
                "statusData",
                "constructionStages",
                "currentStageIndex",
                "constructionMode",
                "constructionDuration",
                "requiredLaborPerStage",
                "buildingMembers"
            );

            serializedObject.ApplyModifiedProperties();
        }
    }
}
