using Photon.Pun;
using Photon.Voice.Unity;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public sealed class VoiceManager : MonoBehaviourPunCallbacks
{
    [SerializeField] private Recorder _recorder;
    private List<VoiceUserSetting> _remoteUserSettings = new();
    private Dictionary<int, Speaker> _speakerCache = new();

    public static VoiceManager Instance { get; private set; }
    private bool _pttPressed;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        LoadAndApplySettings();
        InputManager.Instance.OnPtt += OnPttChanged;
    }

    public override void OnDisable()
    {
        base.OnDisable();

        if (InputManager.Instance != null)
            InputManager.Instance.OnPtt -= OnPttChanged;
    }

    private void OnPttChanged(bool pressed)
    {
        _pttPressed = pressed;
        ApplyTransmitByCurrentMode();
    }

    private void ApplyTransmitByCurrentMode()
    {
        if (_recorder == null) return;

        int myType = PlayerPrefs.GetInt(VoiceParam.MyMicTypeKey, 0);          // 0: 상시, 1: PTT
        bool myMute = PlayerPrefs.GetInt(VoiceParam.MyMicMuteKey, 0) == 1;

        if (myMute)
        {
            _recorder.TransmitEnabled = false;
            return;
        }

        // 상시 송출
        if (myType == 0)
        {
            _recorder.TransmitEnabled = true;
            return;
        }

        // PTT
        _recorder.TransmitEnabled = _pttPressed;
    }

    public void ConnectVoice()
    {
        var voiceClient = Photon.Voice.PUN.PunVoiceClient.Instance;

        if (voiceClient == null || voiceClient.Client == null) return;
        if (voiceClient.Client.IsConnected || voiceClient.Client.InRoom) return;

        Debug.Log("[Voice] Connecting to voice room...");
        voiceClient.ConnectAndJoinRoom();
    }

    public void OnSpeakerCreated(Speaker speaker)
    {
        PhotonView pv = speaker.GetComponentInParent<PhotonView>();
        if (pv != null)
        {
            int actorNr = pv.OwnerActorNr;
            _speakerCache[actorNr] = speaker; // 등록 또는 갱신
            UpdateSpeakerOutput(actorNr);
        }
    }


    public void LoadAndApplySettings()
    {
        float myVol = PlayerPrefs.GetFloat(VoiceParam.MyMicVolumeKey, 1f);
        int myType = PlayerPrefs.GetInt(VoiceParam.MyMicTypeKey, 0);
        bool myMute = PlayerPrefs.GetInt(VoiceParam.MyMicMuteKey, 0) == 1;

        ApplyMyMicSettings(myVol, myType, myMute);
        ApplyMasterOutputSettings();
    }

    // 내 마이크 설정
    public void ApplyMyMicSettings(float vol, int type, bool isMuted)
    {
        if (_recorder == null) return;

        if (isMuted)
            _recorder.TransmitEnabled = false;
        else
            _recorder.TransmitEnabled = (type == 0);

        ExitGames.Client.Photon.Hashtable props = new ExitGames.Client.Photon.Hashtable { { "v_vol", vol }, { "v_mute", isMuted } };
        PhotonNetwork.LocalPlayer.SetCustomProperties(props);

        ApplyTransmitByCurrentMode();
    }

    // 타인 개별 볼륨 적용
    public void SetRemoteVolume(int actorNumber, float vol)
    {
        PlayerPrefs.SetFloat(VoiceParam.GetRemotePlayerKey(actorNumber), vol);
        UpdateSpeakerOutput(actorNumber);
    }

    //타인 개별 뮤트 적용
    public void SetRemoteMute(int actorNumber, bool isOn)
    {
        PlayerPrefs.SetInt(VoiceParam.GetRemoteMuteKey(actorNumber), isOn ? 1 : 0);
        
        if (isOn == false)
        {
            PlayerPrefs.SetInt(VoiceParam.MasterOutputMuteKey, 0);
        }
        UpdateSpeakerOutput(actorNumber);
    }

    //마스터 인풋 볼륨 변경 시 모든 스피커 즉시 갱신
    public void ApplyMasterOutputSettings()
    {
        foreach (var player in PhotonNetwork.PlayerList)
        {
            if (player.IsLocal) continue;
            UpdateSpeakerOutput(player.ActorNumber);
        }
    }

    // 마스터 뮤트 설정 시 모든 개별 세팅도 함께 변경
    public void SetAllRemoteMute(bool isMute)
    {
        PlayerPrefs.SetInt(VoiceParam.MasterOutputMuteKey, isMute ? 1 : 0);

        foreach (var player in PhotonNetwork.PlayerList)
        {
            if (player.IsLocal) continue;
            PlayerPrefs.SetInt(VoiceParam.GetRemoteMuteKey(player.ActorNumber), isMute ? 1 : 0);
            UpdateSpeakerOutput(player.ActorNumber);
        }
    }

    // 특정 플레이어의 오디오 소스를 찾아 최종 볼륨 및 뮤트 상태 적용
    private void UpdateSpeakerOutput(int actorNumber)
    {
        Speaker speaker = FindSpeakerByActorNumber(actorNumber);
        if (speaker == null || !speaker.TryGetComponent<AudioSource>(out var source)) return;

        var targetPlayer = PhotonNetwork.CurrentRoom.GetPlayer(actorNumber);

        float remoteSettedVol = 1f;
        bool remoteIsMuted = false;
        if (targetPlayer != null && targetPlayer.CustomProperties.ContainsKey("v_vol"))
        {
            remoteSettedVol = (float)targetPlayer.CustomProperties["v_vol"];
            //뮤트는 플레이어 위에 아이콘 띄울때나 쓸 듯 싶음.
            remoteIsMuted = (bool)targetPlayer.CustomProperties["v_mute"];
        }

        float masterVol = PlayerPrefs.GetFloat(VoiceParam.MasterOutputKey, 1f);
        bool masterMuted = PlayerPrefs.GetInt(VoiceParam.MasterOutputMuteKey, 0) == 1;

        float personalVol = PlayerPrefs.GetFloat(VoiceParam.GetRemotePlayerKey(actorNumber), 1f);
        bool personalMuted = PlayerPrefs.GetInt(VoiceParam.GetRemoteMuteKey(actorNumber), 0) == 1;

        if (masterMuted || personalMuted)
        {
            source.mute = true;
        }
        else
        {
            source.mute = false;
            source.volume = Mathf.Clamp01(personalVol * masterVol * remoteSettedVol);
        }
    }

    // 딕셔너리를 통해 씬 내의 VoicePrefab 검색
    private Speaker FindSpeakerByActorNumber(int actorNumber)
    {
        if (_speakerCache.TryGetValue(actorNumber, out Speaker speaker))
        {
            if (speaker != null) return speaker;
            _speakerCache.Remove(actorNumber); // 파괴된 객체면 제거
        }
        return null;
    }

    //플레이어 나갈때 캐시 삭제
    public void RemoveSpeakerCache(int actorNumber)
    {
        if (_speakerCache.ContainsKey(actorNumber))
            _speakerCache.Remove(actorNumber);
    }

    // UI표시를 위해 다른 플레이어들의 현재 설정값 목록을 가져옴
    public IEnumerator Co_GetVoiceUserSettings(System.Action<List<VoiceUserSetting>> callback)
    {
        yield return new WaitForSeconds(0.1f);
        _remoteUserSettings.Clear();

        foreach (var player in PhotonNetwork.PlayerList)
        {
            if (player.IsLocal) continue;

            int id = player.ActorNumber;
            float vol = PlayerPrefs.GetFloat(VoiceParam.GetRemotePlayerKey(id), 1f);
            bool mute = PlayerPrefs.GetInt(VoiceParam.GetRemoteMuteKey(id), 0) == 1;

            _remoteUserSettings.Add(new VoiceUserSetting(id, player.NickName, vol, mute));
        }
        callback?.Invoke(_remoteUserSettings);
    }

    //실시간 변화 감지 콜백함수
    public override void OnPlayerPropertiesUpdate(Photon.Realtime.Player targetPlayer, ExitGames.Client.Photon.Hashtable changedProps)
    {
        if (changedProps.ContainsKey("v_vol") || changedProps.ContainsKey("v_mute"))
        {
            UpdateSpeakerOutput(targetPlayer.ActorNumber);
        }
    }
}