using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MemoryMission : MissionBase
{
    [Header("Cell (0~8)")]
    [SerializeField] private Button[] _cellButtons;
    [SerializeField] private Image[] _cellImages;

    [Header("UI")]
    [SerializeField] private Button _startButton;
    [SerializeField] private TMP_Text _roundText;
    [SerializeField] private TMP_Text _infoText;

    [Header("Config")]
    [SerializeField] private int _maxRound = 3;
    [SerializeField] private int[] _roundLengths = { 5, 6, 7 };
    [SerializeField] private float _lightOnTime = 0.35f;
    [SerializeField] private float _lightOffGap = 0.15f;
    [SerializeField] private float _preShowDelay = 0.3f;

    private readonly List<int> _sequence = new();
    private int _inputIndex;
    private int _round;
    private int _targetLen;
    private bool _isShowing;
    private bool _isPlaying;

    private Coroutine _showCor;

    protected override void Awake()
    {
        base.Awake();

        SetCellsInteractable(false);

        if (_startButton != null)
            _startButton.onClick.AddListener(StartGame);

        UpdateUI();
        SetInfo("Start를 눌러 시작하세요");
        ResetCellColors();
    }

    private void OnDisable()
    {
        if (_startButton != null)
            _startButton.onClick.RemoveListener(StartGame);
    }

    private void StartGame()
    {
        if (isFinished) return;
        if (_isShowing) return;

        _sequence.Clear();
        _round = 0;
        _isPlaying = true;

        NextRound();
    }

    private void NextRound()
    {
        if (!_isPlaying) return;

        _round++;
        if(_round > _roundLengths.Length)
        {
            CompleteMission();
            SetInfo("성공! 미션 완료!");
            SetCellsInteractable(false);
            return;
        }

        _targetLen = _roundLengths[_round - 1];
        _inputIndex = 0;

        // 라운드마다 새 패턴
        _sequence.Clear();
        while (_sequence.Count < _targetLen)
            _sequence.Add(Random.Range(0, 9));

        UpdateUI();
        SetInfo($"라운드 {_round}: 패턴을 기억하세요! ({_targetLen}개)");

        if (_showCor != null) StopCoroutine(_showCor);
        _showCor = StartCoroutine(CoShowSequence());
    }

    private IEnumerator CoShowSequence()
    {
        _isShowing = true;
        SetCellsInteractable(false);
        ResetCellColors();

        yield return new WaitForSeconds(_preShowDelay);

        foreach (int idx in _sequence)
        {
            LightCell(idx, true);
            yield return new WaitForSeconds(_lightOnTime);

            LightCell(idx, false);
            yield return new WaitForSeconds(_lightOffGap);
        }

        _isShowing = false;
        SetInfo("이제 순서대로 눌러보세요");
        SetCellsInteractable(true);
    }

    public void OnCellClicked(int idx)
    {
        if (isFinished) return;
        if (!_isPlaying) return;
        if (_isShowing) return;

        // 클릭시 짧게 깜빡
        StartCoroutine(CoClickFlash(idx));

        // 정답 체크
        if(idx != _sequence[_inputIndex])
        {
            SetInfo("틀렸습니다. 미션 실패");
            SetCellsInteractable(false);
            FailMission();
            _isPlaying = false;
            return;
        }

        _inputIndex++;

        // 라운드 클리어
        if(_inputIndex >= _sequence.Count)
        {
            SetCellsInteractable(false);
            SetInfo("정답! 다음 라운드로");
            StartCoroutine(CoNextRoundDelay());
        }
    }

    private IEnumerator CoNextRoundDelay()
    {
        yield return new WaitForSeconds(0.6f);
        NextRound();
    }

    private IEnumerator CoClickFlash(int idx)
    {
        LightCell(idx, true);
        yield return new WaitForSeconds(0.12f);
        LightCell(idx, false);
    }

    private void SetCellsInteractable(bool on)
    {
        if (_cellButtons == null) return;
        for (int i = 0; i < _cellButtons.Length; i++)
            if (_cellButtons[i] != null) _cellButtons[i].interactable = on;
    }

    private void LightCell(int idx, bool on)
    {
        if (_cellButtons == null || idx < 0 || idx >= _cellImages.Length) return;

        // 에셋 없이 색만 조절
        _cellImages[idx].color = on ? new Color(1f, 1f, 0.4f, 1f) : Color.white;
    }

    private void ResetCellColors()
    {
        if (_cellButtons == null) return;
        for (int i = 0; i < _cellImages.Length; i++)
            if (_cellImages[i] != null) _cellImages[i].color = Color.white;
    }

    private void UpdateUI()
    {
        if (_roundText != null)
            _roundText.text = $"Round: {_round}/{_maxRound} ({_targetLen}개)";
    }

    private void SetInfo(string msg)
    {
        if (_infoText != null)
            _infoText.text = msg;
    }

    protected override void OnMissionFailed()
    {
        
    }
}
