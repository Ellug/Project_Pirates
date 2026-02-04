#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

[CustomEditor(typeof(MiniMapBakeConfig))]
public class MiniMapBakeConfigEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        var config = (MiniMapBakeConfig)target;
        GUILayout.Space(8f);

        using (new EditorGUI.DisabledScope(!MiniMapBaker.CanBake(config)))
        {
            if (GUILayout.Button("Bake MiniMap"))
                MiniMapBaker.Bake(config);
        }

        using (new EditorGUI.DisabledScope(config == null || config.contentRoot == null && config.mapRect == null))
        {
            if (GUILayout.Button("Clear Generated"))
                MiniMapBaker.ClearGenerated(config);
        }
    }
}

public static class MiniMapBaker
{
    private struct Segment
    {
        public bool horizontal;
        public float linePos;
        public float start;
        public float end;
    }

    private static Sprite _defaultSprite;

    public static bool CanBake(MiniMapBakeConfig config)
    {
        if (config == null)
            return false;
        if (config.mapRect == null)
            return false;
        if (config.boundsCollider == null)
            return false;
        return true;
    }

    public static void Bake(MiniMapBakeConfig config)
    {
        if (!CanBake(config))
            return;

        RectTransform contentRoot = config.contentRoot != null ? config.contentRoot : config.mapRect;
        if (contentRoot == null)
            return;

        Bounds bounds = config.boundsCollider.bounds;
        Rect rect = config.mapRect.rect;
        float mapWidth = Mathf.Max(1f, rect.width - (config.padding * 2f));
        float mapHeight = Mathf.Max(1f, rect.height - (config.padding * 2f));
        float worldWidth = Mathf.Max(0.001f, bounds.size.x);
        float worldHeight = Mathf.Max(0.001f, bounds.size.z);
        float scale = Mathf.Min(mapWidth / worldWidth, mapHeight / worldHeight);
        Vector3 worldCenter = bounds.center;

        Transform generatedRoot = GetOrCreateGeneratedRoot(contentRoot, config.generatedRootName, config.clearExisting);

        if (config.roomRoot != null)
            BuildRooms(config, generatedRoot, worldCenter, scale);

        if (config.wallRoot != null)
            BuildWalls(config, generatedRoot, worldCenter, scale);
    }

    public static void ClearGenerated(MiniMapBakeConfig config)
    {
        if (config == null)
            return;

        RectTransform contentRoot = config.contentRoot != null ? config.contentRoot : config.mapRect;
        if (contentRoot == null)
            return;

        var existing = contentRoot.Find(config.generatedRootName);
        if (existing != null)
            Undo.DestroyObjectImmediate(existing.gameObject);
    }

