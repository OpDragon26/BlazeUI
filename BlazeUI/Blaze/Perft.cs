using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BlazeUI.Blaze;

public static class Perft
{
    private static ulong RunSingle(int depth, Board board, bool testDifference, bool multiThreaded, PerftTest? comparison, bool printResult = true)
    {
        Bitboards.Init();
        PerftResult Result = new(depth);
        Timer timer = new();
        timer.Start();

        board.considerRepetition = false;

        Move[] moves = Search.SearchBoard(board, false).ToArray();

        if (depth == 1)
            return (ulong)moves.Length;

        if (multiThreaded)
            Parallel.For(0, moves.Length, i =>
            {
                ulong[] threadResults = Result.GetNew();
                
                Board moveBoard = new Board(board);
                moveBoard.MakeMove(moves[i]);
                threadResults[depth]++;
                PerftSearch(moveBoard, depth - 1, threadResults, testDifference);
            });
        else
            foreach (Move move in moves)
            {
                ulong[] threadResults = Result.GetNew();
                
                Board moveBoard = new Board(board);
                moveBoard.MakeMove(move);
                threadResults[depth]++;
                PerftSearch(moveBoard, depth - 1, threadResults, testDifference);
            }

        if (!printResult) return Result.GetResult().Aggregate((a, b) => a + b);
        
        ulong[] perftResult = Result.GetResult();
        if (comparison != null)
            comparison.CompareTo(Result);
        else
            for (int i = perftResult.Length - 1; i > 0; i--)
                Console.WriteLine($"Depth {perftResult.Length - i}: {perftResult[i]}");
        Console.WriteLine(comparison != null ?
            $"Perft {comparison.name} completed at depth {depth} in {timer.Stop()}ms" : 
            $"Depth {depth} perft completed in {timer.Stop()}ms");
        return perftResult.Aggregate((a, b) => a + b);
    }

    private static void RunSingle(int depth, PerftTest test, bool testDifference, bool multiThreaded)
    {
        RunSingle(depth + test.depthOffset, test.board, testDifference, multiThreaded, test);
    }

    private static void PerftSearch(Board board, int depth, ulong[] results, bool testDifference = false)
    {
        if (testDifference)
        {
            if (depth == 0) return;
            
            int legalResult = 0;
            int pseudolegalResult = 0;

            Move[] pseudolegal = Search.FilterChecks(Search.PseudolegalSearchBoard(board), board);
            results[depth] += (uint)pseudolegal.Length;
            Move[] legal = Search.SearchBoard(board, false).ToArray();

            legalResult += legal.Length;
            pseudolegalResult += pseudolegal.Length;
            
            if (legalResult != pseudolegalResult) // mismatch
            {
                MismatchMutex.WaitOne();
                Console.WriteLine($"At depth {depth}");
                PrintMismatch(CompareResults(pseudolegal, legal.ToArray()), board);
                Environment.Exit(1);
            }
            
            foreach (Move move in pseudolegal)
            {
                Board MoveBoard = new Board(board);
                MoveBoard.MakeMove(move);
                PerftSearch(MoveBoard, depth - 1, results, true);
            }
        }
        else
        {
            if (depth == 1)
            {
                results[1] += (ulong)Search.SearchBoard(board, false).Length;
                return;
            }
            
            Span<Move> moves = Search.SearchBoard(board, false);

            foreach (Move move in moves)
            {
                Board MoveBoard = new Board(board);
                MoveBoard.MakeMove(move);
                
                results[depth]++;

                PerftSearch(MoveBoard, depth - 1, results);
            }
        }

    }

    public static void RunAll(int depth, bool multiThreaded = true, bool testDifference = false)
    {
        foreach (KeyValuePair<string, PerftTest> test in PerftTests)
        {
            RunSingle(depth, test.Value, testDifference, multiThreaded);
        }
    }

    public static void Run(int depth, string perft, bool multiThreaded = true, bool testDifference = false)
    {
        if (PerftTests.TryGetValue(perft, out var test))
            RunSingle(depth, test, testDifference, multiThreaded);
        else
            Console.WriteLine($"Perft {perft} not found");
        
    }

    private class PerftResult(int depth)
    {
        readonly List<ulong[]> Results = new();
        readonly Mutex mutex = new();

        public ulong[] GetNew()
        {
            ulong[] newArray = new ulong[depth + 1];
            mutex.WaitOne(); // locks the mutex for the duration of adding a new array
            Results.Add(newArray);
            mutex.ReleaseMutex();
            return newArray;
        }

        public ulong[] GetResult()
        {
            ulong[] final = new ulong[depth + 1];

            foreach (ulong[] threadResult in Results)
                for (int i = 0; i < threadResult.Length; i++)
                    final[i] += threadResult[i];

            return final;
        }
    }

    private class PerftTest(string name, Board board, ulong[] expected, int depthOffset)
    {
        public readonly Board board = board;
        public readonly int depthOffset = depthOffset;
        public readonly string name = name;
        public void CompareTo(PerftResult result)
        {
            ulong[] results = result.GetResult();
            
            for (int i = results.Length - 1; i > 0; i--)
            {
                if (results.Length - i > expected.Length) // out of bounds
                    Console.WriteLine($"Depth {results.Length - i}: {results[i]}");
                else // in bounds
                {
                    Console.WriteLine(results[i] == expected[results.Length - i] ? 
                        $"Depth {results.Length - i}: {results[i]} ✓" : // correct
                        $"Depth {results.Length - i}: {results[i]} ✕ - should be {expected[results.Length - i]}"); // incorrect
                }
            }
        }
    }

