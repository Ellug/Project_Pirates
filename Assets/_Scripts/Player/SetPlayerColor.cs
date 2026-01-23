using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;
using System.Collections.Generic;
using UnityEngine;

public class SetPlayerColor : MonoBehaviourPunCallbacks
{
    public const string UPPER_COLOR_KEY = "UpperColor";

    // 방에서 현재 사용 중인 색 추적
    private static readonly HashSet<PlayerColorType> usedColors = new();

    // 마스터가 호출
    public static void AssignColorsToAll()
    {
        if (!PhotonNetwork.IsMasterClient) return;

        usedColors.Clear();

        foreach (var p in PhotonNetwork.PlayerList)
        {
            AssignColor(p);
        }
    }

    private static void AssignColor(Player player)
    {
        // 이미 있으면 재사용
        if (player.CustomProperties.ContainsKey(UPPER_COLOR_KEY))
        {
            var c = (PlayerColorType)(int)player.CustomProperties[UPPER_COLOR_KEY];
            usedColors.Add(c);
            return;
        }

        PlayerColorType color = GetNextColor();

        var hash = new Hashtable();
        hash[UPPER_COLOR_KEY] = (int)color;
        player.SetCustomProperties(hash);

        usedColors.Add(color);
    }

    private static PlayerColorType GetNextColor()
    {
        foreach (PlayerColorType c in System.Enum.GetValues(typeof(PlayerColorType)))
        {
            if (!usedColors.Contains(c))
                return c;
        }

        Debug.LogWarning("[SetPlayerColor] 색 부족 → Red로 fallback");
        return PlayerColorType.Red;
    }

    // 새로 들어온 애
    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        AssignColor(newPlayer);
    }
}
