using System.Collections.Generic;
using System.Linq;

// Picks a move by looking a few turns ahead and counting material > minimax
public static class ChessAI
{
    // Value for each PieceType: None, Pawn, Knight, Bishop, Rook, Queen, King
    static readonly int[] Values = { 0, 100, 320, 330, 500, 900, 0 };
    const int Mate = 100000, Infinity = 1000000;
    static readonly System.Random rng = new();

    public static Move BestMove(Board board, int depth)
    {
        Move best = default;
        int alpha = -Infinity;
        
        // Shuffle moves so equally good moves vary from game to game
        foreach (var m in Ordered(board, board.AllMoves().OrderBy(_ => rng.Next()).ToList()))
        {
            var next = board.Clone();
            next.Apply(m);
            int score = -Search(next, depth - 1, -Infinity, -alpha);
            if (score > alpha) { alpha = score; best = m; }
        }
        return best;
    }

    // Score for the side to move: positive is good for them
    static int Search(Board board, int depth, int alpha, int beta)
    {
        if (depth == 0) return Evaluate(board);

        var moves = board.AllMoves();
        if (moves.Count == 0) return board.KingIsRoyal && board.IsInCheck(board.WhiteToMove) ? -Mate - depth : 0;

        foreach (var m in Ordered(board, moves))
        {
            var next = board.Clone();
            next.Apply(m);
            int score = -Search(next, depth - 1, -beta, -alpha);
            if (score >= beta) return beta; // the opponent won't allow this line
            if (score > alpha) alpha = score;
        }
        return alpha;
    }

    static int Evaluate(Board board)
    {
        int score = 0;
        foreach (var p in board.Squares)
            if (!p.IsEmpty) score += p.IsWhite == board.WhiteToMove ? Values[(int)p.Type] : -Values[(int)p.Type];
        return score;
    }

    // Biggest captures first, so pruning cuts more
    static IEnumerable<Move> Ordered(Board board, List<Move> moves) =>
        moves.OrderByDescending(m => board.IsCapture(m) ? Values[(int)board[m.To].Type] : -1);
}
