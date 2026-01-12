#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using Nedcrow.HanokBuildingSystem.Runtime;

namespace HanokBuildingSystem.Editor
{
    [CustomEditor(typeof(BuildingMember))]
    [CanEditMultipleObjects]
    public class BuildingMemberEditor : UnityEditor.Editor
    {
        private SerializedProperty memberNameProp;
        private SerializedProperty stageVisualsProp;
        private SerializedProperty currentStageProp;

        private void OnEnable()
        {
            memberNameProp = serializedObject.FindProperty("memberName");
            stageVisualsProp = serializedObject.FindProperty("stageVisuals");
            currentStageProp = serializedObject.FindProperty("currentStage");
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

            // Member Configuration
            EditorGUILayout.LabelField("Member Configuration", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(memberNameProp);

            EditorGUILayout.Space();

            // Stage Visuals
            EditorGUILayout.LabelField("Stage Visuals", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(stageVisualsProp, true);

            BuildingMember member = target as BuildingMember;
            if (member != null)
            {
                // Auto Assign 버튼
                if (GUILayout.Button("Auto Assign Stage Visuals from Children", GUILayout.Height(25)))
                {
                    member.AutoAssignStageVisuals();
                    serializedObject.Update();
                    EditorUtility.SetDirty(member);
                }

                EditorGUILayout.Space();

                // 현재 할당된 스테이지 비주얼 표시
                if (stageVisualsProp.arraySize > 0)
                {
                    EditorGUILayout.LabelField("Assigned Stage Visuals:", EditorStyles.miniBoldLabel);
                    for (int i = 0; i < stageVisualsProp.arraySize; i++)
                    {
                        var element = stageVisualsProp.GetArrayElementAtIndex(i);
                        bool isAssigned = element.objectReferenceValue != null;

                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField($"  Stage {i}:", GUILayout.Width(60));
                        if (isAssigned)
                        {
                            EditorGUILayout.LabelField(element.objectReferenceValue.name, GUILayout.Width(150));
                            EditorGUILayout.LabelField("✓ Assigned", EditorStyles.boldLabel);
                        }
                        else
                        {
                            EditorGUILayout.LabelField("<None>", GUILayout.Width(150));
                            EditorGUILayout.LabelField("✗ Not Assigned", EditorStyles.label);
                        }
                        EditorGUILayout.EndHorizontal();
                    }
                }
            }

            EditorGUILayout.Space();

            // Current State
            EditorGUILayout.LabelField("Current State", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(currentStageProp);

            serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif