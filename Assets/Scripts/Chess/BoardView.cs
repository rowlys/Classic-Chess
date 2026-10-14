using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class BoardView : MonoBehaviour
{
    // In PieceType order: Pawn, Knight, Bishop, Rook, Queen, King
    [SerializeField] Sprite[] whiteSprites = new Sprite[6];
    [SerializeField] Sprite[] blackSprites = new Sprite[6];

    // Plain boards: (1, 1) and (0, 0). Perspective boards: (1, 0.75) and (0, 0.1875)
    [SerializeField] Vector2 squareSize = Vector2.one;
    [SerializeField] Vector2 gridOffset;

    [SerializeField] float pickupHeight = 0.2f; // 4px
    [SerializeField] Color dotColor = new(0.1f, 0.1f, 0.1f, 0.45f);

    Board board;
    readonly Dictionary<Vector2Int, GameObject> pieceObjects = new();

    Vector2Int? selected;
    List<Move> selectedMoves = new();
    readonly List<GameObject> highlightObjects = new();
    Sprite dotSprite;

    void Start()
    {
        dotSprite = CreateDotSprite(3);

        board = new Board();
        board.SetupStartPosition();
        Redraw();
    }

    public Vector2 SquareToLocal(Vector2Int sq) => gridOffset + new Vector2(
        (sq.x - (board.Width - 1) / 2f) * squareSize.x,
        (sq.y - (board.Height - 1) / 2f) * squareSize.y);

    Vector2 PiecePosition(Vector2Int sq) => SquareToLocal(sq) + Vector2.up * (0.5f - squareSize.y / 2);

    int SortOrder(int rank) => 2 * (board.Height - rank);

    public void Redraw()
    {
        foreach (var go in pieceObjects.Values) Destroy(go);
        pieceObjects.Clear();

        for (int x = 0; x < board.Width; x++)
            for (int y = 0; y < board.Height; y++)
            {
                var sq = new Vector2Int(x, y);
                var piece = board[sq];
                if (piece.IsEmpty) continue;

                var go = new GameObject($"{(piece.IsWhite ? "W" : "B")}_{piece.Type} ({x},{y})");
                go.transform.SetParent(transform, false);
                go.transform.localPosition = PiecePosition(sq);

                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = (piece.IsWhite ? whiteSprites : blackSprites)[(int)piece.Type - 1];
                sr.sortingOrder = SortOrder(y);

                pieceObjects[sq] = go;
            }
    }

    void Update()
    {
        var pointer = Pointer.current;
        if (pointer == null || !pointer.press.wasPressedThisFrame) return;

        var sq = WorldToSquare(Camera.main.ScreenToWorldPoint(pointer.position.ReadValue()));
        if (board.InBounds(sq)) OnSquareClicked(sq);
        else Deselect();
    }

    public Vector2Int WorldToSquare(Vector3 world)
    {
        Vector2 local = (Vector2)transform.InverseTransformPoint(world) - gridOffset;
        return new Vector2Int(
            Mathf.FloorToInt(local.x / squareSize.x + board.Width / 2f),
            Mathf.FloorToInt(local.y / squareSize.y + board.Height / 2f));
    }

    void OnSquareClicked(Vector2Int sq)
    {
        if (sq == selected)
        {
            Deselect();
            return;
        }

        int i = selectedMoves.FindIndex(m => m.To == sq);
        if (i >= 0)
        {
            board.Apply(selectedMoves[i]);
            Redraw();
            Deselect();
            return;
        }

        var piece = board[sq];
        if (!piece.IsEmpty && piece.IsWhite == board.WhiteToMove)
        {
            selected = sq;
            selectedMoves = board.MovesFrom(sq);
            DrawHighlights();
        }
        else Deselect();
    }

    void Deselect()
    {
        selected = null;
        selectedMoves.Clear();
        DrawHighlights();
    }

    void DrawHighlights()
    {
        foreach (var go in highlightObjects) Destroy(go);
        highlightObjects.Clear();

        foreach (var (sq, go) in pieceObjects)
            go.transform.localPosition = PiecePosition(sq) + (sq == selected ? Vector2.up * pickupHeight : Vector2.zero);

        foreach (var m in selectedMoves) AddDot(m.To);
    }

    void AddDot(Vector2Int sq)
    {
        var go = new GameObject("Move Dot");
        go.transform.SetParent(transform, false);
        go.transform.localPosition = SquareToLocal(sq);
        go.transform.localScale = new Vector3(squareSize.x, squareSize.y, 1);

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = dotSprite;
        sr.color = dotColor;
        sr.sortingOrder = SortOrder(sq.y) + 1;

        highlightObjects.Add(go);
    }

    static Sprite CreateDotSprite(float radius)
    {
        var tex = new Texture2D(16, 16, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
        for (int x = 0; x < 16; x++)
            for (int y = 0; y < 16; y++)
            {
                float dx = x - 7.5f, dy = y - 7.5f;
                tex.SetPixel(x, y, dx * dx + dy * dy <= radius * radius ? Color.white : Color.clear);
            }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, 16, 16), new Vector2(0.5f, 0.5f), 16);
    }
}
