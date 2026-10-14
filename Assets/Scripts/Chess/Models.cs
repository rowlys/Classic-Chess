using System.Collections.Generic;
using UnityEngine;

public enum PieceType { None, Pawn, Knight, Bishop, Rook, Queen, King }

public struct Piece
{
    public PieceType Type;
    public bool IsWhite;
    public bool HasMoved;

    public Piece(PieceType type, bool isWhite) { Type = type; IsWhite = isWhite; HasMoved = false; }
    public bool IsEmpty => Type == PieceType.None;
}

public struct Move
{
    public Vector2Int From, To;
    public PieceType Promotion;

    public Move(Vector2Int from, Vector2Int to, PieceType promotion = PieceType.None)
    { From = from; To = to; Promotion = promotion; }
}

public class Board
{
    public Piece[,] Squares; // [file, rank]: (0,0) = a1, (7,7) = h8
    public int Width => Squares.GetLength(0);
    public int Height => Squares.GetLength(1);

    public bool WhiteToMove = true;
    public Vector2Int? EnPassantTarget;

    public Board(int width = 8, int height = 8) => Squares = new Piece[width, height];

    public Piece this[Vector2Int sq] { get => Squares[sq.x, sq.y]; set => Squares[sq.x, sq.y] = value; }
    public bool InBounds(Vector2Int sq) => sq.x >= 0 && sq.x < Width && sq.y >= 0 && sq.y < Height;

    static readonly PieceType[] BackRank =
    {
    PieceType.Rook, PieceType.Knight, PieceType.Bishop, PieceType.Queen,
        PieceType.King, PieceType.Bishop, PieceType.Knight, PieceType.Rook
    };

    public void SetupStartPosition()
    {
        Squares = new Piece[8, 8];
        for (int x = 0; x < 8; x++)
        {
            Squares[x, 0] = new Piece(BackRank[x], true);
            Squares[x, 1] = new Piece(PieceType.Pawn, true);
            Squares[x, 6] = new Piece(PieceType.Pawn, false);
            Squares[x, 7] = new Piece(BackRank[x], false);
        }
        WhiteToMove = true;
        EnPassantTarget = null;
    }

    public Board Clone()
    {
        var b = (Board)MemberwiseClone();
        b.Squares = (Piece[,])Squares.Clone();
        return b;
    }

    public void Apply(Move m)
    {
        var piece = this[m.From];
        piece.HasMoved = true;
        this[m.To] = piece;     
        this[m.From] = default; 
        WhiteToMove = !WhiteToMove;
    }

    // Temporary: every square without a friendly piece. Step 5 replaces the body with the real rules.
    public List<Move> MovesFrom(Vector2Int from)
    {
        var moves = new List<Move>();
        for (int x = 0; x < Width; x++)
            for (int y = 0; y < Height; y++)
            {
                var to = new Vector2Int(x, y);
                if (this[to].IsEmpty || this[to].IsWhite != this[from].IsWhite) moves.Add(new Move(from, to));
            }
        return moves;
    }
}