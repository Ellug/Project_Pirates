#if UNITY_EDITOR
// 에디터 전용 코드
using UnityEditor;
using UnityEngine;

// ReadOnlyAttribute가 붙은 필드를 그릴 때 이 Drawer를 사용하도록 등록
[CustomPropertyDrawer(typeof(ReadOnlyAttribute))]
public sealed class ReadOnlyDrawer : PropertyDrawer
{
    // Inspector에 실제로 필드를 그리는 메서드
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        // 현재 GUI 활성화 상태를 백업
        bool prev = GUI.enabled;

        // GUI를 비활성화 → Inspector에서 수정 불가(회색 표시)
        GUI.enabled = false;

        // 기본 PropertyField 방식으로 필드를 그림
        // true : 자식 필드까지 함께 그리도록 허용 (struct, class 대응)
        EditorGUI.PropertyField(position, property, label, true);

        // GUI 활성화 상태를 원래대로 복원
        GUI.enabled = prev;
    }

    // 해당 프로퍼티가 차지할 높이를 Unity 기본 계산에 위임
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        // 자식 필드를 포함한 정확한 높이 반환
        return EditorGUI.GetPropertyHeight(property, label, true);
    }
}
#endif
