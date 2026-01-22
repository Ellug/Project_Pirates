using UnityEngine;
using UnityEngine.Audio;

[DisallowMultipleComponent]
[RequireComponent(typeof(AudioSource))]
public sealed class PlayerFootstepSfx : MonoBehaviour
{
    [Header("Clips")]
    [SerializeField] private AudioClip[] _walkClips;
    [SerializeField] private AudioClip[] _runClips;

    [Header("Step Interval")]
    [SerializeField, Min(0.05f)] private float _walkInterval = 0.5f;
    [SerializeField, Min(0.05f)] private float _runInterval  = 0.32f;

    [Header("Mixer Output")]
    [SerializeField] private AudioMixerGroup _sfxGroup; // AudioMixer의 SFX 그룹

    [Header("3D Settings")]
    [SerializeField, Range(0f, 1f)] private float _volume = 0.75f;
    [SerializeField] private Vector2 _pitchRange = new(0.96f, 1.04f);
    [SerializeField] private float _minDistance = 1.5f;
    [SerializeField] private float _maxDistance = 18f;

    private PlayerModel _model;
    private AudioSource _src;
    private float _timer;

    private void Awake()
    {
        _model = GetComponent<PlayerModel>();
        _src = GetComponent<AudioSource>();

        _src.minDistance = Mathf.Max(0.01f, _minDistance);
        _src.maxDistance = Mathf.Max(_src.minDistance, _maxDistance);

        if (_sfxGroup != null)
            _src.outputAudioMixerGroup = _sfxGroup;
    }

    private void Update()
    {
        if (_model == null || _model.Animator == null) return;

        // 이동 여부는 애니 파라미터로 간단히(당신 코드 유지)
        bool isMoving = _model.Animator.GetFloat(_model.animNameOfMove) > 0.1f;

        if (!isMoving)
        {
            _timer = 0f;
            return;
        }

        bool isRunning = _model.IsRunning;
        float interval = isRunning ? _runInterval : _walkInterval;

        _timer += Time.deltaTime;
        if (_timer < interval) return;
        _timer = 0f;

        var clips = isRunning ? _runClips : _walkClips;
        var clip = PickRandom(clips);
        if (clip == null) return;

        _src.pitch = Random.Range(_pitchRange.x, _pitchRange.y);
        _src.PlayOneShot(clip, Mathf.Clamp01(_volume));
    }

    private static AudioClip PickRandom(AudioClip[] clips)
    {
        if (clips == null || clips.Length == 0) return null;
        return clips[Random.Range(0, clips.Length)];
    }
}
