using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;

// TODO
// 클릭시 서버 연결 시도
// 인풋필드 작성한 값으로 닉네임 설정
// 연결 중 Loading 메시지 출력

public class ConnectButton : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Button _connectButton;
    [SerializeField] private NicknameInput _nicknameInput;
    [SerializeField] private TextMeshProUGUI _loadingText;

    private string _confirmedNickname = "";

    private void Start()
    {
        if(_loadingText != null)
        {
            _loadingText.gameObject.SetActive(false);
        }

        if(_connectButton != null)
        {
            _connectButton.onClick.AddListener(OnClickConnect);
            _connectButton.interactable = true; 
        }
    }

    private void OnClickConnect()
    {
        if(_nicknameInput == null)
        {
            return;
        }

        // 현재 인풋 값으로 닉네임 검증 시도
        _nicknameInput.TryConfirmCurrentInput();

        // 확정된 닉네임이 없으면 return
        string nickname = _nicknameInput.ConfirmedNickname;
        if(string.IsNullOrWhiteSpace(nickname))
        {
            return;
        }

        // Photon 닉네임 설정
        PhotonNetwork.NickName = nickname;
    }

    private void SetLoading(bool isOn, string msg)
    {
        if(_loadingText != null)
        {
            _loadingText.text = msg;
            _loadingText.gameObject.SetActive(isOn);
        }

        if(_connectButton != null)
        {
            _connectButton.interactable = !isOn;
        }
    }
}
