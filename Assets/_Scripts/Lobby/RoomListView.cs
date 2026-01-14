using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class RoomListView : MonoBehaviour
{
    [SerializeField] private Transform _content;
    [SerializeField] private RoomUnitView _itemPrefab;
    [SerializeField] private TMP_Text _emptyText;

    public event Action<RoomSnapshot> JoinClicked;

    public void Render(IReadOnlyList<RoomSnapshot> rooms)
    {
        Clear();

        bool isEmpty = rooms == null || rooms.Count == 0;

        if (_emptyText != null)
            _emptyText.gameObject.SetActive(isEmpty);

        if (isEmpty) return;

        for (int i = 0; i < rooms.Count; i++)
        {
            var view = Instantiate(_itemPrefab, _content);
            view.Bind(rooms[i], HandleJoinClicked);
        }
    }

    // Join 클릭 Invoke
    private void HandleJoinClicked(RoomSnapshot snap)
    {
        JoinClicked?.Invoke(snap);
    }

    private void Clear()
    {
        if (_content == null)
            return;

        for (int i = _content.childCount - 1; i >= 0; i--)
            Destroy(_content.GetChild(i).gameObject);
    }
}
