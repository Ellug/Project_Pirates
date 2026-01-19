using Photon.Pun;
using Photon.Voice.Unity;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public sealed class VoiceManager : Singleton<VoiceManager>
{
    [SerializeField] private Recorder _recorder;
    private List<VoiceUserSetting> _remoteUserSettings = new();

    void Start()
    {
        LoadAndApplySettings();
    }

    // PTT 입력 감지를 위한 Update (선택 사항)
    void Update()
    {
        int myType = PlayerPrefs.GetInt(VoiceParam.MyMicTypeKey, 0);
        bool myMute = PlayerPrefs.GetInt(VoiceParam.MyMicMuteKey, 0) == 1;

        if (myType == 1 && !myMute)
        {
            if (_recorder != null)
            {
                bool isPressingV = Keyboard.current != null && Keyboard.current[Key.V].isPressed;
                _recorder.TransmitEnabled = isPressingV;
            }
        }
    }

    public void OnSpeakerCreated(Speaker speaker)
    {
        PhotonView pv = speaker.GetComponentInParent<PhotonView>();
        if (pv != null)
        {
            UpdateSpeakerOutput(pv.OwnerActorNr);
        }
    }

    public void LoadAndApplySettings()
    {
        float myVol = PlayerPrefs.GetFloat(VoiceParam.MyMicVolumeKey, 1f);
        int myType = PlayerPrefs.GetInt(VoiceParam.MyMicTypeKey, 0);
        bool myMute = PlayerPrefs.GetInt(VoiceParam.MyMicMuteKey, 0) == 1;

        ApplyMyMicSettings(myVol, myType, myMute);
        ApplyMasterInputSettings();
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
            PlayerPrefs.SetInt(VoiceParam.MasterInputMuteKey, 0);
        }
        UpdateSpeakerOutput(actorNumber);
    }

    //마스터 인풋 볼륨 변경 시 모든 스피커 즉시 갱신
    public void ApplyMasterInputSettings()
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
        PlayerPrefs.SetInt(VoiceParam.MasterInputMuteKey, isMute ? 1 : 0);

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

        float masterVol = PlayerPrefs.GetFloat(VoiceParam.MasterInputKey, 1f);
        bool masterMuted = PlayerPrefs.GetInt(VoiceParam.MasterInputMuteKey, 0) == 1;

        float personalVol = PlayerPrefs.GetFloat(VoiceParam.GetRemotePlayerKey(actorNumber), 1f);
        bool personalMuted = PlayerPrefs.GetInt(VoiceParam.GetRemoteMuteKey(actorNumber), 0) == 1;

        if (masterMuted || personalMuted)
        {
            source.mute = true;
        }
        else
        {
            source.mute = false;
            source.volume = personalVol * masterVol;
        }
    }

    // ActorNumber 통해 씬 내의 모든 VoicePrefab 검색
    private Speaker FindSpeakerByActorNumber(int actorNumber)
    {
        var allSpeakers = FindObjectsByType<Speaker>(FindObjectsSortMode.None);
        foreach (var s in allSpeakers)
        {
            PhotonView pv = s.GetComponentInParent<PhotonView>();
            if (pv != null && pv.OwnerActorNr == actorNumber)
            {
                return s;
            }
        }
        return null;
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
}