using System.Diagnostics;
using UnityEditor;
using Debug = UnityEngine.Debug;

// Counts every possible move sequence and compares with the known totals. A mismatch means a rule is wrong somewhere.
public static class PerftCheck
{
    // Standard test positions and their known counts per depth (1, 2, 3...), from chessprogramming.org/Perft_Results
    static readonly (string name, string fen, long[] expected)[] Cases =
    {
        ("Start position", Board.StartFen, new long[] { 20, 400, 8902, 197281 }),
        ("Kiwipete (castling, pins, promotions)", "r3k2r/p1ppqpb1/bn2pnp1/3PN3/1p2P3/2N2Q1p/PPPBBPPP/R3K2R w KQkq - 0 1", new long[] { 48, 2039, 97862 }),
        ("Position 3 (en passant, checks)", "8/2p5/3p4/KP5r/1R3p1k/8/4P1P1/8 w - - 0 1", new long[] { 14, 191, 2812, 43238 }),
        ("Position 4 (promotions, castling rights)", "r3k2r/Pppp1ppp/1b3nbN/nP6/BBP1P3/q4N2/Pp1P2PP/R2Q1RK1 w kq - 0 1", new long[] { 6, 264, 9467 }),
    };

    [MenuItem("Chess/Run Perft Check")]
    static void Run()
    {
        bool allOk = true;
        foreach (var (name, fen, expected) in Cases)
        {
            var board = new Board();
            board.LoadFen(fen);
            for (int depth = 1; depth <= expected.Length; depth++)
            {
                var timer = Stopwatch.StartNew();
                long count = Perft(board, depth);
                bool ok = count == expected[depth - 1];
                allOk &= ok;
                string line = $"{name}, depth {depth}: {count} {(ok ? "OK" : $"WRONG, expected {expected[depth - 1]}")} ({timer.ElapsedMilliseconds} ms)";
                if (ok) Debug.Log(line); else Debug.LogError(line);
            }
        }
        if (allOk) Debug.Log("Perft: all rules correct");
    }

    static long Perft(Board board, int depth)
    {
        var moves = board.AllMoves();
        if (depth == 1) return moves.Count; // no need to play out the last move just to count it

        long total = 0;
        foreach (var m in moves)
        {
            var next = board.Clone();
            next.Apply(m);
            total += Perft(next, depth - 1);
        }
        return total;
    }
}
