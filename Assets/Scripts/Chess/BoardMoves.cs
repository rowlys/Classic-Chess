using System.Collections.Generic;
using UnityEngine;

public partial class Board
{
    static readonly Vector2Int[] Straight = { new(1, 0), new(-1, 0), new(0, 1), new(0, -1) };
    static readonly Vector2Int[] Diagonal = { new(1, 1), new(1, -1), new(-1, 1), new(-1, -1) };
    static readonly Vector2Int[] AllDirections =
        { new(1, 0), new(-1, 0), new(0, 1), new(0, -1), new(1, 1), new(1, -1), new(-1, 1), new(-1, -1) };
    static readonly Vector2Int[] KnightJumps =
        { new(1, 2), new(2, 1), new(2, -1), new(1, -2), new(-1, -2), new(-2, -1), new(-2, 1), new(-1, 2) };

    // Legal moves: the piece's moves minus any that leave your own king attacked
    public List<Move> MovesFrom(Vector2Int from)
    {
        var moves = PseudoMovesFrom(from);
        if (!KingIsRoyal) return moves;

        bool isWhite = this[from].IsWhite;
        moves.RemoveAll(m =>
        {
            var after = Clone();
            after.Apply(m);
            return after.IsInCheck(isWhite);
        });
        return moves;
    }

    public bool IsInCheck(bool white) => FindKing(white) is Vector2Int king && IsAttacked(king, byWhite: !white);

    // Null if that side has no king (possible in custom setups), which counts as never in check
    Vector2Int? FindKing(bool white)
    {
        for (int x = 0; x < Width; x++)
            for (int y = 0; y < Height; y++)
                if (Squares[x, y].Type == PieceType.King && Squares[x, y].IsWhite == white) return new Vector2Int(x, y);
        return null;
    }

    // Is `sq` attacked by the given colour? Looks outward from the square as each piece type, instead of generating every enemy move.
    public bool IsAttacked(Vector2Int sq, bool byWhite)
    {
        bool Has(Vector2Int s, PieceType type) => InBounds(s) && this[s].Type == type && this[s].IsWhite == byWhite;

        foreach (var d in KnightJumps) if (Has(sq + d, PieceType.Knight)) return true;
        foreach (var d in AllDirections) if (Has(sq + d, PieceType.King)) return true;

        // A white pawn attacks diagonally upward, so it would sit one rank *below* sq (and the opposite for black)
        int pawnRank = byWhite ? -1 : 1;
        if (Has(sq + new Vector2Int(-1, pawnRank), PieceType.Pawn) || Has(sq + new Vector2Int(1, pawnRank), PieceType.Pawn)) return true;

        foreach (var d in Straight) if (FirstPieceIs(sq, d, byWhite, PieceType.Rook, PieceType.Queen)) return true;
        foreach (var d in Diagonal) if (FirstPieceIs(sq, d, byWhite, PieceType.Bishop, PieceType.Queen)) return true;
        return false;
    }

    // Slide from sq in direction d: is the first piece hit one of these two types, of this colour?
    bool FirstPieceIs(Vector2Int sq, Vector2Int d, bool white, PieceType a, PieceType b)
    {
        for (var s = sq + d; InBounds(s); s += d)
        {
            var p = this[s];
            if (p.IsEmpty) continue;
            return p.IsWhite == white && (p.Type == a || p.Type == b);
        }
        return false;
    }

    // How the piece on `from` moves, ignoring check
    List<Move> PseudoMovesFrom(Vector2Int from)
    {
        var moves = new List<Move>();
        switch (this[from].Type)
        {
            case PieceType.Pawn:   AddPawnMoves(from, moves); break;
            case PieceType.Knight: AddSteps(from, KnightJumps, moves); break;
            case PieceType.King:   AddSteps(from, AllDirections, moves); if (KingIsRoyal) AddCastling(from, moves); break;
            case PieceType.Bishop: AddSlides(from, Diagonal, moves); break;
            case PieceType.Rook:   AddSlides(from, Straight, moves); break;
            case PieceType.Queen:  AddSlides(from, AllDirections, moves); break;
        }
        return moves;
    }

