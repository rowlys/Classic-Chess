using System.Collections.Generic;
using System.Linq;
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

    // Every legal move for the side to move
    public List<Move> AllMoves()
    {
        var moves = new List<Move>();
        for (int x = 0; x < Width; x++)
            for (int y = 0; y < Height; y++)
            {
                var sq = new Vector2Int(x, y);
                if (!this[sq].IsEmpty && this[sq].IsWhite == WhiteToMove) moves.AddRange(MovesFrom(sq));
            }
        return moves;
    }

    // Judged for the side to move: only they can have just lost a piece
    public GameResult Result()
    {
        var loss = WhiteToMove ? GameResult.BlackWins : GameResult.WhiteWins;

        if (!KingIsRoyal && !HasPieces(WhiteToMove)) return loss; // roguelike: lose when every piece is gone
        if (AllMoves().Count > 0) return HalfmoveClock >= 100 || InsufficientMaterial() ? GameResult.Draw : GameResult.Ongoing;
        return IsInCheck(WhiteToMove) ? loss : GameResult.Draw;   // no moves: checkmate if in check, otherwise stalemate
    }

    bool HasPieces(bool white) => Squares.Cast<Piece>().Any(p => !p.IsEmpty && p.IsWhite == white);

    // Kings alone, or with one knight or bishop, can't force mate. Roguelike kings can still capture each other
    public bool InsufficientMaterial()
    {
        if (!KingIsRoyal) return false;
        var rest = Squares.Cast<Piece>().Where(p => !p.IsEmpty && p.Type != PieceType.King).ToList();
        return rest.Count == 0 || (rest.Count == 1 && rest[0].Type is PieceType.Knight or PieceType.Bishop);
    }

    public bool IsInCheck(bool white) => FindKing(white) is Vector2Int king && IsAttacked(king, byWhite: !white);

    // Null if that side has no king (custom setups): counts as never in check
    Vector2Int? FindKing(bool white)
    {
        for (int x = 0; x < Width; x++)
            for (int y = 0; y < Height; y++)
                if (Squares[x, y].Type == PieceType.King && Squares[x, y].IsWhite == white) return new Vector2Int(x, y);
        return null;
    }

    // Looks outward from sq as each piece type, instead of generating every enemy move
    public bool IsAttacked(Vector2Int sq, bool byWhite)
    {
        bool Has(Vector2Int s, PieceType type) => InBounds(s) && this[s].Type == type && this[s].IsWhite == byWhite;

        foreach (var d in KnightJumps) if (Has(sq + d, PieceType.Knight)) return true;
        foreach (var d in AllDirections) if (Has(sq + d, PieceType.King)) return true;

        // An attacking white pawn sits one rank below sq, a black one above
        int pawnRank = byWhite ? -1 : 1;
        if (Has(sq + new Vector2Int(-1, pawnRank), PieceType.Pawn) || Has(sq + new Vector2Int(1, pawnRank), PieceType.Pawn)) return true;

        foreach (var d in Straight) if (FirstPieceIs(sq, d, byWhite, PieceType.Rook, PieceType.Queen)) return true;
        foreach (var d in Diagonal) if (FirstPieceIs(sq, d, byWhite, PieceType.Bishop, PieceType.Queen)) return true;
        return false;
    }

    // Sliding from sq along d, is the first piece hit this colour and type a or b?
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

    // Knight and king: one jump per offset onto an empty or enemy square
    void AddSteps(Vector2Int from, Vector2Int[] offsets, List<Move> moves)
    {
        bool isWhite = this[from].IsWhite;
        foreach (var d in offsets)
        {
            var to = from + d;
            if (InBounds(to) && (this[to].IsEmpty || IsEnemy(to, isWhite))) moves.Add(new Move(from, to));
        }
    }

    // Bishop, rook, queen: slide until blocked; capture an enemy, stop before a friend
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

    // Needs unmoved king and rook, an empty path, and a safe start and crossed square; MovesFrom checks the landing
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

        // Forward: empty squares only, never a capture
        var one = from + forward;
        if (InBounds(one) && this[one].IsEmpty)
        {
            AddPawnMove(from, one, moves);

            // Double step if unmoved; uses HasMoved, not a start rank, so any board size works
            var two = one + forward;
            if (!this[from].HasMoved && InBounds(two) && this[two].IsEmpty) moves.Add(new Move(from, two));
        }

        // Diagonal: captures, or en passant onto the square the enemy pawn skipped
        for (int dx = -1; dx <= 1; dx += 2)
        {
            var to = one + new Vector2Int(dx, 0);
            if (!InBounds(to)) continue;
            bool enPassant = to == EnPassantTarget && isWhite == WhiteToMove;
            if (IsEnemy(to, isWhite) || enPassant) AddPawnMove(from, to, moves);
        }
    }

    // Far rank promotes; queen is listed first, so a click picks it until there's a picker
    void AddPawnMove(Vector2Int from, Vector2Int to, List<Move> moves)
    {
        bool lastRank = to.y == (this[from].IsWhite ? Height - 1 : 0);
        if (!lastRank) { moves.Add(new Move(from, to)); return; }
        foreach (var type in Promotions) moves.Add(new Move(from, to, type));
    }
}
