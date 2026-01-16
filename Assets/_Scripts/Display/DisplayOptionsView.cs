using System.Collections.Generic;
using TMPro;
using UnityEngine;

public sealed class DisplayOptionsView : MonoBehaviour
{
    [Header("Dropdowns")]
    [SerializeField] private TMP_Dropdown _resolutionDropdown;
    [SerializeField] private TMP_Dropdown _screenModeDropdown;

    private readonly List<(int w, int h)> _resList = new();
    private readonly List<FullScreenMode> _modeList = new();

    private bool _suppress;

    private void OnEnable()
    {
        BuildModeOptions();
        BuildResolutionOptions16By9();

        SyncFromSaved();
        Bind();
    }

    private void OnDisable()
    {
        Unbind();
        PlayerPrefs.Save();
    }

    private void Bind()
    {
        if (_resolutionDropdown != null) _resolutionDropdown.onValueChanged.AddListener(OnResolutionChanged);
        if (_screenModeDropdown != null) _screenModeDropdown.onValueChanged.AddListener(OnModeChanged);
    }

    private void Unbind()
    {
        if (_resolutionDropdown != null) _resolutionDropdown.onValueChanged.RemoveListener(OnResolutionChanged);
        if (_screenModeDropdown != null) _screenModeDropdown.onValueChanged.RemoveListener(OnModeChanged);
    }

    private void BuildModeOptions()
    {
        _modeList.Clear();

        // 필요에 맞게 노출만 조절하세요.
        _modeList.Add(FullScreenMode.Windowed);
        _modeList.Add(FullScreenMode.FullScreenWindow);   // 테두리 없는 전체화면
        _modeList.Add(FullScreenMode.ExclusiveFullScreen); // 전체화면

        if (_screenModeDropdown == null) return;

        _screenModeDropdown.ClearOptions();
        _screenModeDropdown.AddOptions(new List<string>
        {
            "창모드",
            "전체화면 (테두리 없음)",
            "전체화면"
        });
    }

    private void BuildResolutionOptions16By9()
    {
        _resList.Clear();

        // 중복 제거용
        var seen = new HashSet<long>();

        var resolutions = Screen.resolutions;
        for (int i = 0; i < resolutions.Length; i++)
        {
            int w = resolutions[i].width;
            int h = resolutions[i].height;

            // 16:9만 필터 (오차 허용)
            float aspect = (float)w / h;
            if (Mathf.Abs(aspect - (16f / 9f)) > 0.02f)
                continue;

            long key = ((long)w << 32) | (uint)h;
            if (!seen.Add(key)) continue;

            _resList.Add((w, h));
        }

        // 혹시 16:9가 하나도 없으면(드문 케이스) 전체를 보여주는 안전장치
        if (_resList.Count == 0)
        {
            for (int i = 0; i < resolutions.Length; i++)
            {
                int w = resolutions[i].width;
                int h = resolutions[i].height;
                long key = ((long)w << 32) | (uint)h;
                if (!seen.Add(key)) continue;
                _resList.Add((w, h));
            }
        }

        if (_resolutionDropdown == null) return;

        _resolutionDropdown.ClearOptions();

        var labels = new List<string>(_resList.Count);
        for (int i = 0; i < _resList.Count; i++)
        {
            var (w, h) = _resList[i];
            labels.Add($"{w} x {h} (16:9)");
        }
        _resolutionDropdown.AddOptions(labels);
    }

    private void SyncFromSaved()
    {
        _suppress = true;

        int savedW = PlayerPrefs.GetInt(DisplayParam.WIDTH_KEY,  Screen.width);
        int savedH = PlayerPrefs.GetInt(DisplayParam.HEIGHT_KEY, Screen.height);
        var savedModeInt = PlayerPrefs.GetInt(DisplayParam.MODE_KEY, (int)Screen.fullScreenMode);
        var savedMode = (FullScreenMode)savedModeInt;

        // dropdown index 찾기
        int resIndex = FindResolutionIndex(savedW, savedH);
        int modeIndex = FindModeIndex(savedMode);

        if (_resolutionDropdown != null) _resolutionDropdown.SetValueWithoutNotify(resIndex);
        if (_screenModeDropdown != null) _screenModeDropdown.SetValueWithoutNotify(modeIndex);

        // 저장값을 실제로 적용(옵션 패널 열 때 즉시 현재 상태로 보정)
        if (DisplayManager.Instance != null && _resList.Count > 0 && _modeList.Count > 0)
        {
            var (w, h) = _resList[resIndex];
            var mode = _modeList[modeIndex];
            DisplayManager.Instance.Apply(w, h, mode);
        }

        _suppress = false;
    }

    private int FindResolutionIndex(int w, int h)
    {
        for (int i = 0; i < _resList.Count; i++)
            if (_resList[i].w == w && _resList[i].h == h)
                return i;

        // 없으면 현재 해상도와 가장 가까운 값
        int best = 0;
        int bestDist = int.MaxValue;
        for (int i = 0; i < _resList.Count; i++)
        {
            int dw = _resList[i].w - Screen.width;
            int dh = _resList[i].h - Screen.height;
            int dist = dw * dw + dh * dh;
            if (dist < bestDist) { bestDist = dist; best = i; }
        }
        return best;
    }

    private int FindModeIndex(FullScreenMode mode)
    {
        for (int i = 0; i < _modeList.Count; i++)
            if (_modeList[i] == mode)
                return i;
        return 0;
    }

    private void OnResolutionChanged(int index)
    {
        if (_suppress) return;
        if (DisplayManager.Instance == null) return;
        if (index < 0 || index >= _resList.Count) return;

        var (w, h) = _resList[index];

        // 현재 모드 유지
        var mode = GetSelectedModeOrCurrent();
        DisplayManager.Instance.Apply(w, h, mode);
        DisplayManager.Instance.Save(w, h, mode);
    }

    private void OnModeChanged(int index)
    {
        if (_suppress) return;
        if (DisplayManager.Instance == null) return;
        if (index < 0 || index >= _modeList.Count) return;

        var mode = _modeList[index];

        // 현재 해상도 유지
        var (w, h) = GetSelectedResolutionOrCurrent();
        DisplayManager.Instance.Apply(w, h, mode);
        DisplayManager.Instance.Save(w, h, mode);
    }

    private (int w, int h) GetSelectedResolutionOrCurrent()
    {
        if (_resolutionDropdown != null)
        {
            int idx = _resolutionDropdown.value;
            if (0 <= idx && idx < _resList.Count) return _resList[idx];
        }
        return (Screen.width, Screen.height);
    }

    private FullScreenMode GetSelectedModeOrCurrent()
    {
        if (_screenModeDropdown != null)
        {
            int idx = _screenModeDropdown.value;
            if (0 <= idx && idx < _modeList.Count) return _modeList[idx];
        }
        return Screen.fullScreenMode;
    }
}
