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

    // 마스터가 호출 (forceReassign=true면 기존 색상 무시하고 재할당)
    public static void AssignColorsToAll(bool forceReassign = false)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        usedColors.Clear();

        foreach (var p in PhotonNetwork.PlayerList)
        {
            AssignColor(p, forceReassign);
        }
    }

    private static void AssignColor(Player player, bool forceReassign)
    {
        if (player == null) return;

        // 이미 있으면 재사용
        if (!forceReassign && player.CustomProperties.ContainsKey(UPPER_COLOR_KEY))
        {
            var c = (PlayerColorType)(int)player.CustomProperties[UPPER_COLOR_KEY];
            usedColors.Add(c);
            return;
        }

        PlayerColorType color = GetNextColor();

        var hash = new Hashtable
        {
            [UPPER_COLOR_KEY] = (int)color
        };
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

        AssignColor(newPlayer, false);
    }

    // InGame 시작 시 마스터가 AssignColorsToAll 호출.
    // 인게임 중 마스터 전환 시 색상 변경 금지.
}
