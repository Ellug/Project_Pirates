using UnityEngine;

public sealed class DisplayManager : Singleton<DisplayManager>
{
    private const int DEFAULT_W = 1920;
    private const int DEFAULT_H = 1080;
    private const FullScreenMode DEFAULT_MODE = FullScreenMode.FullScreenWindow;

    private void Start()
    {
        LoadAndApplySavedDisplay();
    }

    public void LoadAndApplySavedDisplay()
    {
        int w = PlayerPrefs.GetInt(DisplayParam.WIDTH_KEY,  DEFAULT_W);
        int h = PlayerPrefs.GetInt(DisplayParam.HEIGHT_KEY, DEFAULT_H);

        var modeInt = PlayerPrefs.GetInt(DisplayParam.MODE_KEY, (int)DEFAULT_MODE);
        var mode = (FullScreenMode)Mathf.Clamp(modeInt, 0, (int)FullScreenMode.Windowed);

        Apply(w, h, mode);
    }

    public void Apply(int width, int height, FullScreenMode mode)
    {
        width  = Mathf.Max(640, width);
        height = Mathf.Max(360, height);

        Screen.fullScreenMode = mode;
        Screen.SetResolution(width, height, mode);
    }

    public void Save(int width, int height, FullScreenMode mode)
    {
        PlayerPrefs.SetInt(DisplayParam.WIDTH_KEY, width);
        PlayerPrefs.SetInt(DisplayParam.HEIGHT_KEY, height);
        PlayerPrefs.SetInt(DisplayParam.MODE_KEY, (int)mode);
    }
}
