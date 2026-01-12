#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using Nedcrow.HanokBuildingSystem.Runtime;

namespace HanokBuildingSystem
{
    /// <summary>
    /// ReadOnlyAttribute를 위한 커스텀 PropertyDrawer
    /// Inspector에서 필드를 비활성화하여 읽기 전용으로 표시
    /// </summary>
    [CustomPropertyDrawer(typeof(ReadOnlyAttribute))]
    public class ReadOnlyDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
            => EditorGUI.GetPropertyHeight(property, label, true);

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            bool prev = GUI.enabled;
            GUI.enabled = false; // 입력 비활성화

            EditorGUI.PropertyField(position, property, label, true);

            GUI.enabled = prev;  // 원복
        }
    }
}
#endif
