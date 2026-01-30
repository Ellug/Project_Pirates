using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class TetrisMission : MissionBase
{
    private const int PieceCellCount = 4;

    private enum PieceType
    {
        I,
        O,
        T,
        S,
        Z,
        J,
        L
    }

    [Header("UI")]
    [SerializeField] private TMP_Text _resultText;
    [SerializeField] private RectTransform _cellRoot;
    [SerializeField] private Color _emptyCellColor = new(0f, 0f, 0f, 0.2f);

    [Header("Board")]
    [SerializeField] private int _boardWidth = 10;
    [SerializeField] private int _boardHeight = 20;
    [SerializeField] private float _fallInterval = 0.6f;
    [SerializeField] private float _softDropInterval = 0.05f;
    [SerializeField] private int _linesToComplete = 3;
    [SerializeField] private float _cellSpacing = 1f;

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
        new(0f, 0.9f, 0.9f, 1f),   // I
        new(1f, 0.92f, 0.2f, 1f),  // O
        new(0.75f, 0.3f, 0.9f, 1f),// T
        new(0.2f, 0.9f, 0.35f, 1f),// S
        new(0.95f, 0.2f, 0.2f, 1f),// Z
        new(0.2f, 0.4f, 0.95f, 1f),// J
        new(1f, 0.55f, 0.1f, 1f),  // L
    };

    private GridLayoutGroup _grid;
    private Image[,] _cells;
    private int[,] _board;

    private PieceType _currentType;
    private Vector2Int _currentPosition;
    private int _currentRotation;

    private float _fallTimer;
    private bool _softDrop;
    private bool _isGameOver;
    private bool _isInitialized;
    private int _linesCleared;

    public override void Init()
    {
        _boardWidth = Mathf.Max(4, _boardWidth);
        _boardHeight = Mathf.Max(4, _boardHeight);

        EnsureBoardRoot();
        ConfigureGrid();
        BuildCells();

        StartNewGame();
        _isInitialized = true;
    }

    void Update()
    {
        if (!_isInitialized || _isGameOver) return;

        HandleInput();
        StepFall(Time.deltaTime);
        Render();
    }

    private void EnsureBoardRoot()
    {
        if (_cellRoot == null)
        {
            var existing = transform.Find("Cells") as RectTransform;
            if (existing != null)
            {
                _cellRoot = existing;
            }
            else
            {
                var go = new GameObject("Cells", typeof(RectTransform));
                _cellRoot = go.GetComponent<RectTransform>();
                _cellRoot.SetParent(transform, false);
            }
        }

        _cellRoot.anchorMin = Vector2.zero;
        _cellRoot.anchorMax = Vector2.one;
        _cellRoot.offsetMin = Vector2.zero;
        _cellRoot.offsetMax = Vector2.zero;

        bool useRootGrid = _cellRoot == (RectTransform)transform;
        var rootGrid = GetComponent<GridLayoutGroup>();
        if (rootGrid != null && rootGrid.transform == transform)
            rootGrid.enabled = useRootGrid;

        _grid = _cellRoot.GetComponent<GridLayoutGroup>();
        if (_grid == null)
        {
            _grid = _cellRoot.gameObject.AddComponent<GridLayoutGroup>();
        }
        else
        {
            _grid.enabled = true;
        }
    }

    private void ConfigureGrid()
    {
        if (_grid == null) return;

        _grid.startCorner = GridLayoutGroup.Corner.LowerLeft;
        _grid.startAxis = GridLayoutGroup.Axis.Horizontal;
        _grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        _grid.constraintCount = Mathf.Max(1, _boardWidth);
        _grid.childAlignment = TextAnchor.LowerLeft;

        float spacing = Mathf.Max(0f, _cellSpacing);
        _grid.spacing = new Vector2(spacing, spacing);

        var rect = _cellRoot.rect;
        float width = rect.width > 0f ? rect.width : _boardWidth * 30f;
        float height = rect.height > 0f ? rect.height : _boardHeight * 30f;
        float cellSize = Mathf.Min(
            (width - (_boardWidth - 1) * spacing) / _boardWidth,
            (height - (_boardHeight - 1) * spacing) / _boardHeight
        );
        cellSize = Mathf.Max(6f, Mathf.Floor(cellSize));
        _grid.cellSize = new Vector2(cellSize, cellSize);
    }

    private void BuildCells()
    {
        if (_cells != null &&
            _cells.GetLength(0) == _boardWidth &&
            _cells.GetLength(1) == _boardHeight)
        {
            ClearCellColors();
            return;
        }

        if (_cellRoot == null) return;

        for (int i = _cellRoot.childCount - 1; i >= 0; i--)
        {
            var child = _cellRoot.GetChild(i);
            if (!child.name.StartsWith("Cell_")) continue;

            if (Application.isPlaying)
                Destroy(child.gameObject);
            else
                DestroyImmediate(child.gameObject);
        }

        _cells = new Image[_boardWidth, _boardHeight];
        for (int y = 0; y < _boardHeight; y++)
        {
            for (int x = 0; x < _boardWidth; x++)
            {
                _cells[x, y] = CreateCellImage(x, y);
            }
        }
    }

    private Image CreateCellImage(int x, int y)
    {
        var go = new GameObject($"Cell_{x}_{y}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(_cellRoot, false);

        var image = go.GetComponent<Image>();
        image.sprite = GetWhiteSprite();
        image.raycastTarget = false;
        image.color = _emptyCellColor;

        return image;
    }

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

    private void HandleInput()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        if (keyboard.leftArrowKey.wasPressedThisFrame)
            TryMove(Vector2Int.left);
        if (keyboard.rightArrowKey.wasPressedThisFrame)
            TryMove(Vector2Int.right);
        if (keyboard.upArrowKey.wasPressedThisFrame)
            TryRotate();
        if (keyboard.downArrowKey.wasPressedThisFrame)
            TryMove(Vector2Int.down);

        _softDrop = keyboard.downArrowKey.isPressed;
    }

    private void StepFall(float deltaTime)
    {
        float interval = _softDrop ? _softDropInterval : _fallInterval;
        _fallTimer += deltaTime;
        if (_fallTimer < interval) return;

        _fallTimer = 0f;
        if (!TryMove(Vector2Int.down))
            LockPiece();
    }

    private void SpawnPiece()
    {
        _currentType = (PieceType)Random.Range(0, 7);
        _currentRotation = 0;

        int spawnX = Mathf.Max(0, (_boardWidth - 4) / 2);
        int spawnY = Mathf.Max(0, _boardHeight - 4);
        _currentPosition = new Vector2Int(spawnX, spawnY);

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

        Vector2Int[] kicks =
        {
            new(-1, 0),
            new(1, 0),
            new(-2, 0),
            new(2, 0),
            new(0, 1)
        };

        for (int i = 0; i < kicks.Length; i++)
        {
            var kickedPos = _currentPosition + kicks[i];
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

            if (x < 0 || x >= _boardWidth) return false;
            if (y < 0 || y >= _boardHeight) return false;
            if (_board[x, y] != 0) return false;
        }

        return true;
    }

    private void LockPiece()
    {
        for (int i = 0; i < PieceCellCount; i++)
        {
            var cell = ShapeCache[(int)_currentType, _currentRotation, i];
            int x = _currentPosition.x + cell.x;
            int y = _currentPosition.y + cell.y;
            if (x < 0 || x >= _boardWidth || y < 0 || y >= _boardHeight)
                continue;

            _board[x, y] = (int)_currentType + 1;
        }

        int cleared = ClearFullLines();
        if (cleared > 0)
            _linesCleared += cleared;

        if (_linesCleared >= _linesToComplete)
        {
            _isGameOver = true;
            UpdateHud(true);
            CompleteMission();
            return;
        }

        SpawnPiece();
        UpdateHud();
    }

    private int ClearFullLines()
    {
        int cleared = 0;

        for (int y = 0; y < _boardHeight; y++)
        {
            bool full = true;
            for (int x = 0; x < _boardWidth; x++)
            {
                if (_board[x, y] == 0)
                {
                    full = false;
                    break;
                }
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

    private void Render()
    {
        if (_cells == null || _board == null) return;

        for (int y = 0; y < _boardHeight; y++)
        {
            for (int x = 0; x < _boardWidth; x++)
            {
                int cell = _board[x, y];
                _cells[x, y].color = cell == 0 ? _emptyCellColor : _pieceColors[cell - 1];
            }
        }

        if (_isGameOver) return;

        Color pieceColor = _pieceColors[(int)_currentType];
        for (int i = 0; i < PieceCellCount; i++)
        {
            var cell = ShapeCache[(int)_currentType, _currentRotation, i];
            int x = _currentPosition.x + cell.x;
            int y = _currentPosition.y + cell.y;
            if (x < 0 || x >= _boardWidth || y < 0 || y >= _boardHeight)
                continue;

            _cells[x, y].color = pieceColor;
        }
    }

    private void GameOver()
    {
        _isGameOver = true;
        UpdateHud();
    }

    private void UpdateHud(bool completed = false)
    {
        if (_resultText == null) return;

        if (completed)
        {
            _resultText.text = "미션 완료!";
            return;
        }

        if (_isGameOver)
        {
            _resultText.text = "Game Over";
            return;
        }

        _resultText.text = $"Lines: {_linesCleared}/{_linesToComplete}\n← → 이동 / ↑ 회전 / ↓ 내리기";
    }

    private void ClearCellColors()
    {
        if (_cells == null) return;

        for (int y = 0; y < _boardHeight; y++)
        {
            for (int x = 0; x < _boardWidth; x++)
                _cells[x, y].color = _emptyCellColor;
        }
    }

    private static Vector2Int RotateCW(Vector2Int p)
    {
        return new Vector2Int(3 - p.y, p.x);
    }

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