    bool IsEnemy(Vector2Int sq, bool isWhite) => !this[sq].IsEmpty && this[sq].IsWhite != isWhite;

    // Knight and king: one jump per offset, landing on an empty square or an enemy
    void AddSteps(Vector2Int from, Vector2Int[] offsets, List<Move> moves)
    {
        bool isWhite = this[from].IsWhite;
        foreach (var d in offsets)
        {
            var to = from + d;
            if (InBounds(to) && (this[to].IsEmpty || IsEnemy(to, isWhite))) moves.Add(new Move(from, to));
        }
    }

    // Bishop, rook and queen: keep going until the edge or a piece; an enemy can be captured, a friend blocks
    void AddSlides(Vector2Int from, Vector2Int[] directions, List<Move> moves)
    {
        bool isWhite = this[from].IsWhite;
        foreach (var d in directions)
            for (var to = from + d; InBounds(to); to += d)
            {
                if (this[to].IsEmpty) { moves.Add(new Move(from, to)); continue; }
                if (IsEnemy(to, isWhite)) moves.Add(new Move(from, to));
                break;
            }
    }

    // King and rook both unmoved, nothing between them, and the king isn't in check and doesn't cross an attacked square.
    // Landing on an attacked square is filtered by MovesFrom like any other king move.
    void AddCastling(Vector2Int from, List<Move> moves)
    {
        var king = this[from];
        if (king.HasMoved || IsAttacked(from, !king.IsWhite)) return;

        foreach (int rookX in new[] { 0, Width - 1 })
        {
            var rookSq = new Vector2Int(rookX, from.y);
            var rook = this[rookSq];
            if (rook.Type != PieceType.Rook || rook.IsWhite != king.IsWhite || rook.HasMoved) continue;
            if (Mathf.Abs(rookX - from.x) < 3) continue; // the king needs two squares to move into

            var dir = new Vector2Int(rookX > from.x ? 1 : -1, 0);
            bool clear = true;
            for (var s = from + dir; s != rookSq; s += dir)
                if (!this[s].IsEmpty) { clear = false; break; }

            if (clear && !IsAttacked(from + dir, !king.IsWhite)) moves.Add(new Move(from, from + dir * 2));
        }
    }

    static readonly PieceType[] Promotions = { PieceType.Queen, PieceType.Rook, PieceType.Bishop, PieceType.Knight };

    void AddPawnMoves(Vector2Int from, List<Move> moves)
    {
        bool isWhite = this[from].IsWhite;
        var forward = isWhite ? Vector2Int.up : Vector2Int.down;

        // Straight ahead: only onto empty squares, never a capture
        var one = from + forward;
        if (InBounds(one) && this[one].IsEmpty)
        {
            AddPawnMove(from, one, moves);

            // Double step on the pawn's first move. Uses HasMoved, not a fixed start rank, so it works on any board size.
            var two = one + forward;
            if (!this[from].HasMoved && InBounds(two) && this[two].IsEmpty) moves.Add(new Move(from, two));
        }

        // Diagonally ahead: captures, or en passant onto the square the enemy pawn just skipped
        for (int dx = -1; dx <= 1; dx += 2)
        {
            var to = one + new Vector2Int(dx, 0);
            if (!InBounds(to)) continue;
            bool enPassant = to == EnPassantTarget && isWhite == WhiteToMove;
            if (IsEnemy(to, isWhite) || enPassant) AddPawnMove(from, to, moves);
        }
    }

    // Reaching the far rank promotes. All four choices are listed with the queen first, so a click picks the queen until there's a picker.
    void AddPawnMove(Vector2Int from, Vector2Int to, List<Move> moves)
    {
        bool lastRank = to.y == (this[from].IsWhite ? Height - 1 : 0);
        if (!lastRank) { moves.Add(new Move(from, to)); return; }
        foreach (var type in Promotions) moves.Add(new Move(from, to, type));
    }
}
