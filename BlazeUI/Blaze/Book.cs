using System;
using System.Collections.Generic;
using System.Threading;

namespace BlazeUI.Blaze;

public static class Book
{
    private static readonly List<Layer> book = new();
    private static bool init;
    private static readonly Random random = new();

    public static void Init(string[] origin)
    {
        if (init) return;
        init = true;
        
        foreach (string line in origin)
        {
            //Console.WriteLine(line);
            AddLine(Parser.ParseUCI(line));
        }
    }
    
    public static bool TryRetrieve(Board board, int depth, out Move? move)
    {
        move = null;
        if (depth >= book.Count)
            return false;

        return book[depth].TryRetrieve(board, out move);
    }
    
    private static void AddLine(PGNNode[] line)
    {
        for (int depth = 0; depth < line.Length; depth++)
        {
            Board board = depth == 0 ? new Board(Presets.StartingBoard) : line[depth - 1].board;
            Move move = line[depth].move;
            AddToLayer(board, move, depth);
        }
    }

    private static void AddToLayer(Board board, Move move, int depth)
    {
        if (depth == book.Count)
            book.Add(new());
        book[depth].AddEntry(board, move);
    }

    private class Layer
    {
        readonly List<Entry> entries = new();

        public void AddEntry(Board board, Move move)
        {
            Entry? entry = entries.Find(e => e.board.Equals(board));
            if (entry == null)
                entries.Add(new Entry(board, move));
            else
                entry.moves.Add(move);
        }

        public bool TryRetrieve(Board board, out Move? move)
        {
            Entry? entry = entries.Find(e => e.board.Equals(board));
            move = entry?.moves[random.Next(entry.moves.Count)];
            return entry != null;
        }
    }
    
    private class Entry(Board board, Move move)
    {
        public readonly Board board = board;
        public readonly List<Move> moves = [move];
    }
}

public static class Parser
{
    public static void PrintGame(PGNNode[] game, int perspective, int pause = 10)
    {
        foreach (PGNNode node in game)
        {
            Console.Clear();
            CLIMatch.PrintBoard(node.board, perspective);
            Thread.Sleep(pause * 100);
        }
    }

    public static PGNNode[] ParsePGN(string pgn)
    {
        List<PGNNode> nodes = new List<PGNNode>();
        string[] game = pgn.Replace("\n", " ").Split(' ');
        Board board = new Board(Presets.StartingBoard);

        foreach (string alg in game)
        {
            if (alg.Equals(string.Empty) || alg[^1] == '.' || alg.Equals("0-1") || alg.Equals("1-0") || alg.Equals("1/2-1/2")) // notates the index of the move, or end of game
                continue;
            Move move;
            try
            {
                move = Move.Parse(alg, board); // converts the move from algebraic notation to Move
            }
            catch
            {
                Console.WriteLine(alg);
                throw;
            }

            board.MakeMove(move);

            nodes.Add(new PGNNode { board = new Board(board), move = move });
        }

        return nodes.ToArray();
    }

    public static string ToUCI(PGNNode[] game)
    {
        List<string> UCI = [];
        foreach (PGNNode node in game)
            UCI.Add(node.move.GetUCI());
        
        return string.Join(' ', UCI);
    }

    public static PGNNode[] ParseUCI(string pgn)
    {
        List<PGNNode> nodes = new List<PGNNode>();
        string[] game = pgn.Replace("\n", " ").Split(' ');
        Board board = new Board(Presets.StartingBoard);

        foreach (string uci in game)
        {
            if (uci.Equals(string.Empty) || uci[^1] == '.') // notates the index of the move
                continue;
            Move move = new Move(uci, board); // converts the move from UCI notation to Move
            board.MakeMove(move);
            
            nodes.Add(new PGNNode { board = new Board(board), move = move });
        }

        return nodes.ToArray();
    }
}

public struct PGNNode(Board board, Move move, long time = 0)
{
    public Board board = board;
    public Move move = move;
    public readonly long time = time;
}
