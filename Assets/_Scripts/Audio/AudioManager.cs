using System.Collections;
using UnityEngine;
using UnityEngine.Audio;

public sealed class AudioManager : Singleton<AudioManager>
{
    [Header("Mixer")]
    [SerializeField] private AudioMixer _mixer;

    [Header("Sources")]
    [SerializeField] private AudioSource _bgmA;
    [SerializeField] private AudioSource _bgmB;
    [SerializeField] private AudioSource _uiSource;

    [Header("BGM Table")]
    [SerializeField] private BgmTable _bgmTable;

    [Header("BGM Crossfade")]
    [SerializeField, Min(0f)] private float _bgmFadeSeconds = 1.5f;

    private Coroutine _fadeCo;

    protected override void OnSingletonAwake()
    {
        PrepareBgm(_bgmA);
        PrepareBgm(_bgmB);
    }

    private void Start()
    {
        LoadAndApplySavedVolumes();
    }

    private static void PrepareBgm(AudioSource s)
    {
        if (s == null) return;
        s.playOnAwake = false;
        s.loop = true;
        s.spatialBlend = 0f;
        s.volume = 0f;
    }

    // BGM
    public void PlayBgm(SceneState state)
    {
        if (_bgmTable == null) return;
        if (!_bgmTable.TryGetClip(state, out var clip) || clip == null) return;

        CrossFadeTo(clip);
    }

    private void CrossFadeTo(AudioClip clip)
    {
        if (_bgmA == null || _bgmB == null) return;

        var from = GetDominantSource();
        var to   = (from == _bgmA) ? _bgmB : _bgmA;

        // 같은 트랙이면 무시
        if (from.isPlaying && from.clip == clip) return;

        // to 재사용
        if (to.isPlaying) to.Stop();
        to.clip = clip;
        to.volume = 0f;
        to.Play();

        if (_fadeCo != null) StopCoroutine(_fadeCo);
        _fadeCo = StartCoroutine(CoCrossFade(from, to, 1f, _bgmFadeSeconds));
    }

    private AudioSource GetDominantSource()
    {
        // 둘 다 안 돌면 A를 기준으로 (첫 재생)
        if (!_bgmA.isPlaying && !_bgmB.isPlaying) return _bgmA;
        return (_bgmA.volume >= _bgmB.volume) ? _bgmA : _bgmB;
    }

    private IEnumerator CoCrossFade(AudioSource from, AudioSource to, float toTarget, float seconds)
    {
        if (seconds <= 0f)
        {
            if (from != null && from.isPlaying) from.Stop();
            if (from != null) from.volume = 0f;

            if (to != null) to.volume = toTarget;
            _fadeCo = null;
            yield break;
        }

        float t = 0f;
        float fromStart = (from != null) ? from.volume : 0f;

        while (t < seconds)
        {
            t += Time.deltaTime;
            float a = Mathf.Clamp01(t / seconds);

            if (from != null) from.volume = Mathf.Lerp(fromStart, 0f, a);
            if (to != null)   to.volume   = Mathf.Lerp(0f, toTarget, a);

            yield return null;
        }

        if (from != null)
        {
            from.volume = 0f;
            if (from.isPlaying) from.Stop();
            from.clip = null;
        }

        if (to != null) to.volume = toTarget;

        _fadeCo = null;
    }

    // UI
    public void PlayUi(AudioClip clip, float volume = 1f)
    {
        if (clip == null || _uiSource == null) return;
        _uiSource.PlayOneShot(clip, Mathf.Clamp01(volume));
    }

    // Volume
    public void LoadAndApplySavedVolumes()
    {
        float master = PlayerPrefs.GetFloat(AudioParam.MASTER_KEY, 1f);
        float bgm    = PlayerPrefs.GetFloat(AudioParam.BGM_KEY, 1f);
        float ui     = PlayerPrefs.GetFloat(AudioParam.UI_KEY, 1f);

        SetVolume(AudioBus.Master, master);
        SetVolume(AudioBus.BGM, bgm);
        SetVolume(AudioBus.UI, ui);
    }

    public void SetVolume(AudioBus bus, float normalized01)
    {
        if (_mixer == null) return;

        float db = NormalizedToDb(normalized01);

        switch (bus)
        {
            case AudioBus.BGM: _mixer.SetFloat(AudioParam.BGM_PARAM, db); break;
            case AudioBus.UI:  _mixer.SetFloat(AudioParam.UI_PARAM, db);  break;
            default:           _mixer.SetFloat(AudioParam.MASTER_PARAM, db); break;
        }
    }

    private static float NormalizedToDb(float v01)
    {
        v01 = Mathf.Clamp(v01, 0.0001f, 1f);
        return Mathf.Log10(v01) * 20f;
    }
}
