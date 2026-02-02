using UnityEngine;

[CreateAssetMenu(fileName = "New Item", menuName = "Scriptable Objects/Item Data")]
public class ItemData : ScriptableObject
{
    [Header("Item Info")]
    public int itemId;
    public string itemName;
    public Sprite icon;
    public string itemInformation;
}
