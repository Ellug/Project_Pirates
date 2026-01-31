using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;
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

    [Header("Color")]
    [SerializeField] private Color _showColor = new Color(1f, 0f, 0f, 1f); // Red
    [SerializeField] private Color _clickColor = new Color(0f, 1f, 0f, 1f); // Green
    [SerializeField] private Color _defaultColor = Color.white;

    private readonly List<int> _sequence = new();
    private int _inputIndex;
    private int _round;
    private int _targetLen;
    private bool _isShowing;
    private bool _isPlaying;

    private Coroutine _showCor;
    private Coroutine _nextRoundCor;
    private Coroutine _flashCor;

    public override void Init()
    {
        if (_cellButtons == null || _cellButtons.Length == 0)
        {
            Debug.LogError("[MemoryMission] Cell Buttons가 비어있습니다.");
            return;
        }

        if (_cellImages == null || _cellImages.Length != _cellButtons.Length)
        {
            _cellImages = new Image[_cellButtons.Length];
            for (int i = 0; i < _cellButtons.Length; i++)
                _cellImages[i] = _cellButtons[i] != null ? _cellButtons[i].GetComponent<Image>() : null;
        }

        // 버튼/셀 연결
        BindStartButton();
        BindCellButtons();

        // 상태 초기화
        StopAllMissionCoroutines();

        _sequence.Clear();
        _round = 0;
        _inputIndex = 0;
        _isShowing = false;
        _isPlaying = false;

        _targetLen = (_roundLengths != null && _roundLengths.Length > 0) ? _roundLengths[0] : 0;

        SetCellsInteractable(false);
        ResetCellColors();
        UpdateUI();
        SetInfo("Start를 눌러 시작하세요");
    }

    private void OnDisable()
    {
        StopAllMissionCoroutines();

        if (_startButton != null)
            _startButton.onClick.RemoveListener(StartGame);

        if (_cellButtons != null)
        {
            for (int i = 0; i < _cellButtons.Length; i++)
            {
                int idx = i;
                if (_cellButtons[i] != null)
                    _cellButtons[i].onClick.RemoveListener(() => OnCellClicked(idx));
            }
        }
    }

    private void BindStartButton()
    {
        if (_startButton == null) return;

        _startButton.onClick.RemoveListener(StartGame);
        _startButton.onClick.AddListener(StartGame);
    }

    private void BindCellButtons()
    {
        for (int i = 0; i < _cellButtons.Length; i++)
        {
            Button b = _cellButtons[i];
            if (b == null) continue;

            var cell = b.GetComponent<MemoryCell>();
            if (cell != null)
            {
                cell.Init(this, i);
                continue;
            }

            // MemoryCell이 없다면 직접 연결
            int idx = i;
            b.onClick.RemoveListener(() => OnCellClicked(idx));
            b.onClick.AddListener(() => OnCellClicked(idx));
        }
    }

    private void StopAllMissionCoroutines()
    {
        if (_showCor != null) StopCoroutine(_showCor);
        if (_nextRoundCor != null) StopCoroutine(_nextRoundCor);
        if (_flashCor != null) StopCoroutine(_flashCor);

        _showCor = null;
        _nextRoundCor = null;
        _flashCor = null;
    }

    private void StartGame()
    {
        if (_isShowing) return;

        // 재시작시 리셋
        StopAllMissionCoroutines();
        ResetCellColors();
        SetCellsInteractable(false);

        _sequence.Clear();
        _round = 0;
        _inputIndex = 0;

        _isPlaying = true;
        _isShowing = false;

        NextRound();
    }

    private void NextRound()
    {
        if (!_isPlaying) return;

        int totalRounds = (_roundLengths != null) ? _roundLengths.Length : 0;
        if (totalRounds <= 0)
        {
            _isPlaying = false;
            SetInfo("라운드 설정(_roundLengths)이 비어있습니다.");
            return;
        }

        _round++;

        // 실제 라운드 기준으로 완료 판정
        if (_round > totalRounds)
        {
            SetInfo("성공! 미션 완료!");
            SetCellsInteractable(false);
            CompleteMission();
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

        for (int i = 0; i < _sequence.Count; i++)
        {
            int idx = _sequence[i];

            LightCell(idx, _showColor);
            yield return new WaitForSeconds(_lightOnTime);

            LightCell(idx, _defaultColor);
            yield return new WaitForSeconds(_lightOffGap);
        }

        _isShowing = false;
        SetInfo("이제 순서대로 눌러보세요");
        SetCellsInteractable(true);
    }

    public void OnCellClicked(int idx)
    {
        if (!_isPlaying) return;
        if (_isShowing) return;

        if (_sequence == null || _sequence.Count == 0) return;
        if (_inputIndex < 0 || _inputIndex >= _sequence.Count) return;

        // 클릭시 점등
        if (_flashCor != null) StopCoroutine(_flashCor);
        _flashCor = StartCoroutine(CoClickFlash(idx));

        // 정답 체크
        if (idx != _sequence[_inputIndex])
        {
            _isPlaying = false;
            SetCellsInteractable(false);
            SetInfo("틀렸습니다. Start를 눌러 재시도하세요.");
            return;
        }

        _inputIndex++;

        // 라운드 클리어
        if (_inputIndex >= _sequence.Count)
        {
            SetCellsInteractable(false);
            SetInfo("정답! 다음 라운드로");

            if (_nextRoundCor != null) StopCoroutine(_nextRoundCor);
            _nextRoundCor = StartCoroutine(CoNextRoundDelay());
        }
    }

    private IEnumerator CoNextRoundDelay()
    {
        yield return new WaitForSeconds(0.6f);
        NextRound();
    }

    private IEnumerator CoClickFlash(int idx)
    {
        LightCell(idx, _clickColor);
        yield return new WaitForSeconds(0.12f);
        LightCell(idx, _defaultColor);
        _flashCor = null;
    }

    private void SetCellsInteractable(bool on)
    {
        if (_cellButtons == null) return;
        for (int i = 0; i < _cellButtons.Length; i++)
            if (_cellButtons[i] != null) _cellButtons[i].interactable = on;
    }

    private void LightCell(int idx, Color color)
    {
        if (_cellImages == null) return;
        if (idx < 0 || idx >= _cellImages.Length) return;
        if (_cellImages[idx] == null) return;

        // 에셋없이 색만 조정
        _cellImages[idx].color = color;
    }

    private void ResetCellColors()
    {
        if (_cellImages == null) return;

        for (int i = 0; i < _cellImages.Length; i++)
            if (_cellImages[i] != null)
                _cellImages[i].color = _defaultColor;
    }

    private void UpdateUI()
    {
        int totalRounds = (_roundLengths != null) ? _roundLengths.Length : 0;

        if (_roundText != null)
            _roundText.text = $"Round: {_round}/{totalRounds} ({_targetLen}개)";
    }

    private void SetInfo(string msg)
    {
        if (_infoText != null)
            _infoText.text = msg;
    }
}