    public static void Breakdown(Board board, int depth)
    {
        Move[] moves = Search.SearchBoard(board, false).ToArray();
        Array.Sort(moves, (a, b) => a.GetUCI().CompareTo(b.GetUCI()));
        ulong total = 0;

        foreach (Move move in moves)
        {
            Board moveBoard = new Board(board);
            moveBoard.MakeMove(move);
            ulong perftResult = RunSingle(depth - 1, moveBoard, false, depth > 3, null, false);
            Console.WriteLine($"{move.GetUCI()}: {perftResult}");
            total += perftResult;
        }
        
        Console.WriteLine($"Nodes Searched: {total}");
    }

    public static void BreakdownEval(Board board, int depth)
    {
        Move[] moves = Search.SearchBoard(board, false).ToArray();
        Array.Sort(moves, (a, b) => a.GetUCI().CompareTo(b.GetUCI()));

        foreach (Move move in moves)
        {
            Board moveBoard = new Board(board);
            moveBoard.MakeMove(move);
            Console.WriteLine($"{move.GetUCI()}: {Search.Minimax(moveBoard, depth - 1, int.MinValue, int.MaxValue)}");
        }
    }

    public static void BreakdownWithExamine(Board board, int depth, int[] examination)
    {
        foreach (int examine in examination)
        {
            depth--;
            Move[] initMoves = Search.SearchBoard(board, false).ToArray();
            Array.Sort(initMoves, (a, b) => a.GetUCI().CompareTo(b.GetUCI()));
            board.MakeMove(initMoves[examine]);
            Console.Write($"{initMoves[examine].GetUCI()}, ");
        }
        Console.WriteLine();

        if (depth < 1)
            Console.WriteLine("Depth too low");
        if (depth == 1)
        {
            Move[] moves = Search.SearchBoard(board, false).ToArray();
            Array.Sort(moves, (a, b) => a.GetUCI().CompareTo(b.GetUCI()));
            
            foreach (Move move in moves)
                Console.WriteLine($"{move.GetUCI()}");
            
            Console.WriteLine($"Found: {moves.Length}");
        }
        else
        {
            Move[] moves = Search.SearchBoard(board, false).ToArray();
            
            Array.Sort(moves, (a, b) => a.GetUCI().CompareTo(b.GetUCI()));
            ulong total = 0;

            foreach (Move move in moves)
            {
                Board moveBoard = new Board(board);
                moveBoard.MakeMove(move);
                ulong perftResult = RunSingle(depth - 1, moveBoard, false, depth > 3, null, false);
                Console.WriteLine($"{move.GetUCI()}: {perftResult}");
                total += perftResult;
            }

            Console.WriteLine($"Nodes Searched: {total}");
        }
        
    }
    
    static readonly PerftTest StartingPositionTest = new("Starting Position", new(Presets.StartingBoard), 
        [
            1,
            20,
            400,
            8_902,
            197_281,
            4_865_609,
            119_060_324,
            3_195_901_860,
            84_998_978_956
        ], 0);
    
    static readonly PerftTest KiwipeteTest = new("Kiwipete", new("r3k2r/p1ppqpb1/bn2pnp1/3PN3/1p2P3/2N2Q1p/PPPBBPPP/R3K2R w KQkq - 0 0"), 
    [
        1,
        48,
        2_039,
        97_862,
        4_085_603,
        193_690_690,
        8_031_647_685,
    ], -1);

    private static readonly Dictionary<string, PerftTest> PerftTests = new()
    {
        { "start", StartingPositionTest },
        { "kiwipete", KiwipeteTest },
    };
    
    public static void TestGameSpeed(int games, int depth)
    {
        List<PGNNode> allMoves = new();
        for (int i = 0; i < games; i++)
        {
            List<PGNNode> game = Match.RandomGame(depth);
            Console.WriteLine(CLIMatch.GetPGN(game));
            allMoves.AddRange(game);
            Console.WriteLine($"Game {i + 1}/{games}");
        }

        Console.WriteLine($"Average time per move: {allMoves.Where(node => node.time > 50).Average(node => node.time)}ms");
    }
    
    public static void AnalyzeBoard(Board board)
    {
        Move[] pseudolegal = Search.FilterChecks(Search.PseudolegalSearchBoard(board), board);
        Move[] legal = Search.SearchBoard(board, false).ToArray();

        if (pseudolegal.Length != legal.Length)
            PrintMismatch(CompareResults(pseudolegal,  legal), board);
        else
        {
            Console.WriteLine("Correct moves found:");
            foreach (Move move in legal)
                Console.WriteLine(move.Notate(board));
        }
    }

    private static MismatchedMove[] CompareResults(Move[] pseudolegal, Move[] legal)
    {
        List<MismatchedMove> mismatchedList = new();

        foreach (Move move in pseudolegal)
            if (!legal.Contains(move)) // move present in pseudolegal but not in legal -> missing move
                mismatchedList.Add(new MismatchedMove(move, Mismatch.Missing));
        foreach (Move move in legal)
            if (!pseudolegal.Contains(move)) // move present in legal but not pseudolegal -> extra move
                mismatchedList.Add(new MismatchedMove(move, Mismatch.Extra));

        return mismatchedList.ToArray();
    }

    private static readonly Mutex MismatchMutex = new();
    private static void PrintMismatch(MismatchedMove[] moves, Board board)
    {
        Console.WriteLine("Board:");
        CLIMatch.PrintBoard(board);
        Console.WriteLine();

        foreach (MismatchedMove move in moves)
            Console.WriteLine(move);
    }
    
    private readonly struct MismatchedMove(Move move, Mismatch mismatch)
    {
        public override string ToString()
        {
            return $"{mismatch.ToString()} move: {move.GetUCI()}";
        }
    }

    enum Mismatch
    {
        Extra,
        Missing
    }
}