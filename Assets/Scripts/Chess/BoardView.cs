using System.Collections.Generic;
using UnityEngine;

public class BoardView : MonoBehaviour
{
    // In PieceType order: Pawn, Knight, Bishop, Rook, Queen, King
    [SerializeField] Sprite[] whiteSprites = new Sprite[6];
    [SerializeField] Sprite[] blackSprites = new Sprite[6];

    // Plain boards: (1, 1) and (0, 0). Perspective boards: (1, 0.75) and (0, 0.1875)
    [SerializeField] Vector2 squareSize = Vector2.one;
    [SerializeField] Vector2 gridOffset;

    Board board;
    readonly List<GameObject> pieceObjects = new();

    void Start()
    {
        board = new Board();
        board.SetupStartPosition();
        Redraw();
    }

    public Vector2 SquareToLocal(Vector2Int sq) => gridOffset + new Vector2(
        (sq.x - (board.Width - 1) / 2f) * squareSize.x,
        (sq.y - (board.Height - 1) / 2f) * squareSize.y);

    public void Redraw()
    {
        foreach (var go in pieceObjects) Destroy(go);
        pieceObjects.Clear();

        for (int x = 0; x < board.Width; x++)
            for (int y = 0; y < board.Height; y++)
            {
                var piece = board.Squares[x, y];
                if (piece.IsEmpty) continue;

                var go = new GameObject($"{(piece.IsWhite ? "W" : "B")}_{piece.Type} ({x},{y})");
                go.transform.SetParent(transform, false);
                go.transform.localPosition = SquareToLocal(new Vector2Int(x, y)) + Vector2.up * (0.5f - squareSize.y / 2);

                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = (piece.IsWhite ? whiteSprites : blackSprites)[(int)piece.Type - 1];
                sr.sortingOrder = board.Height - y;

                pieceObjects.Add(go);
            }
    }
}