using UnityEngine;

// 아이템의 종류를 구분하기 위한 열거형
public enum ItemType
{
    Mission,
    Consumable,
    Weapon
}

[CreateAssetMenu(fileName = "New Item", menuName = "Scriptable Objects/Item Data")]
public class ItemData : ScriptableObject
{
    [Header("Item Info")]
    public int itemId;
    public string itemName;
    public Sprite icon;
    public ItemType type;
}
