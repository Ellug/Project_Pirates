public enum AudioBus { Master, BGM, UI }

public static class AudioParam
{
    // AudioMixer Exposed Parameter Names
    public const string MASTER_PARAM = "MasterVol";
    public const string BGM_PARAM    = "BgmVol";
    public const string UI_PARAM     = "UiVol";

    // PlayerPrefs Keys
    public const string MASTER_KEY = "audio.master";
    public const string BGM_KEY    = "audio.bgm";
    public const string UI_KEY     = "audio.ui";
}
