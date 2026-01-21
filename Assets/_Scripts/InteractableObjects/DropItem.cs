using UnityEngine;

public class DropItem : InteractionObject
{
    [SerializeField] private ItemData _itemData;

    // 내가 아이템을 습득하면 내 인벤토리로 들어오고 사라진다.
    public override void OnInteract(PlayerInteraction player)
    {
        // 플레이어 인벤토리로 아이템 습득 로직
        // 아이템의 종류는 다양한데 이것을 어떻게 구별할까?
        // -> Scriptable Object로 아이템과 연결
        if (player.GetComponent<PlayerModel>().TryGetItem(_itemData))
        {
            gameObject.SetActive(false);
        }
        else
        {
            Debug.Log("인벤토리가 가득차서 획득할 수 없습니다.");
        } 
    }

    // 나 말고 누군가 아이템을 습득하면 사라진다.
    public override void OnOthersInteract()
    {
        gameObject.SetActive(false);
    }
}
