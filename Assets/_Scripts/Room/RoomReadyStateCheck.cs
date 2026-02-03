using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;

public class RoomReadyStateCheck
{
    private const string READY = "ready";
    private Hashtable _table = new Hashtable {};

    public void SetLocalReady(bool ready)
    {
        if (!PhotonNetwork.InRoom || PhotonNetwork.LocalPlayer == null)
            return;

        var p = PhotonNetwork.LocalPlayer;

        if (p.CustomProperties != null && p.CustomProperties.TryGetValue(READY, out object v) && v is bool b && b == ready)
            return;

        _table[READY] = ready;
        p.SetCustomProperties(_table);
    }

    public void ToggleLocalReady()
    {
        if (!PhotonNetwork.InRoom || PhotonNetwork.LocalPlayer == null)
            return;

        bool cur = IsReady(PhotonNetwork.LocalPlayer);
        SetLocalReady(!cur);
    }

    public bool IsReady(Player p)
    {
        if (p == null || p.CustomProperties == null) return false;
        if (p.CustomProperties.TryGetValue(READY, out object v) && v is bool b) return b;
        return false;
    }

    public bool AreAllPlayersReady(Player[] players, int count)
    {
        if (players == null || count <= 0) return false;

        for (int i = 0; i < count; i++)
            if (!IsReady(players[i])) return false;

        return true;
    }

    public bool IsReadyChanged(Hashtable changedProps)
    {
        return changedProps != null && changedProps.ContainsKey(READY);
    }
}
