using System.Text;
using Photon.Realtime;
using TMPro;
using UnityEngine;

public class RoomPlayerListView : MonoBehaviour
{
    private const string PROP_READY = "ready";

    [Header("UI")]
    [SerializeField] private TMP_Text _text;

    [Header("Style")]
    [SerializeField] private string _meColor = "#FF3B30";

    private readonly StringBuilder _sb = new(256);

    public bool HasText => _text != null;

    public void SetNotInRoom()
    {
        if (_text == null) return;
        _text.text = "(Not in room)";
    }

    // test render
    public void Render(string roomName, Player[] players, int count, Player localPlayer)
    {
        if (_text == null) return;

        _sb.Clear();
        _sb.AppendLine($"Room: {roomName}");
        _sb.AppendLine($"Players: {count}");
        _sb.AppendLine();

        for (int i = 0; i < count; i++)
        {
            var p = players[i];
            bool isMe = p == localPlayer;
            bool ready = IsReady(p);

            string name = string.IsNullOrWhiteSpace(p.NickName) ? $"Player {p.ActorNumber}" : p.NickName;
            if (isMe)
                name = $"<color={_meColor}>{name} (ME)</color>";

            _sb.Append('[').Append(p.ActorNumber).Append("] ")
               .Append(name).Append("  ")
               .AppendLine(ready ? "READY" : "-----");
        }

        _text.text = _sb.ToString();
    }

    private static bool IsReady(Player p)
    {
        if (p == null || p.CustomProperties == null) return false;
        if (p.CustomProperties.TryGetValue(PROP_READY, out object v) && v is bool b) return b;
        return false;
    }
}
