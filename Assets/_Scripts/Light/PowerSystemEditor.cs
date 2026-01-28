#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(PowerSystem))]
public class PowerSystemEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        GUILayout.Space(10);
        GUILayout.Label("=== Power Test Panel ===", EditorStyles.boldLabel);

        PowerSystem ps = (PowerSystem)target;

        GUI.backgroundColor = Color.red;
        if (GUILayout.Button("⚡ POWER OFF (Blackout)", GUILayout.Height(35)))
        {
            ps.PowerOff();
        }

        GUI.backgroundColor = Color.green;
        if (GUILayout.Button("💡 POWER ON (Restore)", GUILayout.Height(35)))
        {
            ps.PowerOn();
        }

        GUI.backgroundColor = Color.white;
    }
}
#endif
