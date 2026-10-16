using System.Collections.Generic;
using UnityEngine;

public enum PieceType { None, Pawn, Knight, Bishop, Rook, Queen, King }

public enum GameResult { Ongoing, WhiteWins, BlackWins, Draw }

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

    // False in the roguelike: the king is just a capturable piece
    public bool KingIsRoyal = true;

    public Board(int width = 8, int height = 8) => Squares = new Piece[width, height];

    public Piece this[Vector2Int sq] { get => Squares[sq.x, sq.y]; set => Squares[sq.x, sq.y] = value; }
    public bool InBounds(Vector2Int sq) => sq.x >= 0 && sq.x < Width && sq.y >= 0 && sq.y < Height;

    public const string StartFen = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";

    static readonly Dictionary<char, PieceType> FenTypes = new()
    {
        ['p'] = PieceType.Pawn, ['n'] = PieceType.Knight, ['b'] = PieceType.Bishop,
        ['r'] = PieceType.Rook, ['q'] = PieceType.Queen, ['k'] = PieceType.King,
    };

    public void SetupStartPosition() => LoadFen(StartFen);

    // FEN: standard one-line position string; the board takes its size from it, so mini boards work too
    // shortcut: single-digit gaps only (max 9 wide); parse multi-digit numbers to go wider
    public void LoadFen(string fen)
    {
        var fields = fen.Split(' ');
        var rows = fields[0].Split('/');

        int width = 0;
        foreach (char c in rows[0]) width += char.IsDigit(c) ? c - '0' : 1;
        Squares = new Piece[width, rows.Length];

        for (int r = 0; r < rows.Length; r++)
        {
            int y = rows.Length - 1 - r, x = 0;
            foreach (char c in rows[r])
            {
                if (char.IsDigit(c)) { x += c - '0'; continue; }
                Squares[x++, y] = new Piece(FenTypes[char.ToLower(c)], char.IsUpper(c)) { HasMoved = true };
            }
        }

        // FEN has no "has moved" flag: infer it from pawn start ranks and the castling field ("KQkq")
        for (int x = 0; x < Width; x++)
            for (int y = 0; y < Height; y++)
                if (Squares[x, y].Type == PieceType.Pawn && y == (Squares[x, y].IsWhite ? 1 : Height - 2))
                    Squares[x, y].HasMoved = false;

        foreach (char c in fields.Length > 2 ? fields[2] : "-")
        {
            if (c == '-') continue;
            bool white = char.IsUpper(c);
            int y = white ? 0 : Height - 1;
            Squares[char.ToLower(c) == 'k' ? Width - 1 : 0, y].HasMoved = false;
            for (int x = 0; x < Width; x++)
                if (Squares[x, y].Type == PieceType.King && Squares[x, y].IsWhite == white) Squares[x, y].HasMoved = false;
        }

        WhiteToMove = fields.Length < 2 || fields[1] == "w";
        EnPassantTarget = fields.Length > 3 && fields[3] != "-"
            ? new Vector2Int(fields[3][0] - 'a', fields[3][1] - '1')
            : (Vector2Int?)null;
    }

    public Board Clone()
    {
        var b = (Board)MemberwiseClone();
        b.Squares = (Piece[,])Squares.Clone();
        return b;
    }

    // Includes en passant, where the captured pawn isn't on the target square
    public bool IsCapture(Move m) => !this[m.To].IsEmpty || (this[m.From].Type == PieceType.Pawn && m.From.x != m.To.x);

    public void Apply(Move m)
    {
        var piece = this[m.From];
        var delta = m.To - m.From;

        // En passant: a diagonal pawn move onto an empty square captures the pawn beside it
        if (piece.Type == PieceType.Pawn && delta.x != 0 && this[m.To].IsEmpty)
            this[new Vector2Int(m.To.x, m.From.y)] = default;

        // Castling: the king moves two squares, the rook lands on the square it crossed
        if (piece.Type == PieceType.King && Mathf.Abs(delta.x) == 2)
        {
            var rookFrom = new Vector2Int(delta.x > 0 ? Width - 1 : 0, m.From.y);
            var rook = this[rookFrom];
            rook.HasMoved = true;
            this[m.From + new Vector2Int(delta.x / 2, 0)] = rook;
            this[rookFrom] = default;
        }

        // After a double step, remember the skipped square for one turn (en passant)
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