    private static Transform GetOrCreateGeneratedRoot(RectTransform parent, string name, bool clearExisting)
    {
        Transform existing = parent.Find(name);
        if (existing != null && clearExisting)
            Undo.DestroyObjectImmediate(existing.gameObject);

        if (existing != null && !clearExisting)
            return existing;

        var go = new GameObject(name, typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(go, "Create MiniMap Root");

        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.localRotation = Quaternion.identity;
        rt.localScale = Vector3.one;
        rt.sizeDelta = Vector2.zero;

        return rt;
    }

    private static void BuildRooms(MiniMapBakeConfig config, Transform parent, Vector3 worldCenter, float scale)
    {
        foreach (Transform room in config.roomRoot)
        {
            Transform target = room;
            if (config.useRoomFloorIfFound)
            {
                var floor = room.Find("RoomFloor");
                if (floor != null)
                    target = floor;
            }

            if (!TryGetBounds(target, out var bounds))
                continue;

            Vector2 size = new Vector2(bounds.size.x * scale, bounds.size.z * scale);
            Vector2 pos = WorldToMap(bounds.center, worldCenter, scale);
            CreateImage(parent, "Room", size, pos, config.roomColor, config.roomSprite);
        }
    }

    private static void BuildWalls(MiniMapBakeConfig config, Transform parent, Vector3 worldCenter, float scale)
    {
        var segments = new List<Segment>();
        var renderers = config.wallRoot.GetComponentsInChildren<Renderer>(true);
        foreach (var renderer in renderers)
        {
            if (!ShouldIncludeRenderer(renderer))
                continue;

            Bounds b = renderer.bounds;
            float sizeX = b.size.x;
            float sizeZ = b.size.z;
            bool horizontal = sizeX >= sizeZ;

            float start = horizontal ? b.min.x : b.min.z;
            float end = horizontal ? b.max.x : b.max.z;
            float length = Mathf.Abs(end - start);
            if (length < config.minSegmentLength)
                continue;

            segments.Add(new Segment
            {
                horizontal = horizontal,
                linePos = horizontal ? b.center.z : b.center.x,
                start = Mathf.Min(start, end),
                end = Mathf.Max(start, end)
            });
        }

        var merged = MergeSegments(segments, config.axisTolerance, config.gapTolerance, config.minSegmentLength);
        foreach (var seg in merged)
        {
            float length = Mathf.Abs(seg.end - seg.start);
            if (length < config.minSegmentLength)
                continue;

            Vector3 worldPos;
            Vector2 size;

            if (seg.horizontal)
            {
                worldPos = new Vector3((seg.start + seg.end) * 0.5f, worldCenter.y, seg.linePos);
                size = new Vector2(length * scale, config.wallThickness);
            }
            else
            {
                worldPos = new Vector3(seg.linePos, worldCenter.y, (seg.start + seg.end) * 0.5f);
                size = new Vector2(config.wallThickness, length * scale);
            }

            Vector2 pos = WorldToMap(worldPos, worldCenter, scale);
            CreateImage(parent, "Wall", size, pos, config.wallColor, config.wallSprite);
        }
    }

    private static bool TryGetBounds(Transform root, out Bounds bounds)
    {
        bounds = default;
        bool hasBounds = false;

        var renderers = root.GetComponentsInChildren<Renderer>(true);
        foreach (var renderer in renderers)
        {
            if (!ShouldIncludeRenderer(renderer))
                continue;

            if (!hasBounds)
            {
                bounds = renderer.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(renderer.bounds);
            }
        }

        return hasBounds;
    }

    private static bool ShouldIncludeRenderer(Renderer renderer)
    {
        if (renderer == null)
            return false;
        if (renderer is ParticleSystemRenderer)
            return false;
        return renderer.enabled;
    }

    private static List<Segment> MergeSegments(List<Segment> segments, float axisTolerance, float gapTolerance, float minLength)
    {
        var result = new List<Segment>();
        MergeByOrientation(segments, true, axisTolerance, gapTolerance, minLength, result);
        MergeByOrientation(segments, false, axisTolerance, gapTolerance, minLength, result);
        return result;
    }

    private static void MergeByOrientation(List<Segment> segments, bool horizontal, float axisTolerance, float gapTolerance, float minLength, List<Segment> output)
    {
        var groups = new List<List<Segment>>();
        foreach (var seg in segments)
        {
            if (seg.horizontal != horizontal)
                continue;

            List<Segment> group = null;
            foreach (var g in groups)
            {
                if (Mathf.Abs(g[0].linePos - seg.linePos) <= axisTolerance)
                {
                    group = g;
                    break;
                }
            }

            if (group == null)
            {
                group = new List<Segment>();
                groups.Add(group);
            }

            group.Add(seg);
        }

        foreach (var group in groups)
        {
            if (group.Count == 0)
                continue;

            float linePosSum = 0f;
            foreach (var seg in group)
                linePosSum += seg.linePos;
            float linePos = linePosSum / group.Count;

            group.Sort((a, b) => a.start.CompareTo(b.start));
            Segment current = group[0];
            current.linePos = linePos;

            for (int i = 1; i < group.Count; i++)
            {
                Segment next = group[i];
                if (next.start <= current.end + gapTolerance)
                {
                    current.end = Mathf.Max(current.end, next.end);
                }
                else
                {
                    if (Mathf.Abs(current.end - current.start) >= minLength)
                        output.Add(current);
                    current = next;
                    current.linePos = linePos;
                }
            }

            if (Mathf.Abs(current.end - current.start) >= minLength)
                output.Add(current);
        }
    }

    private static Vector2 WorldToMap(Vector3 worldPos, Vector3 worldCenter, float scale)
    {
        return new Vector2(
            (worldPos.x - worldCenter.x) * scale,
            (worldPos.z - worldCenter.z) * scale
        );
    }

    private static Image CreateImage(Transform parent, string name, Vector2 size, Vector2 anchoredPosition, Color color, Sprite sprite)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        Undo.RegisterCreatedObjectUndo(go, "Create MiniMap Element");
        go.transform.SetParent(parent, false);

        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;

        var image = go.GetComponent<Image>();
        image.color = color;
        image.sprite = GetSprite(sprite);
        image.raycastTarget = false;

        return image;
    }

    private static Sprite GetSprite(Sprite sprite)
    {
        if (sprite != null)
            return sprite;

        if (_defaultSprite == null)
            _defaultSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");

        return _defaultSprite;
    }
}
#endif
