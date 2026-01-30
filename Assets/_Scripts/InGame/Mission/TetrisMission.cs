using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class TetrisMission : MissionBase
{
    private const int PieceCellCount = 4;
    private enum PieceType { I, O, T, S, Z, J, L }

    [Header("UI")]
    [SerializeField] private TMP_Text _resultText;
    [SerializeField] private RectTransform _cellRoot;
    [SerializeField] private Color _emptyCellColor = new(0f, 0f, 0f, 0.2f);
    [SerializeField] private Color _dangerZoneColor = new(0.3f, 0.1f, 0.1f, 0.3f);  // 게임오버 위험 영역

    [Header("Board")]
    [SerializeField] private int _boardWidth = 10;
    [SerializeField] private int _boardHeight = 20;
    [SerializeField] private float _fallInterval = 0.3f;
    [SerializeField] private float _softDropInterval = 0.05f;
    [SerializeField] private int _linesToComplete = 20;

    [Header("Input")]
    [SerializeField] private float _delayAutoShift = 0.17f;    // 키 홀드 시 반복 시작까지 대기 시간
    [SerializeField] private float _autoRepeatRate = 0.03f;    // 반복 이동 간격

    // 각 조각의 기본 형태
    private static readonly Vector2Int[][] BaseShapes =
    {
        new[] { new Vector2Int(0, 1), new Vector2Int(1, 1), new Vector2Int(2, 1), new Vector2Int(3, 1) }, // I
        new[] { new Vector2Int(1, 1), new Vector2Int(2, 1), new Vector2Int(1, 2), new Vector2Int(2, 2) }, // O
        new[] { new Vector2Int(1, 2), new Vector2Int(0, 1), new Vector2Int(1, 1), new Vector2Int(2, 1) }, // T
        new[] { new Vector2Int(1, 2), new Vector2Int(2, 2), new Vector2Int(0, 1), new Vector2Int(1, 1) }, // S
        new[] { new Vector2Int(0, 2), new Vector2Int(1, 2), new Vector2Int(1, 1), new Vector2Int(2, 1) }, // Z
        new[] { new Vector2Int(0, 2), new Vector2Int(0, 1), new Vector2Int(1, 1), new Vector2Int(2, 1) }, // J
        new[] { new Vector2Int(2, 2), new Vector2Int(0, 1), new Vector2Int(1, 1), new Vector2Int(2, 1) }, // L
    };

    private static readonly Vector2Int[,,] ShapeCache = BuildShapeCache();
    private static Sprite _whiteSprite;

    private readonly Color[] _pieceColors =
    {
        new(0f, 0.9f, 0.9f, 1f),   // I - cyan
        new(1f, 0.92f, 0.2f, 1f),  // O - yellow
        new(0.75f, 0.3f, 0.9f, 1f),// T - purple
        new(0.2f, 0.9f, 0.35f, 1f),// S - green
        new(0.95f, 0.2f, 0.2f, 1f),// Z - red
        new(0.2f, 0.4f, 0.95f, 1f),// J - blue
        new(1f, 0.55f, 0.1f, 1f),  // L - orange
    };

    private Image[,] _cells;
    private int[,] _board;

    private PieceType _currentType;
    private Vector2Int _currentPosition;
    private int _currentRotation;

    private float _fallTimer;
    private bool _softDrop;
    private bool _isGameOver;
    private int _linesCleared;

    // DAS/ARR 입력 처리용
    private float _leftHoldTime;
    private float _rightHoldTime;
    private float _leftRepeatTimer;
    private float _rightRepeatTimer;

    public override void Init()
    {
        _resultText.text = "";
        BuildCells();
        StartNewGame();
    }

    void Update()
    {
        if (_isGameOver) return;

        HandleInput();
        StepFall(Time.deltaTime);
        Render();
    }

    // 보드 셀 이미지들 생성
    private void BuildCells()
    {
        // 기존 셀 제거
        for (int i = _cellRoot.childCount - 1; i >= 0; i--)
            Destroy(_cellRoot.GetChild(i).gameObject);

        _cells = new Image[_boardWidth, _boardHeight];
        for (int y = 0; y < _boardHeight; y++)
        {
            for (int x = 0; x < _boardWidth; x++)
            {
                var go = new GameObject($"Cell_{x}_{y}", typeof(RectTransform), typeof(Image));
                go.transform.SetParent(_cellRoot, false);

                var image = go.GetComponent<Image>();
                image.sprite = GetWhiteSprite();
                image.raycastTarget = false;
                image.color = _emptyCellColor;

                _cells[x, y] = image;
            }
        }
    }

    // 게임 상태 초기화 및 첫 조각 생성
    private void StartNewGame()
    {
        _board = new int[_boardWidth, _boardHeight];
        _linesCleared = 0;
        _fallTimer = 0f;
        _isGameOver = false;

        SpawnPiece();
        UpdateHud();
        Render();
    }

    // 키보드 입력 처리 (DAS/ARR 방식)
    private void HandleInput()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        float dt = Time.deltaTime;

        // 좌측 이동
        if (keyboard.leftArrowKey.wasPressedThisFrame)
        {
            TryMove(Vector2Int.left);
            _leftHoldTime = 0f;
            _leftRepeatTimer = 0f;
        }
        else if (keyboard.leftArrowKey.isPressed)
        {
            _leftHoldTime += dt;
            if (_leftHoldTime >= _delayAutoShift)
            {
                _leftRepeatTimer += dt;
                while (_leftRepeatTimer >= _autoRepeatRate)
                {
                    TryMove(Vector2Int.left);
                    _leftRepeatTimer -= _autoRepeatRate;
                }
            }
        }

        // 우측 이동
        if (keyboard.rightArrowKey.wasPressedThisFrame)
        {
            TryMove(Vector2Int.right);
            _rightHoldTime = 0f;
            _rightRepeatTimer = 0f;
        }
        else if (keyboard.rightArrowKey.isPressed)
        {
            _rightHoldTime += dt;
            if (_rightHoldTime >= _delayAutoShift)
            {
                _rightRepeatTimer += dt;
                while (_rightRepeatTimer >= _autoRepeatRate)
                {
                    TryMove(Vector2Int.right);
                    _rightRepeatTimer -= _autoRepeatRate;
                }
            }
        }

        // 회전
        if (keyboard.upArrowKey.wasPressedThisFrame)
            TryRotate();

        // 하드드롭
        if (keyboard.spaceKey.wasPressedThisFrame)
            HardDrop();

        // 소프트드롭
        if (keyboard.downArrowKey.wasPressedThisFrame)
            TryMove(Vector2Int.down);

        _softDrop = keyboard.downArrowKey.isPressed;
    }

    // 즉시 바닥까지 떨어뜨림
    private void HardDrop()
    {
        while (TryMove(Vector2Int.down)) { }
        LockPiece();
    }

    // 시간당 자동 낙하
    private void StepFall(float deltaTime)
    {
        float interval = _softDrop ? _softDropInterval : _fallInterval;
        _fallTimer += deltaTime;
        if (_fallTimer < interval) return;

        _fallTimer = 0f;
        if (!TryMove(Vector2Int.down))
            LockPiece();
    }

    // 새 조각 생성 (상단 중앙)
    private void SpawnPiece()
    {
        _currentType = (PieceType)Random.Range(0, 7);
        _currentRotation = 0;
        _currentPosition = new Vector2Int((_boardWidth - 4) / 2, _boardHeight - 4);

        if (!IsValidPosition(_currentPosition, _currentRotation))
            GameOver();
    }

    private bool TryMove(Vector2Int delta)
    {
        var newPos = _currentPosition + delta;
        if (!IsValidPosition(newPos, _currentRotation)) return false;

        _currentPosition = newPos;
        return true;
    }

    private void TryRotate()
    {
        int nextRotation = (_currentRotation + 1) & 3;
        if (IsValidPosition(_currentPosition, nextRotation))
        {
            _currentRotation = nextRotation;
            return;
        }

        // Wall Kick
        Vector2Int[] kicks = { new(-1, 0), new(1, 0), new(-2, 0), new(2, 0), new(0, 1) };
        foreach (var kick in kicks)
        {
            var kickedPos = _currentPosition + kick;
            if (IsValidPosition(kickedPos, nextRotation))
            {
                _currentPosition = kickedPos;
                _currentRotation = nextRotation;
                return;
            }
        }
    }

    private bool IsValidPosition(Vector2Int pos, int rotation)
    {
        for (int i = 0; i < PieceCellCount; i++)
        {
            var cell = ShapeCache[(int)_currentType, rotation, i];
            int x = pos.x + cell.x;
            int y = pos.y + cell.y;

            if (x < 0 || x >= _boardWidth || y < 0 || y >= _boardHeight)
                return false;
            if (_board[x, y] != 0)
                return false;
        }
        return true;
    }

    // 현재 조각을 보드에 고정
    private void LockPiece()
    {
        for (int i = 0; i < PieceCellCount; i++)
        {
            var cell = ShapeCache[(int)_currentType, _currentRotation, i];
            int x = _currentPosition.x + cell.x;
            int y = _currentPosition.y + cell.y;
            _board[x, y] = (int)_currentType + 1;
        }

        _linesCleared += ClearFullLines();

        if (_linesCleared >= _linesToComplete)
        {
            _isGameOver = true;
            UpdateHud(true);
            StartCoroutine(DelayedComplete());
            return;
        }

        SpawnPiece();
        UpdateHud();
    }

    // 가득 찬 줄 제거 후 위 줄 내림
    private int ClearFullLines()
    {
        int cleared = 0;

        for (int y = 0; y < _boardHeight; y++)
        {
            bool full = true;
            for (int x = 0; x < _boardWidth; x++)
            {
                if (_board[x, y] == 0) { full = false; break; }
            }

            if (!full) continue;

            cleared++;
            for (int yy = y; yy < _boardHeight - 1; yy++)
            {
                for (int x = 0; x < _boardWidth; x++)
                    _board[x, yy] = _board[x, yy + 1];
            }
            for (int x = 0; x < _boardWidth; x++)
                _board[x, _boardHeight - 1] = 0;

            y--;
        }

        return cleared;
    }

    // 보드와 현재 조각 렌더링
    private void Render()
    {
        int dangerLine = _boardHeight - 4;  // 스폰 영역 (상단 4줄)

        for (int y = 0; y < _boardHeight; y++)
        {
            for (int x = 0; x < _boardWidth; x++)
            {
                int cell = _board[x, y];
                if (cell != 0)
                    _cells[x, y].color = _pieceColors[cell - 1];
                else
                    _cells[x, y].color = y >= dangerLine ? _dangerZoneColor : _emptyCellColor;
            }
        }

        if (_isGameOver) return;

        Color pieceColor = _pieceColors[(int)_currentType];
        for (int i = 0; i < PieceCellCount; i++)
        {
            var cell = ShapeCache[(int)_currentType, _currentRotation, i];
            int x = _currentPosition.x + cell.x;
            int y = _currentPosition.y + cell.y;
            _cells[x, y].color = pieceColor;
        }
    }

    private void GameOver()
    {
        _isGameOver = true;
        UpdateHud();
    }

    private IEnumerator DelayedComplete()
    {
        yield return new WaitForSeconds(2f);
        CompleteMission();
    }

    private void UpdateHud(bool completed = false)
    {
        if (completed)
            _resultText.text = "미션 완료!";
        else if (_isGameOver)
            _resultText.text = "Game Over";
    }

    private static Vector2Int RotateCW(Vector2Int p) => new(3 - p.y, p.x);

    private static Vector2Int[,,] BuildShapeCache()
    {
        var cache = new Vector2Int[7, 4, PieceCellCount];
        for (int p = 0; p < BaseShapes.Length; p++)
        {
            for (int i = 0; i < PieceCellCount; i++)
                cache[p, 0, i] = BaseShapes[p][i];

            for (int r = 1; r < 4; r++)
            {
                for (int i = 0; i < PieceCellCount; i++)
                    cache[p, r, i] = RotateCW(cache[p, r - 1, i]);
            }
        }
        return cache;
    }

    private static Sprite GetWhiteSprite()
    {
        if (_whiteSprite != null) return _whiteSprite;

        var tex = Texture2D.whiteTexture;
        _whiteSprite = Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
        return _whiteSprite;
    }
}
