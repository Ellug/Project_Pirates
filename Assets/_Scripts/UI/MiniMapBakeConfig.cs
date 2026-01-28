using UnityEngine;

[DisallowMultipleComponent]
public class MiniMapBakeConfig : MonoBehaviour
{
    [Header("Targets")]
    public RectTransform mapRect;
    public RectTransform contentRoot;
    public BoxCollider boundsCollider;
    public Transform wallRoot;
    public Transform roomRoot;

    [Header("Mapping")]
    public float padding = 8f;

    [Header("Output")]
    public bool clearExisting = true;
    public string generatedRootName = "MiniMapGenerated";

    [Header("Walls")]
    public float wallThickness = 2f;
    public float axisTolerance = 0.1f;
    public float gapTolerance = 0.2f;
    public float minSegmentLength = 0.05f;
    public Color wallColor = new(1f, 1f, 1f, 1f);
    public Sprite wallSprite;

    [Header("Rooms")]
    public bool useRoomFloorIfFound = true;
    public Color roomColor = new(1f, 1f, 1f, 0.15f);
    public Sprite roomSprite;
}
