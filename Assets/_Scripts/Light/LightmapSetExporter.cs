#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class LightmapSetExporter
{
    [MenuItem("Tools/Lighting/Export Current Lightmap Set")]
    public static void Export()
    {
        var set = ScriptableObject.CreateInstance<LightmapSet>();

        set.lightmaps = LightmapSettings.lightmaps;
        set.lightProbes = LightmapSettings.lightProbes;

        string path = EditorUtility.SaveFilePanelInProject(
            "Save Lightmap Set",
            "LightmapSet",
            "asset",
            "Save baked lightmap set"
        );

        if (string.IsNullOrEmpty(path)) return;

        AssetDatabase.CreateAsset(set, path);
        AssetDatabase.SaveAssets();

        Debug.Log("[LightmapSetExporter] Exported : " + path);
    }
}
#endif
