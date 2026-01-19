[System.Serializable]
public class VoiceUserSetting
{
    public int ViewID;
    public string Nickname;
    public float Volume;
    public bool IsMuted;

    public VoiceUserSetting(int id, string name, float vol, bool mute)
    {
        ViewID = id;
        Nickname = name;
        Volume = vol;
        IsMuted = mute;
    }
}

public enum VoiceBus { MyMic, MasterInput }

public static class VoiceParam
{
    public const string MyMicVolumeKey = "voice.myMicVol";
    public const string MyMicTypeKey = "voice.myMicType";
    public const string MyMicMuteKey = "voice.myMicMute";

    public const string MasterInputKey = "voice.masterInput";
    public const string MasterInputMuteKey = "voice.masterInputMute";

    public static string GetRemotePlayerKey(int actorNumber) => $"voice.remote.{actorNumber}";
    public static string GetRemoteMuteKey(int actorNumber) => $"voice.mute.{actorNumber}";
}