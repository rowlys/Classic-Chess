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

public partial class Board
{
    public Piece[,] Squares; // [file, rank]: (0,0) = a1, (7,7) = h8
    public int Width => Squares.GetLength(0);
    public int Height => Squares.GetLength(1);

    public bool WhiteToMove = true;
    public Vector2Int? EnPassantTarget;

    // Classic chess: true. The roguelike turns it off so the king is just a piece that can be captured.
    public bool KingIsRoyal = true;

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
        var delta = m.To - m.From;

        // En passant: a pawn moving diagonally onto an empty square captures the pawn beside it
        if (piece.Type == PieceType.Pawn && delta.x != 0 && this[m.To].IsEmpty)
            this[new Vector2Int(m.To.x, m.From.y)] = default;

        // Castling: the king moves two squares, the rook jumps to the square the king crossed
        if (piece.Type == PieceType.King && Mathf.Abs(delta.x) == 2)
        {
            var rookFrom = new Vector2Int(delta.x > 0 ? Width - 1 : 0, m.From.y);
            var rook = this[rookFrom];
            rook.HasMoved = true;
            this[m.From + new Vector2Int(delta.x / 2, 0)] = rook;
            this[rookFrom] = default;
        }

        // After a double step, remember the skipped square for one turn so it can be captured en passant
        EnPassantTarget = piece.Type == PieceType.Pawn && Mathf.Abs(delta.y) == 2
            ? m.From + new Vector2Int(0, delta.y / 2)
            : (Vector2Int?)null;

        if (m.Promotion != PieceType.None) piece.Type = m.Promotion;
        piece.HasMoved = true;
        this[m.To] = piece;
        this[m.From] = default;
        WhiteToMove = !WhiteToMove;
    }
}